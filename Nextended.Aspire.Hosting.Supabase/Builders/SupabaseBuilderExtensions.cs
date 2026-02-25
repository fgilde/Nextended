using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Helpers;
using Nextended.Aspire.Hosting.Supabase.Resources;
using static Nextended.Aspire.Hosting.Supabase.Helpers.SupabaseLogger;

namespace Nextended.Aspire.Hosting.Supabase.Builders;

/// <summary>
/// Provides the main extension method for adding Supabase to an Aspire application.
/// </summary>
public static class SupabaseBuilderExtensions
{
    #region Constants

    internal static class Images
    {
        // Original versions that worked locally
        public const string Postgres = "supabase/postgres";
        public const string PostgresTag = "15.1.1.78";
        public const string GoTrue = "supabase/gotrue";
        public const string GoTrueTag = "v2.185.0";
        public const string PostgREST = "postgrest/postgrest";
        public const string PostgRESTTag = "v12.2.0";
        public const string StorageApi = "supabase/storage-api";
        public const string StorageApiTag = "v1.11.13";
        public const string Kong = "kong";
        public const string KongTag = "2.8.1";
        public const string PostgresMeta = "supabase/postgres-meta";
        public const string PostgresMetaTag = "v0.83.2";
        public const string Studio = "supabase/studio";
        public const string StudioTag = "latest";
        public const string EdgeRuntime = "supabase/edge-runtime";
        public const string EdgeRuntimeTag = "v1.67.4";
        public const string Realtime = "supabase/realtime";
        public const string RealtimeTag = "v2.30.23";
    }

    internal static class Ports
    {
        public const int Postgres = 5432;
        public const int GoTrue = 9999;
        public const int PostgREST = 3000;
        public const int StorageApi = 5000;
        public const int Kong = 8000;
        public const int PostgresMeta = 8080;
        public const int Studio = 3000;
        public const int EdgeRuntime = 9000;
        public const int Realtime = 4000;
    }

    internal static class Defaults
    {
        public const string JwtSecret = "super-secret-jwt-token-with-at-least-32-characters-long";
        public const string AnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6ImFub24iLCJleHAiOjE5ODM4MTI5OTZ9.CRXP1A7WOeoJeXxjNni43kdQwgnWNReilDMblYTn_I0";
        public const string ServiceRoleKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImV4cCI6MTk4MzgxMjk5Nn0.EGIM96RAZx35lJzdJsyH-qQwv8Hdp7fsn3W0YpN81IU";
        public const string Password = "postgres-insecure-dev-password";
        public const int ExternalPostgresPort = 54322;
        public const int ExternalKongPort = 8000;
        public const int ExternalStudioPort = 54323;
    }

    #endregion

    #region Clear Infrastructure

    /// <summary>
    /// Clears all Supabase infrastructure (Docker containers, volumes, and data files).
    /// Call this before AddSupabase() for a clean start.
    /// </summary>
    public static IDistributedApplicationBuilder ClearSupabase(
        this IDistributedApplicationBuilder builder,
        string containerPrefix = "supabase")
    {
        LogInformation("Clearing Supabase infrastructure...");

        var containerNames = new[]
        {
            containerPrefix,
            $"{containerPrefix}-db",
            $"{containerPrefix}-auth",
            $"{containerPrefix}-rest",
            $"{containerPrefix}-storage",
            $"realtime-dev.{containerPrefix}-realtime",
            $"{containerPrefix}-kong",
            $"{containerPrefix}-meta",
            $"{containerPrefix}-edge",
            $"{containerPrefix}-init"
        };

        foreach (var container in containerNames)
        {
            try
            {
                var stopProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"rm -f {container}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                stopProcess?.WaitForExit(5000);
            }
            catch { /* Ignore errors if container doesn't exist */ }
        }

        var infraDir = Path.Combine(builder.AppHostDirectory, "..", "infra", "supabase");
        if (Directory.Exists(infraDir))
        {
            try
            {
                Directory.Delete(infraDir, recursive: true);
                LogInformation($"Directory deleted: {infraDir}");
            }
            catch (Exception ex)
            {
                LogWarning($"Could not delete directory: {ex.Message}");
            }
        }

        LogInformation("Cleanup completed.");
        return builder;
    }

    #endregion

    #region Main Entry Point

    /// <summary>
    /// Adds a complete Supabase stack to the application.
    /// The returned resource IS the Studio Dashboard container and serves as the visual parent
    /// for all other Supabase containers in the Aspire dashboard.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the Supabase resource (will appear as "supabase" in dashboard).</param>
    /// <returns>A resource builder for further configuration.</returns>
    public static IResourceBuilder<SupabaseStackResource> AddSupabase(
        this IDistributedApplicationBuilder builder,
        string name)
    {
        // Create the main stack resource (which IS the Studio container)
        var stack = new SupabaseStackResource(name)
        {
            JwtSecret = Defaults.JwtSecret,
            AnonKey = Defaults.AnonKey,
            ServiceRoleKey = Defaults.ServiceRoleKey,
            AppBuilder = builder
        };

        // Ensure directories exist
        var rootDir = Path.Combine(builder.AppHostDirectory, "..", "infra", "supabase");
        var dirs = EnsureDirectories(rootDir);
        stack.InfraRootDir = rootDir;
        stack.InitSqlPath = dirs.Init;

        var containerPrefix = name;
        var isPublishMode = builder.ExecutionContext.IsPublishMode;

        // --- Create typed container resources ---

        // DATABASE
        var dbResource = new SupabaseDatabaseResource($"{containerPrefix}-db")
        {
            Password = Defaults.Password,
            ExternalPort = Defaults.ExternalPostgresPort,
            Stack = stack
        };

        // Create initial configuration files with default password
        SupabaseSqlGenerator.WriteInitSql(dirs.Init, dbResource.Password);

        // Read init SQL content for publish mode
        var initSqlPath = Path.Combine(dirs.Init, "00_init.sql");
        var initSqlContent = File.Exists(initSqlPath) ? File.ReadAllText(initSqlPath) : "";
        var initSqlBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(initSqlContent));

        var dbBuilder = builder.AddResource(dbResource)
            .WithImage(Images.Postgres, Images.PostgresTag)
            .WithContainerName($"{containerPrefix}-db")
            .WithEnvironment("POSTGRES_PASSWORD", dbResource.Password)
            .WithEnvironment("POSTGRES_DB", "postgres")
            .WithEndpoint(port: isPublishMode ? null : dbResource.ExternalPort, targetPort: Ports.Postgres, name: "tcp", scheme: "tcp", isExternal: !isPublishMode);

        if (isPublishMode)
        {
            // In Azure Container Apps:
            // The supabase/postgres image creates roles with its own default passwords.
            // The demote-postgres script removes superuser from postgres, preventing password changes.
            //
            // Solution: Use a wrapper script that:
            // 1. DISABLES the demote-postgres script (so postgres stays superuser)
            // 2. Creates a password-setting script in the CORRECT directory
            // 3. Starts postgres with listen_addresses=* for container networking
            //
            // IMPORTANT: Password is read from environment at RUNTIME, not hardcoded at build time.
            // Data is ephemeral (lost on restart) - for production use Azure Database for PostgreSQL.

            stack.InitSqlBase64 = initSqlBase64;

            // Wrapper script - MUST use LF line endings (no CRLF)
            // Uses printf for password escaping to handle special characters correctly
            var wrapperScript =
                "#!/bin/bash\n" +
                "set -e\n" +
                "\n" +
                "echo '[DB-Init] === Starting Supabase PostgreSQL initialization ==='\n" +
                "echo \"[DB-Init] Password length: ${#POSTGRES_PASSWORD} characters\"\n" +
                "\n" +
                "# Step 1: Disable the demote-postgres script\n" +
                "# This script removes superuser from postgres, which prevents password changes\n" +
                "echo '[DB-Init] Step 1: Checking for demote-postgres script...'\n" +
                "DEMOTE_SCRIPT='/docker-entrypoint-initdb.d/migrations/10000000000000_demote-postgres.sql'\n" +
                "if [ -f \"$DEMOTE_SCRIPT\" ]; then\n" +
                "    echo '[DB-Init] Found demote-postgres script, disabling it...'\n" +
                "    echo '-- Disabled by Aspire wrapper to allow password management' > \"$DEMOTE_SCRIPT\"\n" +
                "    echo '[DB-Init] demote-postgres script disabled'\n" +
                "else\n" +
                "    echo '[DB-Init] WARNING: demote-postgres script not found at expected location'\n" +
                "    echo '[DB-Init] Searching for demote scripts...'\n" +
                "    find /docker-entrypoint-initdb.d -name '*demote*' 2>/dev/null || echo 'No demote scripts found'\n" +
                "fi\n" +
                "\n" +
                "# Step 2: Ensure init-scripts directory exists\n" +
                "echo '[DB-Init] Step 2: Ensuring init-scripts directory exists...'\n" +
                "INIT_SCRIPTS_DIR='/docker-entrypoint-initdb.d/init-scripts'\n" +
                "if [ ! -d \"$INIT_SCRIPTS_DIR\" ]; then\n" +
                "    echo '[DB-Init] Creating init-scripts directory...'\n" +
                "    mkdir -p \"$INIT_SCRIPTS_DIR\"\n" +
                "fi\n" +
                "\n" +
                "# Step 3: Create password-setting script with proper escaping\n" +
                "# Use printf to safely escape the password for SQL\n" +
                "echo '[DB-Init] Step 3: Creating password init script...'\n" +
                "\n" +
                "# Escape password for SQL: replace ' with ''\n" +
                "SAFE_PWD=$(printf '%s' \"$POSTGRES_PASSWORD\" | sed \"s/'/''/g\")\n" +
                "\n" +
                "# Write the SQL script - using direct placeholder (no psql \\set commands)\n" +
                "cat > \"$INIT_SCRIPTS_DIR/99999999999999-set-passwords.sql\" << 'SQLEOF'\n" +
                "-- Supabase Password Configuration Script\n" +
                "-- Generated by Aspire wrapper at container startup\n" +
                "-- This runs AFTER roles are created (in 00000000000000-initial-schema.sql)\n" +
                "-- demote-postgres has been disabled, so postgres is still superuser\n" +
                "\n" +
                "DO $$\n" +
                "DECLARE\n" +
                "    target_password TEXT := '__PASSWORD_PLACEHOLDER__';\n" +
                "BEGIN\n" +
                "    RAISE NOTICE '[Password-Init] Setting passwords for all Supabase roles...';\n" +
                "    \n" +
                "    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'supabase_admin') THEN\n" +
                "        EXECUTE format('ALTER ROLE supabase_admin WITH PASSWORD %L', target_password);\n" +
                "        RAISE NOTICE '[Password-Init] Password set for supabase_admin';\n" +
                "    ELSE\n" +
                "        RAISE WARNING '[Password-Init] Role supabase_admin does not exist!';\n" +
                "    END IF;\n" +
                "    \n" +
                "    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'authenticator') THEN\n" +
                "        EXECUTE format('ALTER ROLE authenticator WITH PASSWORD %L', target_password);\n" +
                "        RAISE NOTICE '[Password-Init] Password set for authenticator';\n" +
                "    ELSE\n" +
                "        RAISE WARNING '[Password-Init] Role authenticator does not exist!';\n" +
                "    END IF;\n" +
                "    \n" +
                "    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'supabase_auth_admin') THEN\n" +
                "        EXECUTE format('ALTER ROLE supabase_auth_admin WITH PASSWORD %L', target_password);\n" +
                "        RAISE NOTICE '[Password-Init] Password set for supabase_auth_admin';\n" +
                "    ELSE\n" +
                "        RAISE WARNING '[Password-Init] Role supabase_auth_admin does not exist!';\n" +
                "    END IF;\n" +
                "    \n" +
                "    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'supabase_storage_admin') THEN\n" +
                "        EXECUTE format('ALTER ROLE supabase_storage_admin WITH PASSWORD %L', target_password);\n" +
                "        RAISE NOTICE '[Password-Init] Password set for supabase_storage_admin';\n" +
                "    ELSE\n" +
                "        RAISE WARNING '[Password-Init] Role supabase_storage_admin does not exist!';\n" +
                "    END IF;\n" +
                "    \n" +
                "    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'postgres') THEN\n" +
                "        EXECUTE format('ALTER ROLE postgres WITH PASSWORD %L', target_password);\n" +
                "        RAISE NOTICE '[Password-Init] Password set for postgres';\n" +
                "    END IF;\n" +
                "    \n" +
                "    RAISE NOTICE '[Password-Init] === All role passwords configured successfully! ===';\n" +
                "END;\n" +
                "$$;\n" +
                "SQLEOF\n" +
                "\n" +
                "# Replace the placeholder with the actual escaped password\n" +
                "sed -i \"s/__PASSWORD_PLACEHOLDER__/$SAFE_PWD/g\" \"$INIT_SCRIPTS_DIR/99999999999999-set-passwords.sql\"\n" +
                "\n" +
                "echo '[DB-Init] Password script created successfully'\n" +
                "\n" +
                "# Step 4: List init-scripts directory to verify\n" +
                "echo '[DB-Init] Step 4: Listing init-scripts directory:'\n" +
                "ls -la \"$INIT_SCRIPTS_DIR/\" 2>/dev/null | head -20\n" +
                "\n" +
                "echo '[DB-Init] Listing migrations directory:'\n" +
                "ls -la /docker-entrypoint-initdb.d/migrations/ 2>/dev/null | head -10 || echo 'No migrations dir'\n" +
                "\n" +
                "# Step 5: Start PostgreSQL\n" +
                "echo '[DB-Init] Step 5: Starting PostgreSQL with listen_addresses=*...'\n" +
                "echo '[DB-Init] === Wrapper script completed, handing over to docker-entrypoint.sh ==='\n" +
                "exec /usr/local/bin/docker-entrypoint.sh postgres -c listen_addresses='*' -c max_connections=200\n";

            var wrapperBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(wrapperScript));

            dbBuilder
                .WithEnvironment("WRAPPER_SCRIPT_BASE64", wrapperBase64)
                .WithEntrypoint("/bin/bash")
                .WithArgs("-c",
                    "echo \"$WRAPPER_SCRIPT_BASE64\" | base64 -d > /tmp/init-wrapper.sh && " +
                    "chmod +x /tmp/init-wrapper.sh && " +
                    "exec /tmp/init-wrapper.sh");

            LogWarning("PostgreSQL in ACA uses ephemeral storage. Data will be lost on restart. For production, use Azure Database for PostgreSQL.");
        }
        else
        {
            // Local development: Use bind mounts for persistence
            dbBuilder
                .WithBindMount(dirs.Data, "/var/lib/postgresql/data")
                .WithBindMount(dirs.Init, "/docker-entrypoint-initdb.d", isReadOnly: true);
        }
        stack.Database = dbBuilder;

        // Get database endpoint for dynamic service discovery (used for local development)
        var dbEndpoint = dbBuilder.GetEndpoint("tcp");

        // All services use Aspire's endpoint references which work in both local and Azure Container Apps
        // Azure Container Apps has internal DNS that resolves container names automatically

        // POST-INIT CONTAINER - runs AFTER Auth starts to create triggers on auth.users
        // NOTE: Password updates now happen in the DB container itself (wrapper script)
        // This container is only for trigger creation and user setup
        var scriptsDir = Path.Combine(Path.GetDirectoryName(dirs.Init)!, "scripts");
        Directory.CreateDirectory(scriptsDir);

        var postInitSqlPath = Path.Combine(scriptsDir, "post_init.sql");
        SupabaseSqlGenerator.WritePostInitSql(postInitSqlPath, dbResource.Password);

        var postInitShPath = Path.Combine(scriptsDir, "post_init.sh");
        SupabaseSqlGenerator.WritePostInitScript(postInitShPath, $"{containerPrefix}-db", dbResource.Password);

        // Store scripts dir path for later use
        stack.ScriptsDir = scriptsDir;

        if (isPublishMode)
        {
            // Read post_init.sql content (triggers and user setup)
            var postInitSqlContent = File.Exists(postInitSqlPath) ? File.ReadAllText(postInitSqlPath) : "";

            // Check for users.sql and append if small enough (< 50KB to stay safe)
            var usersSqlPath = Path.Combine(scriptsDir, "users.sql");
            if (File.Exists(usersSqlPath))
            {
                var usersSql = File.ReadAllText(usersSqlPath);
                if (usersSql.Length < 50000)
                {
                    postInitSqlContent += "\n\n-- Users\n" + usersSql;
                }
                else
                {
                    LogWarning("users.sql is too large for Azure deployment. Users will not be created automatically.");
                }
            }

            // Store init SQL content for later (init container created after Auth)
            stack.PostInitSqlBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(postInitSqlContent));
        }

        // AUTH (GoTrue)
        var authResource = new SupabaseAuthResource($"{containerPrefix}-auth") { Stack = stack };

        // Build database connection string using Aspire's endpoint references
        // Azure Container Apps has internal DNS - container names resolve automatically
        var authDbUrl = ReferenceExpression.Create(
            $"postgres://supabase_auth_admin:{dbResource.Password}@{dbEndpoint.Property(EndpointProperty.Host)}:{dbEndpoint.Property(EndpointProperty.Port)}/postgres?search_path=auth");

        stack.Auth = builder.AddResource(authResource)
            .WithImage(Images.GoTrue, Images.GoTrueTag)
            .WithContainerName($"{containerPrefix}-auth")
            .WithEnvironment("GOTRUE_API_HOST", "0.0.0.0")
            .WithEnvironment("GOTRUE_API_PORT", Ports.GoTrue.ToString())
            .WithEnvironment("GOTRUE_DB_DRIVER", "postgres")
            .WithEnvironment("GOTRUE_DB_DATABASE_URL", authDbUrl)
            .WithEnvironment("GOTRUE_DB_NAMESPACE", "auth")
            .WithEnvironment("GOTRUE_SITE_URL", authResource.SiteUrl)
            // API_EXTERNAL_URL will be set after Kong is created (see below)
            .WithEnvironment("GOTRUE_URI_ALLOW_LIST", "*")
            .WithEnvironment("GOTRUE_JWT_SECRET", stack.JwtSecret)
            .WithEnvironment("GOTRUE_JWT_EXP", authResource.JwtExpiration.ToString())
            .WithEnvironment("GOTRUE_JWT_DEFAULT_GROUP_NAME", "authenticated")
            .WithEnvironment("GOTRUE_JWT_ADMIN_ROLES", "service_role")
            .WithEnvironment("GOTRUE_JWT_AUD", "authenticated")
            .WithEnvironment("GOTRUE_EXTERNAL_EMAIL_ENABLED", "true")
            .WithEnvironment("GOTRUE_MAILER_AUTOCONFIRM", authResource.AutoConfirm ? "true" : "false")
            .WithEnvironment("GOTRUE_MAILER_SECURE_EMAIL_CHANGE_ENABLED", "false")
            .WithEnvironment("GOTRUE_DISABLE_SIGNUP", authResource.DisableSignup ? "true" : "false")
            .WithEnvironment("GOTRUE_ANONYMOUS_USERS_ENABLED", authResource.AnonymousUsersEnabled ? "true" : "false")
            .WithEnvironment("GOTRUE_RATE_LIMIT_HEADER", "X-Forwarded-For")
            .WithEnvironment("GOTRUE_RATE_LIMIT_EMAIL_SENT", "100")
            .WithEndpoint(targetPort: Ports.GoTrue, name: "http", scheme: "http", isExternal: false)
            .WithContainerRuntimeArgs("--restart=on-failure:10")
            .WaitFor(stack.Database);  // Password updates happen in DB container itself

        // REST (PostgREST)
        var restResource = new SupabaseRestResource($"{containerPrefix}-rest") { Stack = stack };

        // Build database URI using Aspire's endpoint references
        // Azure Container Apps has internal DNS - container names resolve automatically
        var restDbUri = ReferenceExpression.Create(
            $"postgres://authenticator:{dbResource.Password}@{dbEndpoint.Property(EndpointProperty.Host)}:{dbEndpoint.Property(EndpointProperty.Port)}/postgres");

        stack.Rest = builder.AddResource(restResource)
            .WithImage(Images.PostgREST, Images.PostgRESTTag)
            .WithContainerName($"{containerPrefix}-rest")
            .WithEnvironment("PGRST_DB_URI", restDbUri)
            .WithEnvironment("PGRST_DB_SCHEMAS", string.Join(",", restResource.Schemas))
            .WithEnvironment("PGRST_DB_ANON_ROLE", restResource.AnonRole)
            .WithEnvironment("PGRST_JWT_SECRET", stack.JwtSecret)
            .WithEnvironment("PGRST_DB_USE_LEGACY_GUCS", "false")
            // Required for JWT validation in requests
            .WithEnvironment("PGRST_APP_SETTINGS_JWT_SECRET", stack.JwtSecret)
            .WithEnvironment("PGRST_APP_SETTINGS_JWT_EXP", "3600")
            .WithEndpoint(targetPort: Ports.PostgREST, name: "http", scheme: "http", isExternal: false)
            .WithContainerRuntimeArgs("--restart=on-failure:10")
            .WaitFor(stack.Database);  // Password updates happen in DB container itself

        // STORAGE
        var storageResource = new SupabaseStorageResource($"{containerPrefix}-storage") { Stack = stack };

        // Build database URL using Aspire's endpoint references
        // Azure Container Apps has internal DNS - container names resolve automatically
        var storageDatabaseUrl = ReferenceExpression.Create(
            $"postgres://supabase_storage_admin:{dbResource.Password}@{dbEndpoint.Property(EndpointProperty.Host)}:{dbEndpoint.Property(EndpointProperty.Port)}/postgres");
        // Build REST URL using Aspire's endpoint reference for HTTP
        var postgrestUrl = stack.Rest.GetEndpoint("http");

        var storageBuilder = builder.AddResource(storageResource)
            .WithImage(Images.StorageApi, Images.StorageApiTag)
            .WithContainerName($"{containerPrefix}-storage")
            .WithEnvironment("ANON_KEY", stack.AnonKey)
            .WithEnvironment("SERVICE_KEY", stack.ServiceRoleKey)
            .WithEnvironment("POSTGREST_URL", postgrestUrl)
            .WithEnvironment("PGRST_JWT_SECRET", stack.JwtSecret)
            .WithEnvironment("DATABASE_URL", storageDatabaseUrl)
            .WithEnvironment("FILE_STORAGE_BACKEND_PATH", "/var/lib/storage")
            .WithEnvironment("STORAGE_BACKEND", storageResource.Backend)
            .WithEnvironment("FILE_SIZE_LIMIT", storageResource.FileSizeLimit.ToString())
            .WithEnvironment("TENANT_ID", "stub")
            .WithEnvironment("REGION", isPublishMode ? "azure" : "local")
            .WithEnvironment("GLOBAL_S3_BUCKET", "stub")
            .WithEnvironment("IS_MULTITENANT", "false")
            .WithEnvironment("ENABLE_IMAGE_TRANSFORMATION", storageResource.EnableImageTransformation ? "true" : "false")
            // Required for proxy path handling
            .WithEnvironment("REQUEST_ALLOW_X_FORWARDED_PATH", "true")
            .WithEndpoint(targetPort: Ports.StorageApi, name: "http", scheme: "http", isExternal: false)
            .WithContainerRuntimeArgs("--restart=on-failure:10")
            .WaitFor(stack.Database)  // Password updates happen in DB container itself
            .WaitFor(stack.Rest);

        if (isPublishMode)
        {
            // In publish mode: Use separate volume for storage data (Azure Files in ACA)
            // Use short volume name to avoid ARM naming collisions
            var volPrefix = containerPrefix.Length > 8 ? containerPrefix[..8] : containerPrefix;
            storageBuilder.WithVolume($"{volPrefix}store", "/var/lib/storage");
        }
        else
        {
            // Local development: Use bind mount
            storageBuilder.WithBindMount(dirs.Storage, "/var/lib/storage");
        }
        stack.Storage = storageBuilder;

        // REALTIME
        var realtimeResource = new SupabaseRealtimeResource($"{containerPrefix}-realtime") { Stack = stack };

        // Build database URL using Aspire's endpoint references
        var realtimeDbHost = dbEndpoint.Property(EndpointProperty.Host);
        var realtimeDbPort = dbEndpoint.Property(EndpointProperty.Port);

        // The realtime-dev. prefix in the container name is critical:
        // Supabase Realtime extracts the tenant ID from the Host header.
        // Kong forwards requests with Host: <container-name>:<port>, so the
        // container name MUST start with "realtime-dev." for tenant lookup to work.
        // (Previous DNS failure was caused by the container crash-looping, not by the dot.)
        var realtimeContainerName = $"realtime-dev.{containerPrefix}-realtime";

        var realtimeBuilder = builder.AddResource(realtimeResource)
            .WithImage(Images.Realtime, Images.RealtimeTag);

        if (!isPublishMode)
        {
            realtimeBuilder.WithContainerName(realtimeContainerName);
        }

        realtimeBuilder
            .WithEnvironment("PORT", Ports.Realtime.ToString())
            .WithEnvironment("DB_HOST", realtimeDbHost)
            .WithEnvironment("DB_PORT", realtimeDbPort)
            .WithEnvironment("DB_USER", "supabase_admin")
            .WithEnvironment("DB_PASSWORD", dbResource.Password)
            .WithEnvironment("DB_NAME", "postgres")
            .WithEnvironment("DB_AFTER_CONNECT_QUERY", "SET search_path TO _realtime")
            .WithEnvironment("DB_ENC_KEY", "supabaserealtime")
            .WithEnvironment("API_JWT_SECRET", stack.JwtSecret)
            .WithEnvironment("SECRET_KEY_BASE", "UpNVntn3cDxHJpq99YMc1T1AQgQpc8kfYTuRgBiYa15BLrx8etQoXz3gZv1/u2oq")
            .WithEnvironment("ERL_AFLAGS", "-proto_dist inet_tcp")
            .WithEnvironment("DNS_NODES", "")
            .WithEnvironment("APP_NAME", "realtime")
            .WithEnvironment("SEED_SELF_HOST", "true")
            .WithEnvironment("RUN_JANITOR", "true")
            .WithEnvironment("RLIMIT_NOFILE", "10000")
            .WithEnvironment("ENABLE_ERL_CRASH_DUMP", "false")
            .WithEnvironment("DISABLE_HEALTHCHECK_LOGGING", "true")
            .WithEndpoint(targetPort: Ports.Realtime, name: "http", scheme: "http", isExternal: false)
            .WithContainerRuntimeArgs("--restart=on-failure:10")
            .WaitFor(stack.Database);
        stack.Realtime = realtimeBuilder;

        // META (Postgres-Meta) - Created before Kong so Kong can reference its endpoint
        var metaResource = new SupabaseMetaResource($"{containerPrefix}-meta") { Stack = stack };

        var metaBuilder = builder.AddResource(metaResource)
            .WithImage(Images.PostgresMeta, Images.PostgresMetaTag)
            .WithContainerName($"{containerPrefix}-meta")
            .WithEnvironment("PG_META_PORT", metaResource.Port.ToString())
            .WithEnvironment("PG_META_DB_NAME", "postgres")
            .WithEnvironment("PG_META_DB_USER", "supabase_admin")
            .WithEnvironment("PG_META_DB_PASSWORD", dbResource.Password)
            .WithEndpoint(targetPort: Ports.PostgresMeta, name: "http", scheme: "http", isExternal: false)
            .WaitFor(stack.Database);

        // Set DB host/port using Aspire's endpoint references
        // Azure Container Apps has internal DNS - container name works without FQDN
        metaBuilder
            .WithEnvironment("PG_META_DB_HOST", dbEndpoint.Property(EndpointProperty.Host))
            .WithEnvironment("PG_META_DB_PORT", dbEndpoint.Property(EndpointProperty.Port));
        stack.Meta = metaBuilder;

        // KONG (API Gateway)
        var kongResource = new SupabaseKongResource($"{containerPrefix}-kong")
        {
            ExternalPort = Defaults.ExternalKongPort,
            Stack = stack
        };

        // Generate Kong config
        SupabaseSqlGenerator.WriteKongConfig(
            Path.Combine(dirs.Config, "kong.yml"),
            stack.AnonKey,
            stack.ServiceRoleKey,
            containerPrefix,
            Ports.GoTrue,
            Ports.PostgREST,
            Ports.StorageApi,
            Ports.PostgresMeta,
            Ports.EdgeRuntime,
            Ports.Realtime);

        // Read Kong config content for publish mode
        var kongConfigContent = File.ReadAllText(Path.Combine(dirs.Config, "kong.yml"));
        var kongConfigBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(kongConfigContent));

        var kongBuilder = builder.AddResource(kongResource)
            .WithImage(Images.Kong, Images.KongTag)
            .WithContainerName($"{containerPrefix}-kong")
            .WithEnvironment("KONG_DATABASE", "off")
            .WithEnvironment("KONG_DNS_ORDER", "LAST,A,CNAME")
            .WithEnvironment("KONG_PLUGINS", string.Join(",", kongResource.Plugins))
            .WithEnvironment("KONG_NGINX_PROXY_PROXY_BUFFER_SIZE", "160k")
            .WithEnvironment("KONG_NGINX_PROXY_PROXY_BUFFERS", "64 160k")
            .WithEnvironment("KONG_NGINX_PROXY_LARGE_CLIENT_HEADER_BUFFERS", "4 64k")
            // Required for JWT validation in Kong
            .WithEnvironment("SUPABASE_ANON_KEY", stack.AnonKey)
            .WithEnvironment("SUPABASE_SERVICE_KEY", stack.ServiceRoleKey)
            // isProxied: false in local mode to bypass DCP proxy which doesn't support WebSocket upgrades from browsers
            .WithHttpEndpoint(port: isPublishMode ? null : kongResource.ExternalPort, targetPort: Ports.Kong, name: "http", isProxied: isPublishMode)
            .WaitFor(stack.Auth)
            .WaitFor(stack.Rest)
            .WaitFor(stack.Storage)
            .WaitFor(stack.Realtime)
            .WaitFor(stack.Meta);

        if (isPublishMode)
        {
            kongBuilder.WithExternalHttpEndpoints();
        }

        if (isPublishMode)
        {
            // In Azure Container Apps:
            // Init containers are separate Container Apps - they CANNOT share volumes!
            // Solution: Use a custom entrypoint that writes the config at startup
            // We pass the Kong config template as base64 and use envsubst to replace service URLs

            var kongConfigTemplate = SupabaseSqlGenerator.GetKongConfigTemplateForPublish(stack.AnonKey, stack.ServiceRoleKey);
            var kongConfigTemplateBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(kongConfigTemplate));
            stack.KongConfigBase64 = kongConfigTemplateBase64;

            // Get endpoint references for all services Kong needs to route to
            var authEndpoint = stack.Auth.GetEndpoint("http");
            var restEndpoint = stack.Rest.GetEndpoint("http");
            var storageEndpoint = stack.Storage.GetEndpoint("http");
            var metaEndpoint = stack.Meta.GetEndpoint("http");

            // Custom entrypoint: decode config template, substitute URLs, write file, start Kong
            // Note: We use sed instead of envsubst to avoid needing to install gettext
            // This is more compatible across different container environments
            kongBuilder
                .WithEnvironment("KONG_CONFIG_TEMPLATE_BASE64", kongConfigTemplateBase64)
                .WithEnvironment("AUTH_URL", authEndpoint)
                .WithEnvironment("REST_URL", restEndpoint)
                .WithEnvironment("STORAGE_URL", storageEndpoint)
                .WithEnvironment("META_URL", metaEndpoint)
                // Realtime: Use endpoint reference (works in ACA internal DNS)
                .WithEnvironment("REALTIME_URL", stack.Realtime!.GetEndpoint("http"));

            // Edge: Deferred because EdgeRuntime is created in WithEdgeFunctions() after AddSupabase()
            kongBuilder.WithEnvironment(context =>
            {
                if (stack.EdgeRuntime != null)
                    context.EnvironmentVariables["EDGE_URL"] = stack.EdgeRuntime.GetEndpoint("http");
                else
                    context.EnvironmentVariables["EDGE_URL"] = $"http://{containerPrefix}-edge:{Ports.EdgeRuntime}";
            });

            kongBuilder
                .WithEntrypoint("/bin/sh")
                .WithArgs("-c",
                    "set -e && " +
                    "echo '[Kong Init] Starting configuration...' && " +
                    "mkdir -p /home/kong && " +
                    "echo '[Kong Init] Decoding config template...' && " +
                    "echo \"$KONG_CONFIG_TEMPLATE_BASE64\" | base64 -d > /tmp/kong.yml.template && " +
                    "echo '[Kong Init] Substituting URLs using sed...' && " +
                    "sed -e \"s|\\${AUTH_URL}|$AUTH_URL|g\" " +
                        "-e \"s|\\${REST_URL}|$REST_URL|g\" " +
                        "-e \"s|\\${STORAGE_URL}|$STORAGE_URL|g\" " +
                        "-e \"s|\\${META_URL}|$META_URL|g\" " +
                        "-e \"s|\\${EDGE_URL}|$EDGE_URL|g\" " +
                        "-e \"s|\\${REALTIME_URL}|$REALTIME_URL|g\" " +
                        "/tmp/kong.yml.template > /home/kong/kong.yml && " +
                    "echo '[Kong Init] Config created. URLs:' && " +
                    "echo \"AUTH=$AUTH_URL REST=$REST_URL STORAGE=$STORAGE_URL META=$META_URL REALTIME=$REALTIME_URL\" && " +
                    "echo '[Kong Init] Starting Kong...' && " +
                    "export KONG_DECLARATIVE_CONFIG=/home/kong/kong.yml && " +
                    "exec /docker-entrypoint.sh kong docker-start");
        }
        else
        {
            // Local development: Use bind mount and standard config path
            kongBuilder
                .WithEnvironment("KONG_DECLARATIVE_CONFIG", "/home/kong/kong.yml")
                .WithBindMount(Path.Combine(dirs.Config, "kong.yml"), "/home/kong/kong.yml", isReadOnly: true);
        }
        stack.Kong = kongBuilder;

        // Now that Kong is created, set API_EXTERNAL_URL for Auth using Aspire's endpoint reference
        stack.Auth.WithEnvironment("API_EXTERNAL_URL", stack.Kong.GetEndpoint("http"));

        // STUDIO - Configure the stack resource itself as the Studio container
        stack.StudioPort = Defaults.ExternalStudioPort;
        
        // Build URLs using Aspire's endpoint references for HTTP services
        var studioMetaUrl = stack.Meta.GetEndpoint("http");
        var studioKongUrl = stack.Kong.GetEndpoint("http");
        var studioAuthUrl = stack.Auth.GetEndpoint("http");

        var stackBuilder = builder.AddResource(stack)
            .WithImage(Images.Studio, Images.StudioTag)
            .WithContainerName(name)
            .WithEnvironment("STUDIO_PG_META_URL", studioMetaUrl)
            .WithEnvironment("POSTGRES_PASSWORD", dbResource.Password)
            .WithEnvironment("POSTGRES_DB", "postgres")
            .WithEnvironment("POSTGRES_USER", "supabase_admin")
            .WithEnvironment("DEFAULT_ORGANIZATION_NAME", "Default Organization")
            .WithEnvironment("DEFAULT_PROJECT_NAME", "Default Project")
            .WithEnvironment("SUPABASE_URL", studioKongUrl)
            .WithEnvironment("SUPABASE_PUBLIC_URL", studioKongUrl)
            .WithEnvironment("SUPABASE_ANON_KEY", stack.AnonKey)
            .WithEnvironment("SUPABASE_SERVICE_KEY", stack.ServiceRoleKey)
            .WithEnvironment("GOTRUE_URL", studioAuthUrl)
            .WithEnvironment("AUTH_JWT_SECRET", stack.JwtSecret)
            .WithEnvironment("LOGFLARE_API_KEY", "")
            .WithEnvironment("LOGFLARE_URL", "")
            .WithEnvironment("NEXT_PUBLIC_ENABLE_LOGS", "false")
            .WithEnvironment("NEXT_ANALYTICS_BACKEND_PROVIDER", "")
            .WithEnvironment("SNIPPETS_MANAGEMENT_FOLDER", "/app/snippets")
            .WithEnvironment("EDGE_FUNCTIONS_MANAGEMENT_FOLDER", "/app/edge-functions")
            .WithHttpEndpoint(port: isPublishMode ? null : stack.StudioPort, targetPort: Ports.Studio, name: "http")
            .WaitFor(stack.Meta)
            .WaitFor(stack.Kong)
            .WaitFor(stack.Auth);

        // Set DB host/port using Aspire's endpoint references
        // Azure Container Apps has internal DNS - container name works without FQDN
        stackBuilder
            .WithEnvironment("POSTGRES_HOST", dbEndpoint.Property(EndpointProperty.Host))
            .WithEnvironment("POSTGRES_PORT", dbEndpoint.Property(EndpointProperty.Port));

        stack.StackBuilder = stackBuilder;

        // POST-INIT CONTAINER - Create AFTER Auth so it can wait for auth.users table
        // This container creates triggers and user profiles
        IResourceBuilder<ContainerResource>? initContainer = null;

        if (isPublishMode && !string.IsNullOrEmpty(stack.PostInitSqlBase64))
        {
            // Post-init: Wait for Auth to create auth.users, then create triggers
            var postInitCommand = "set -e && " +
                                  "echo '[Post-Init] Waiting for Auth to initialize...' && " +
                                  "sleep 60 && " +  // Give Auth time to run migrations and create auth.users
                                  "echo \"$POST_INIT_SQL_BASE64\" | base64 -d > /tmp/post_init.sql && " +
                                  "echo '[Post-Init] Running post-init SQL (triggers, profiles)...' && " +
                                  "PGPASSWORD=\"$DB_PASSWORD\" psql -h \"$DB_HOST\" -p \"$DB_PORT\" -U postgres -d postgres " +
                                  "-v new_password=\"$DB_PASSWORD\" -f /tmp/post_init.sql && " +
                                  "echo '[Post-Init] Completed successfully'";

            initContainer = builder.AddContainer($"{containerPrefix}-init", Images.Postgres, Images.PostgresTag)
                .WithContainerName($"{containerPrefix}-init")
                .WithEnvironment("POST_INIT_SQL_BASE64", stack.PostInitSqlBase64)
                // Azure Container Apps has internal DNS - container name works without FQDN
                .WithEnvironment("DB_HOST", dbEndpoint.Property(EndpointProperty.Host))
                .WithEnvironment("DB_PORT", dbEndpoint.Property(EndpointProperty.Port))
                .WithEnvironment("DB_PASSWORD", dbResource.Password)
                .WithEntrypoint("/bin/bash")
                .WithArgs("-c", postInitCommand)
                .WaitFor(stack.Database)
                .WaitFor(stack.Auth);  // Wait for Auth to create auth.users table

            stack.InitContainer = initContainer;
        }
        else if (!isPublishMode && !string.IsNullOrEmpty(stack.ScriptsDir))
        {
            // Local development: Use bind mount
            initContainer = builder.AddContainer($"{containerPrefix}-init", Images.Postgres, Images.PostgresTag)
                .WithContainerName($"{containerPrefix}-init")
                .WithBindMount(stack.ScriptsDir, "/scripts", isReadOnly: true)
                .WithEntrypoint("/bin/bash")
                .WithArgs("/scripts/post_init.sh")
                .WaitFor(stack.Database)
                .WaitFor(stack.Auth);
        }

        // Realtime must wait for init container to COMPLETE (creates realtime schema and supabase_realtime publication).
        // WaitForCompletion waits until the container exits (not just starts), which is critical for init containers.
        if (initContainer != null)
        {
            stack.Realtime.WaitForCompletion(initContainer);
        }

        // Set parent relationships - Stack (Studio) is the visual parent for all containers
        stack.Database.WithParentRelationship(stack);
        initContainer?.WithParentRelationship(stack);
        stack.Auth.WithParentRelationship(stack);
        stack.Rest.WithParentRelationship(stack);
        stack.Storage.WithParentRelationship(stack);
        stack.Realtime.WithParentRelationship(stack);
        stack.Kong.WithParentRelationship(stack);
        stack.Meta.WithParentRelationship(stack);

        return stackBuilder;
    }

    #endregion

    #region Directory Management

    private record SupabaseDirs(string Init, string Storage, string Data, string Config);

    private static SupabaseDirs EnsureDirectories(string root)
    {
        var dirs = new SupabaseDirs(
            Init: Path.Combine(root, "db-init"),
            Storage: Path.Combine(root, "storage"),
            Data: Path.Combine(root, "db-data"),
            Config: Path.Combine(root, "config")
        );
        Directory.CreateDirectory(dirs.Init);
        Directory.CreateDirectory(dirs.Storage);
        Directory.CreateDirectory(dirs.Data);
        Directory.CreateDirectory(dirs.Config);
        return dirs;
    }

    #endregion

}
