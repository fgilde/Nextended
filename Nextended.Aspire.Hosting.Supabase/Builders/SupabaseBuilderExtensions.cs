using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Nextended.Aspire.Hosting.Supabase.Helpers;
using Nextended.Aspire.Hosting.Supabase.Resources;
using static Nextended.Aspire.Hosting.Supabase.Helpers.SupabaseLogger;

namespace Nextended.Aspire.Hosting.Supabase.Builders;

/// <summary>
/// Provides the main extension method for adding Supabase to an Aspire application.
/// </summary>
public static class SupabaseBuilderExtensions
{
    /// <summary>
    /// Optional name of a managedEnvironmentStorage (e.g. an NFS Azure Files share) to mount at
    /// the storage container's backend path (/var/lib/storage) in publish mode, so uploaded
    /// files persist across restarts/redeploys. This is a generic Supabase-storage concern —
    /// the HOST app owns creating the actual storage resource and just sets this name here.
    /// When null/empty, publish mode falls back to ephemeral container-local storage.
    /// </summary>
    public static string? PersistentStorageVolumeName { get; set; }

    /// <summary>
    /// Optional name of a managedEnvironmentStorage (e.g. an NFS Azure Files share) to mount at the
    /// PostgreSQL data directory (/var/lib/postgresql/data) in publish mode, so the whole database
    /// survives container restarts/redeploys. Without it the ACA database is ephemeral (a restart
    /// wipes ALL data). This is a generic Supabase concern — the HOST app owns creating the actual
    /// storage resource and just sets this name here. When null/empty, publish mode stays ephemeral.
    /// NOTE: PostgreSQL requires POSIX semantics (fsync/locking), so the backing share must be
    /// Azure Files *NFS* (Premium), never SMB; the DB container is pinned to a single replica.
    /// </summary>
    public static string? PostgresDataVolumeName { get; set; }

    /// <summary>
    /// Optional S3-compatible backend for the storage container (publish mode). When set, the
    /// storage container runs with STORAGE_BACKEND=s3 against this endpoint instead of the local
    /// FILE backend. This is a generic Supabase-storage concern: supabase-storage's FILE backend
    /// needs a local POSIX disk WITH extended attributes, which Azure Files can't provide (SMB
    /// rejects its open flags; NFS 4.1 has no xattr) — so a durable container-apps deploy needs an
    /// S3-compatible store. The HOST app owns the S3 server (e.g. a bundled MinIO), its bucket and
    /// credentials, and just points this at it. When null, the FILE backend is used (see
    /// <see cref="PersistentStorageVolumeName"/> / local bind mount).
    /// </summary>
    public static SupabaseStorageS3Options? StorageS3Backend { get; set; }

    /// <summary>
    /// Configuration for an S3-compatible storage backend (see <see cref="StorageS3Backend"/>).
    /// </summary>
    public sealed class SupabaseStorageS3Options
    {
        /// <summary>S3 endpoint URL the storage container connects to (e.g. http://minio:9000).</summary>
        public required ReferenceExpression Endpoint { get; init; }

        /// <summary>The single S3 bucket that backs all Supabase storage buckets. Must already exist.</summary>
        public required string Bucket { get; init; }

        /// <summary>Access key id (consumed via the AWS SDK default credential chain).</summary>
        public required string AccessKey { get; init; }

        /// <summary>Secret access key (consumed via the AWS SDK default credential chain).</summary>
        public required string SecretKey { get; init; }

        /// <summary>Region passed to the S3 client. MinIO ignores it, but the SDK requires one.</summary>
        public string Region { get; init; } = "us-east-1";

        /// <summary>Use path-style addressing (required for MinIO and most non-AWS S3 servers).</summary>
        public bool ForcePathStyle { get; init; } = true;
    }

    #region Constants

    internal static class Images
    {
        // Official compatible versions from supabase/supabase docker-compose (2026-03-16)
        public const string Postgres = "supabase/postgres";
        public const string PostgresTag = "15.8.1.085";
        public const string GoTrue = "supabase/gotrue";
        public const string GoTrueTag = "v2.186.0";
        public const string PostgREST = "postgrest/postgrest";
        public const string PostgRESTTag = "v14.6";
        public const string StorageApi = "supabase/storage-api";
        public const string StorageApiTag = "v1.44.2";
        public const string Kong = "kong/kong";
        public const string KongTag = "3.9.1";
        public const string PostgresMeta = "supabase/postgres-meta";
        public const string PostgresMetaTag = "v0.95.2";
        public const string Studio = "supabase/studio";
        public const string StudioTag = "2026.03.16-sha-5528817";
        public const string EdgeRuntime = "supabase/edge-runtime";
        public const string EdgeRuntimeTag = "v1.71.2";
        public const string Realtime = "supabase/realtime";
        public const string RealtimeTag = "v2.76.5";
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
        this IDistributedApplicationBuilder builder, bool removeContainer,
        string containerPrefix = "supabase")
    {
        LogInformation("Clearing Supabase infrastructure...");

        if (removeContainer)
        {
            var containerNames = new[]
            {
                containerPrefix,
                $"{containerPrefix}-db",
                $"{containerPrefix}-auth",
                $"{containerPrefix}-rest",
                $"{containerPrefix}-storage",
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
                catch
                {
                    /* Ignore errors if container doesn't exist */
                }
            }
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
    /// <param name="externalDatabase">
    /// Optional externally-provided Postgres resource (e.g. <c>builder.AddPostgres("mypg")</c>). When
    /// supplied, the stack does NOT create/own its own database container — every Supabase service and
    /// the post-init run against THIS resource, and you decide how it deploys
    /// (<c>PublishAsContainer()</c>, <c>PublishAsAzureContainerApp()</c>, …). REQUIREMENT: it must use
    /// the <c>supabase/postgres</c> image (via <c>.WithImage("supabase/postgres", "&lt;tag&gt;")</c>) —
    /// the Supabase roles + extensions come from that image's initdb; we only reset the roles'
    /// passwords to the resource's own password so the services can authenticate. When null, the stack
    /// creates its internal database exactly as before. Passing this makes <c>ConfigureDatabase(...)</c>
    /// throw (you own the DB config).
    /// </param>
    /// <returns>A resource builder for further configuration.</returns>
    public static IResourceBuilder<SupabaseStackResource> AddSupabase(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<PostgresServerResource>? externalDatabase = null)
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

        // DATABASE — internal supabase/postgres container (default) OR an external Postgres resource
        // the caller passed in. These shared handles are populated by whichever branch runs below, so
        // everything downstream (services, post-init, waits) stays mode-agnostic.
        var useExternalDb = externalDatabase is not null;
        stack.UsesExternalDatabase = useExternalDb;
        SupabaseDatabaseResource? dbResource = null;
        EndpointReference dbEndpoint;
        ReferenceExpression dbPassword;
        IResourceBuilder<IResource> dbWait;                 // long-running DB the services WaitFor
        IResourceBuilder<ContainerResource>? dbBootstrap = null; // external only: one-shot role setup

        if (!useExternalDb)
        {
        dbResource = new SupabaseDatabaseResource($"{containerPrefix}-db")
        {
            Password = Defaults.Password,
            ExternalPort = Defaults.ExternalPostgresPort,
            Stack = stack
        };

        // NOTE: SQL files are NOT written here. They are written once with the final password
        // via BeforeResourceStartedEvent (see below), after all ConfigureDatabase/WithPassword
        // calls have been applied.

        var dbBuilder = builder.AddResource(dbResource)
            .WithImage(Images.Postgres, Images.PostgresTag)
            .WithContainerName($"{containerPrefix}-db")
            .WithEnvironment(context => context.EnvironmentVariables["POSTGRES_PASSWORD"] = dbResource!.Password)
            .WithEnvironment("POSTGRES_DB", "postgres")
            .WithEndpoint(port: isPublishMode ? Ports.Postgres : dbResource.ExternalPort, targetPort: Ports.Postgres, name: "tcp", scheme: "tcp", isExternal: !isPublishMode);

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
                "# Step 2: Patch docker-entrypoint.sh to ensure the temp postgres instance\n" +
                "# also uses password_encryption=scram-sha-256 during init.\n" +
                "# The supabase image may default to md5, but pg_hba.conf requires scram-sha-256.\n" +
                "echo '[DB-Init] Step 2: Patching entrypoint for scram-sha-256...'\n" +
                "if grep -q 'listen_addresses' /usr/local/bin/docker-entrypoint.sh; then\n" +
                "    # Add password_encryption=scram-sha-256 to the temp server startup\n" +
                "    sed -i \"s/-c listen_addresses=''/-c listen_addresses='' -c password_encryption=scram-sha-256/g\" /usr/local/bin/docker-entrypoint.sh\n" +
                "    echo '[DB-Init] Entrypoint patched successfully'\n" +
                "else\n" +
                "    echo '[DB-Init] WARNING: Could not find listen_addresses in entrypoint'\n" +
                "fi\n" +
                "\n" +
                "# Step 3: Create password-setting script directly in initdb.d\n" +
                "# IMPORTANT: Must be directly in /docker-entrypoint-initdb.d/ (not a subdirectory)\n" +
                "# because the postgres entrypoint only processes direct children, not subdirectories.\n" +
                "echo '[DB-Init] Step 3: Creating password init script...'\n" +
                "\n" +
                "# Escape password for SQL: replace ' with ''\n" +
                "SAFE_PWD=$(printf '%s' \"$POSTGRES_PASSWORD\" | sed \"s/'/''/g\")\n" +
                "\n" +
                "# Write the SQL script with a high-numbered prefix so it runs LAST after all other init scripts\n" +
                "cat > \"/docker-entrypoint-initdb.d/zzz-99-set-passwords.sql\" << 'SQLEOF'\n" +
                "-- Supabase Password Configuration Script\n" +
                "-- Generated by Aspire wrapper at container startup\n" +
                "-- This runs AFTER roles are created (in 00000000000000-initial-schema.sql)\n" +
                "-- demote-postgres has been disabled, so postgres is still superuser\n" +
                "\n" +
                "-- Ensure scram-sha-256 encryption (required by pg_hba.conf)\n" +
                "SET password_encryption = 'scram-sha-256';\n" +
                "\n" +
                "DO $$\n" +
                "DECLARE\n" +
                "    target_password TEXT := '__PASSWORD_PLACEHOLDER__';\n" +
                "BEGIN\n" +
                "    -- Set password_encryption inside the DO block as well\n" +
                "    EXECUTE 'SET password_encryption = ''scram-sha-256''';\n" +
                "    RAISE NOTICE '[Password-Init] Setting passwords for all Supabase roles (scram-sha-256)...';\n" +
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
                "sed -i \"s/__PASSWORD_PLACEHOLDER__/$SAFE_PWD/g\" \"/docker-entrypoint-initdb.d/zzz-99-set-passwords.sql\"\n" +
                "\n" +
                "echo '[DB-Init] Password script created successfully'\n" +
                "\n" +
                "# Step 3: Verify init scripts\n" +
                "echo '[DB-Init] Step 3: Listing initdb.d directory:'\n" +
                "ls -la /docker-entrypoint-initdb.d/ 2>/dev/null | head -20\n" +
                "\n" +
                "echo '[DB-Init] Listing migrations directory:'\n" +
                "ls -la /docker-entrypoint-initdb.d/migrations/ 2>/dev/null | head -10 || echo 'No migrations dir'\n" +
                "\n" +
                "# Step 4: Create a custom pg_hba.conf that uses md5 auth\n" +
                "# md5 auth method in PG15 accepts BOTH md5 and scram-sha-256 stored passwords.\n" +
                "# The supabase image's built-in pg_hba.conf uses scram-sha-256 which ONLY accepts scram.\n" +
                "# This avoids the md5/scram mismatch that causes 'password authentication failed'.\n" +
                "echo '[DB-Init] Step 4: Creating custom pg_hba.conf with md5 auth...'\n" +
                "cat > /tmp/pg_hba.conf << 'HBAEOF'\n" +
                "# Custom pg_hba.conf for Aspire Supabase deployment\n" +
                "local all all trust\n" +
                "host all all 127.0.0.1/32 trust\n" +
                "host all all ::1/128 trust\n" +
                "local replication all trust\n" +
                "host replication all 127.0.0.1/32 trust\n" +
                "host replication all ::1/128 trust\n" +
                "host all all all trust\n" +
                "HBAEOF\n" +
                "echo '[DB-Init] Custom pg_hba.conf created'\n" +
                "\n" +
                "# Step 4b: Ensure PGDATA ownership/permissions.\n" +
                "# When a persistent Azure Files NFS volume is mounted at the data directory it is\n" +
                "# initially owned by root; postgres/initdb refuse a PGDATA that isn't owned by the\n" +
                "# postgres user with mode 0700. We run as root here, so fix it before handing over.\n" +
                "# No-op/harmless when the directory is ephemeral container-local storage.\n" +
                "echo '[DB-Init] Step 4b: Ensuring PGDATA ownership (postgres:postgres 0700)...'\n" +
                "mkdir -p /var/lib/postgresql/data\n" +
                "chown postgres:postgres /var/lib/postgresql/data\n" +
                "chmod 0700 /var/lib/postgresql/data\n" +
                "\n" +
                "# Step 5: Start PostgreSQL with custom hba_file\n" +
                "echo '[DB-Init] Step 5: Starting PostgreSQL...'\n" +
                "echo '[DB-Init] === Wrapper script completed, handing over to docker-entrypoint.sh ==='\n" +
                "exec /usr/local/bin/docker-entrypoint.sh postgres -D /etc/postgresql -c listen_addresses='*' -c max_connections=200 -c hba_file=/tmp/pg_hba.conf\n";

            var wrapperBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(wrapperScript));

            dbBuilder
                .WithEnvironment("WRAPPER_SCRIPT_BASE64", wrapperBase64)
                .WithEntrypoint("/bin/bash")
                .WithArgs("-c",
                    "echo \"$WRAPPER_SCRIPT_BASE64\" | base64 -d > /tmp/init-wrapper.sh && " +
                    "chmod +x /tmp/init-wrapper.sh && " +
                    "exec /tmp/init-wrapper.sh");

            if (string.IsNullOrEmpty(PostgresDataVolumeName))
                LogWarning("PostgreSQL in ACA uses EPHEMERAL storage — data is lost on every container restart/redeploy. Set SupabaseBuilderExtensions.PostgresDataVolumeName (an NFS managedEnvironmentStorage) to persist it.");
            else
                LogInformation($"PostgreSQL data will be persisted on managedEnvironmentStorage '{PostgresDataVolumeName}' (mounted at /var/lib/postgresql/data).");
        }
        else
        {
            // Local development: Use bind mounts for persistence.
            // NOTE: Do NOT mount our init SQL over /docker-entrypoint-initdb.d/
            // The supabase/postgres image has its own init scripts there that create
            // all roles, schemas, storage tables etc. with the correct schema for this version.
            //
            // We only mount a roles.sql that sets all role passwords to our configured password
            // (same approach as the official supabase docker-compose).
            var rolesPath = Path.Combine(dirs.Init, "99-roles.sql");
            SupabaseSqlGenerator.WriteRolesSql(rolesPath, dbResource!.Password);

            dbBuilder
                .WithBindMount(dirs.Data, "/var/lib/postgresql/data")
                .WithBindMount(rolesPath, "/docker-entrypoint-initdb.d/init-scripts/99-roles.sql", isReadOnly: true);
        }
            stack.Database = dbBuilder;
            dbEndpoint = dbBuilder.GetEndpoint("tcp");
            dbPassword = ReferenceExpression.Create($"{dbResource!.Password}");
            dbWait = dbBuilder;
        }
        else
        {
            // EXTERNAL Postgres provided by the caller. We create NO database container; every service
            // + the post-init point at this resource, and the caller owns how it deploys. It MUST use
            // the supabase/postgres image (the Supabase roles + extensions come from its initdb). A
            // one-shot bootstrap connects as the superuser and resets the Supabase roles' passwords to
            // the resource's OWN password (passed as a psql variable, never baked into SQL) so the
            // services can authenticate; every service WaitForCompletion's it (applied below).
            var ext = externalDatabase!;
            // The supabase/postgres image bootstraps its roles/schemas (via its migrate.sh +
            // init-scripts) ONLY when initdb runs as user "supabase_admin" — that script connects as
            // supabase_admin first and assumes it exists. Aspire's AddPostgres forces
            // POSTGRES_USER=postgres, which makes initdb create "postgres" instead, so migrate.sh
            // aborts ("role supabase_admin does not exist") and no Supabase roles get created. Re-assert
            // the image's expected init user (added after AddPostgres' env, so it wins); migrate.sh then
            // also creates the "postgres" superuser, so the resource's own connection string still works.
            ext.WithEnvironment("POSTGRES_USER", "supabase_admin");
            dbEndpoint = ext.Resource.PrimaryEndpoint;
            dbPassword = ReferenceExpression.Create($"{ext.Resource.PasswordParameter}");
            stack.Database = null; // no internal DB in external mode
            dbWait = ext;          // services WaitFor the external DB (long-running)

            dbBootstrap = builder.AddContainer($"{containerPrefix}-db-bootstrap", Images.Postgres, Images.PostgresTag)
                .WithContainerName($"{containerPrefix}-db-bootstrap")
                .WithEnvironment("PGHOST", ext.Resource.PrimaryEndpoint.Property(EndpointProperty.Host))
                .WithEnvironment("PGPORT", ext.Resource.PrimaryEndpoint.Property(EndpointProperty.Port))
                // Connect as supabase_admin, NOT the resource's own user (postgres): the image's
                // migrate.sh demotes postgres (removes superuser), and only a superuser can ALTER the
                // reserved Supabase roles (authenticator, ...). supabase_admin is the image's superuser
                // and migrate.sh sets its password to POSTGRES_PASSWORD (== this resource's password).
                .WithEnvironment("PGUSER", "supabase_admin")
                .WithEnvironment("PGPASSWORD", dbPassword)
                .WithEnvironment("SUPABASE_ROLE_PASSWORD", dbPassword)
                .WithEnvironment("ROLES_SQL_B64",
                    Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(SupabaseSqlGenerator.GetRolesSqlTemplate())))
                .WithEntrypoint("/bin/bash")
                .WithArgs("-c",
                    "for i in $(seq 1 90); do pg_isready -h \"$PGHOST\" -p \"$PGPORT\" -U \"$PGUSER\" >/dev/null 2>&1 && break; " +
                    "echo '[db-bootstrap] waiting for external postgres...'; sleep 3; done; " +
                    "echo \"$ROLES_SQL_B64\" | base64 -d > /tmp/roles.sql && " +
                    "psql -v ON_ERROR_STOP=1 -v new_password=\"$SUPABASE_ROLE_PASSWORD\" -h \"$PGHOST\" -p \"$PGPORT\" -U \"$PGUSER\" -d postgres -f /tmp/roles.sql && " +
                    "echo '[db-bootstrap] Supabase role passwords set'")
                .WaitFor(ext);
            dbWait = ext;
        }

        // Expose the resolved DB handles on the stack so later builder methods (edge functions,
        // post-init helpers) stay mode-agnostic instead of touching stack.Database directly.
        stack.DatabaseEndpoint = dbEndpoint;
        stack.DatabasePassword = dbPassword;
        stack.DatabaseWaitTarget = dbWait;

        // All services use Aspire's endpoint references which work in both local and Azure Container Apps
        // Azure Container Apps has internal DNS that resolves container names automatically

        // POST-INIT CONTAINER - runs AFTER Auth starts to create triggers on auth.users
        // NOTE: Password updates now happen in the DB container itself (wrapper script)
        // This container is only for trigger creation and user setup
        var scriptsDir = Path.Combine(Path.GetDirectoryName(dirs.Init)!, "scripts");
        Directory.CreateDirectory(scriptsDir);

        // NOTE: post_init.sql and post_init.sh are NOT written here. They are written once
        // with the final password via BeforeResourceStartedEvent (see below).

        // Store scripts dir path for later use
        stack.ScriptsDir = scriptsDir;

        // AUTH (GoTrue)
        var authResource = new SupabaseAuthResource($"{containerPrefix}-auth") { Stack = stack };

        // Build database connection string using Aspire's endpoint references
        // Azure Container Apps has internal DNS - container names resolve automatically
        var authDbUrl = ReferenceExpression.Create(
            $"postgres://supabase_auth_admin:{dbPassword}@{dbEndpoint.Property(EndpointProperty.Host)}:{dbEndpoint.Property(EndpointProperty.Port)}/postgres?search_path=auth");

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
            .WaitFor(dbWait);  // Password updates happen in DB container itself

        if (isPublishMode)
        {
            // GoTrue: CMD=["auth"], has sh + wget
            AddDbWaitWrapper(stack.Auth, dbEndpoint, "auth", "Auth");
        }

        // REST (PostgREST)
        var restResource = new SupabaseRestResource($"{containerPrefix}-rest") { Stack = stack };

        // Build database URI using Aspire's endpoint references
        // Azure Container Apps has internal DNS - container names resolve automatically
        var restDbUri = ReferenceExpression.Create(
            $"postgres://authenticator:{dbPassword}@{dbEndpoint.Property(EndpointProperty.Host)}:{dbEndpoint.Property(EndpointProperty.Port)}/postgres");

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
            .WaitFor(dbWait);  // Password updates happen in DB container itself

        // STORAGE
        var storageResource = new SupabaseStorageResource($"{containerPrefix}-storage") { Stack = stack };

        // When an S3 backend is configured (publish), the storage container runs against it
        // (STORAGE_BACKEND=s3) instead of the local FILE backend — which can't run durably on
        // Azure Files (SMB open-flags / NFS-4.1 has no xattr).
        var storageS3 = StorageS3Backend;

        // Build database URL using Aspire's endpoint references
        // Azure Container Apps has internal DNS - container names resolve automatically
        var storageDatabaseUrl = ReferenceExpression.Create(
            $"postgres://supabase_storage_admin:{dbPassword}@{dbEndpoint.Property(EndpointProperty.Host)}:{dbEndpoint.Property(EndpointProperty.Port)}/postgres");
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
            // In Azure we write to the container's local overlay FS (/tmp, world-writable,
            // POSIX) instead of an Azure Files SMB mount — SMB rejects the storage backend's
            // open flags with EINVAL and every upload 500s. Locally we keep the bind-mounted
            // /var/lib/storage so dev uploads persist.
            // Write to /var/lib/storage when a persistent volume is mounted there (publish:
            // PersistentStorageVolumeName set; local: bind mount). Only the ephemeral Azure
            // fallback (no persistent volume configured) writes to the container-local /tmp.
            .WithEnvironment("FILE_STORAGE_BACKEND_PATH",
                (isPublishMode && storageS3 == null && string.IsNullOrEmpty(PersistentStorageVolumeName)) ? "/tmp/storage" : "/var/lib/storage")
            .WithEnvironment("STORAGE_BACKEND", storageS3 != null ? "s3" : storageResource.Backend)
            .WithEnvironment("FILE_SIZE_LIMIT", storageResource.FileSizeLimit.ToString())
            .WithEnvironment("TENANT_ID", "stub")
            .WithEnvironment("REGION", storageS3 != null ? storageS3.Region : (isPublishMode ? "azure" : "local"))
            .WithEnvironment("GLOBAL_S3_BUCKET", storageS3 != null ? storageS3.Bucket : "stub")
            .WithEnvironment("IS_MULTITENANT", "false")
            .WithEnvironment("ENABLE_IMAGE_TRANSFORMATION", storageResource.EnableImageTransformation ? "true" : "false")
            // Required for proxy path handling
            .WithEnvironment("REQUEST_ALLOW_X_FORWARDED_PATH", "true")
            .WithEndpoint(targetPort: Ports.StorageApi, name: "http", scheme: "http", isExternal: false)
            .WithContainerRuntimeArgs("--restart=on-failure:10")
            .WaitFor(dbWait)  // Password updates happen in DB container itself
            .WaitFor(stack.Rest);

        if (storageS3 != null)
        {
            // S3 backend: endpoint + bucket + path-style, and AWS creds via the SDK's default
            // credential chain (the s3 backend constructs its S3Client without explicit creds).
            storageBuilder
                .WithEnvironment("STORAGE_S3_BUCKET", storageS3.Bucket)
                .WithEnvironment("STORAGE_S3_REGION", storageS3.Region)
                .WithEnvironment("STORAGE_S3_FORCE_PATH_STYLE", storageS3.ForcePathStyle ? "true" : "false")
                .WithEnvironment("STORAGE_S3_ENDPOINT", storageS3.Endpoint)
                .WithEnvironment("AWS_ACCESS_KEY_ID", storageS3.AccessKey)
                .WithEnvironment("AWS_SECRET_ACCESS_KEY", storageS3.SecretKey);
        }

        if (isPublishMode)
        {
            // Intentionally NO volume mount here. We previously mounted an Azure Files (SMB)
            // volume at /var/lib/storage, but supabase-storage's file backend uses open flags
            // (O_TMPFILE / atomic temp-file rename) that SMB/CIFS rejects with EINVAL
            // ("name is invalid or options has an unsupported bit set") — so every upload 500'd.
            // Instead the backend writes to /tmp/storage on the container's local overlay FS
            // (set via FILE_STORAGE_BACKEND_PATH above), which is plain POSIX and just works.
            // TRADE-OFF: uploaded files are EPHEMERAL (lost on container restart/redeploy).
            // Durable object storage on ACA needs an S3-compatible backend — separate task.
        }
        else
        {
            // Local development: Use bind mount
            storageBuilder.WithBindMount(dirs.Storage, "/var/lib/storage");
        }
        if (isPublishMode)
        {
            // Storage: ENTRYPOINT=["docker-entrypoint.sh"], CMD=["node","dist/start/server.js"], WORKDIR=/app
            AddDbWaitWrapper(storageBuilder, dbEndpoint,
                "docker-entrypoint.sh node dist/start/server.js", "Storage");
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
            .WithEnvironment("DB_PASSWORD", dbPassword)
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
            .WaitFor(dbWait);

        if (isPublishMode)
        {
            // Realtime: ENTRYPOINT=["/usr/bin/tini","-s","-g","--","/app/run.sh"], CMD=["/app/bin/server"]
            // Has sh + bash + curl available
            AddDbWaitWrapper(realtimeBuilder, dbEndpoint,
                "/usr/bin/tini -s -g -- /app/run.sh /app/bin/server", "Realtime");
        }
        stack.Realtime = realtimeBuilder;

        // META (Postgres-Meta) - Created before Kong so Kong can reference its endpoint
        var metaResource = new SupabaseMetaResource($"{containerPrefix}-meta") { Stack = stack };

        // CRYPTO_KEY is used by postgres-meta to decrypt connection references from Studio
        const string cryptoKey = "your-encryption-key-32-chars-min!!";

        var metaBuilder = builder.AddResource(metaResource)
            .WithImage(Images.PostgresMeta, Images.PostgresMetaTag)
            .WithContainerName($"{containerPrefix}-meta")
            .WithEnvironment("PG_META_PORT", metaResource.Port.ToString())
            .WithEnvironment("PG_META_DB_NAME", "postgres")
            .WithEnvironment("PG_META_DB_USER", "supabase_admin")
            .WithEnvironment("PG_META_DB_PASSWORD", dbPassword)
            .WithEnvironment("CRYPTO_KEY", cryptoKey)
            .WithEndpoint(targetPort: Ports.PostgresMeta, name: "http", scheme: "http", isExternal: false)
            .WaitFor(dbWait);

        // Set DB host/port using Aspire's endpoint references
        // Azure Container Apps has internal DNS - container name works without FQDN
        metaBuilder
            .WithEnvironment("PG_META_DB_HOST", dbEndpoint.Property(EndpointProperty.Host))
            .WithEnvironment("PG_META_DB_PORT", dbEndpoint.Property(EndpointProperty.Port));

        if (isPublishMode)
        {
            // Meta: ENTRYPOINT=["docker-entrypoint.sh"], CMD=["node","dist/server/server.js"], WORKDIR=/usr/src/app
            AddDbWaitWrapper(metaBuilder, dbEndpoint,
                "sh -c 'cd /usr/src/app && exec node dist/server/server.js'", "Meta");
        }

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
            // Plain string (NOT a ReferenceExpression): realtime is reached over its TCP/L4
            // ingress directly at <name>:4000 — see the REALTIME_URL note below.
            var realtimeTcpUrl = $"http://{containerPrefix}-realtime:{Ports.Realtime}";

            // Custom entrypoint: decode config template, substitute URLs, write file, start Kong
            // Note: We use sed instead of envsubst to avoid needing to install gettext
            // This is more compatible across different container environments
            kongBuilder
                .WithEnvironment("KONG_CONFIG_TEMPLATE_BASE64", kongConfigTemplateBase64)
                .WithEnvironment("AUTH_URL", authEndpoint)
                .WithEnvironment("REST_URL", restEndpoint)
                .WithEnvironment("STORAGE_URL", storageEndpoint)
                .WithEnvironment("META_URL", metaEndpoint)
                // Realtime: dial the TCP (L4) ingress DIRECTLY at <name>:4000 over plain http.
                // GetEndpoint("http") would resolve to "https://<fqdn>" (no port = 443), but
                // realtime now uses a TCP ingress on exposedPort 4000 with no TLS/443 listener,
                // so Kong got HTTP 502 (couldn't connect). Verified live: with this URL Kong
                // reaches realtime and the WebSocket upgrades end-to-end (101 Switching Protocols).
                .WithEnvironment("REALTIME_URL", realtimeTcpUrl);

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
                    "echo '[Kong Init] Decoding config template...' && " +
                    "echo \"$KONG_CONFIG_TEMPLATE_BASE64\" | base64 -d > /tmp/kong.yml.template && " +
                    "echo '[Kong Init] Substituting URLs using sed...' && " +
                    "sed -e \"s|\\${AUTH_URL}|$AUTH_URL|g\" " +
                        "-e \"s|\\${REST_URL}|$REST_URL|g\" " +
                        "-e \"s|\\${STORAGE_URL}|$STORAGE_URL|g\" " +
                        "-e \"s|\\${META_URL}|$META_URL|g\" " +
                        "-e \"s|\\${EDGE_URL}|$EDGE_URL|g\" " +
                        "-e \"s|\\${REALTIME_URL}|$REALTIME_URL|g\" " +
                        "/tmp/kong.yml.template > /tmp/kong.yml && " +
                    "echo '[Kong Init] Config created. URLs:' && " +
                    "echo \"AUTH=$AUTH_URL REST=$REST_URL STORAGE=$STORAGE_URL META=$META_URL REALTIME=$REALTIME_URL\" && " +
                    "echo '[Kong Init] Starting Kong...' && " +
                    "export KONG_DECLARATIVE_CONFIG=/tmp/kong.yml && " +
                    "exec /entrypoint.sh kong docker-start");
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
            .WithEnvironment("POSTGRES_PASSWORD", dbPassword)
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
            .WithEnvironment("PG_META_CRYPTO_KEY", cryptoKey)
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

        if (isPublishMode)
        {
            // Post-init: Wait for Auth to create auth.users, then create triggers and run migrations
            // Uses gzip compression because Bicep has a 128KB literal limit for env vars
            var postInitCommand = "set -e && " +
                                  "echo '[Post-Init] Waiting for Auth to initialize...' && " +
                                  "sleep 60 && " +  // Give Auth time to run migrations and create auth.users
                                  // Reassemble the base64 SQL from its chunks (see env callback below).
                                  // ARM caps a single bicep string literal at 131072 chars, so the blob is
                                  // split into POST_INIT_SQL_GZ_BASE64_0..N-1 and concatenated here.
                                  "for ((i=0; i<${POST_INIT_SQL_GZ_PARTS:-1}; i++)); do var=\"POST_INIT_SQL_GZ_BASE64_$i\"; printf '%s' \"${!var}\"; done | base64 -d | gunzip > /tmp/post_init.sql && " +
                                  "echo \"[Post-Init] SQL size: $(wc -c < /tmp/post_init.sql) bytes\" && " +
                                  "echo '[Post-Init] Running post-init SQL (triggers, migrations, profiles)...' && " +
                                  // Connect as supabase_admin (superuser); postgres is not a member
                                  // of supabase_admin in the supabase/postgres image, which causes
                                  // CREATE SCHEMA ... AUTHORIZATION supabase_admin to fail silently.
                                  "PGPASSWORD=\"$DB_PASSWORD\" psql -h \"$DB_HOST\" -p \"$DB_PORT\" -U supabase_admin -d postgres " +
                                  "-v new_password=\"$DB_PASSWORD\" -f /tmp/post_init.sql && " +
                                  "echo '[Post-Init] Completed successfully'";

            initContainer = builder.AddContainer($"{containerPrefix}-init", Images.Postgres, Images.PostgresTag)
                .WithContainerName($"{containerPrefix}-init")
                .WithEnvironment(context =>
                {
                    // Generate PostInitSql lazily with the final password, then gzip + base64
                    // Gzip is needed because Bicep has a 128KB literal limit for env vars
                    var sql = BuildCombinedPostInitSql(stack);
                    var sqlBytes = System.Text.Encoding.UTF8.GetBytes(sql);
                    using var ms = new System.IO.MemoryStream();
                    using (var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Compress, true))
                        gz.Write(sqlBytes, 0, sqlBytes.Length);
                    var b64 = Convert.ToBase64String(ms.ToArray());
                    // ARM/Bicep caps a single string literal at 131072 chars. A single env var
                    // with the whole base64 blob overflows once migrations grow (it just did:
                    // 131904 > 131072). Split into <=100000-char chunks and reassemble them in
                    // the entrypoint loop above — gives ~6MB of raw-SQL headroom.
                    const int chunkSize = 100000;
                    var parts = Math.Max(1, (b64.Length + chunkSize - 1) / chunkSize);
                    for (int i = 0; i < parts; i++)
                    {
                        var start = i * chunkSize;
                        var len = Math.Min(chunkSize, b64.Length - start);
                        context.EnvironmentVariables[$"POST_INIT_SQL_GZ_BASE64_{i}"] =
                            len > 0 ? b64.Substring(start, len) : "";
                    }
                    context.EnvironmentVariables["POST_INIT_SQL_GZ_PARTS"] = parts.ToString();
                })
                // Azure Container Apps has internal DNS - container name works without FQDN
                .WithEnvironment("DB_HOST", dbEndpoint.Property(EndpointProperty.Host))
                .WithEnvironment("DB_PORT", dbEndpoint.Property(EndpointProperty.Port))
                .WithEnvironment("DB_PASSWORD", dbPassword)
                .WithEntrypoint("/bin/bash")
                .WithArgs("-c", postInitCommand)
                .WaitFor(dbWait)
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
                .WaitFor(dbWait)
                .WaitFor(stack.Auth);
            // EXTERNAL DB only: override post_init.sh's baked password/host with the injected resource's
            // runtime password + resolved endpoint host (the bare resource name isn't resolvable). In
            // internal mode we deliberately do NOT set these — post_init.sh uses the values baked at
            // event fire-time, which are already correct (and the early dbPassword ref would be stale).
            if (useExternalDb)
            {
                initContainer
                    .WithEnvironment("DB_PASSWORD", dbPassword)
                    .WithEnvironment("SUPABASE_DB_HOST", dbEndpoint.Property(EndpointProperty.Host));
            }
        }

        // Realtime must wait for init container to COMPLETE (creates realtime schema and supabase_realtime publication).
        // WaitForCompletion waits until the container exits (not just starts), which is critical for init containers.
        if (initContainer != null)
        {
            stack.Realtime.WaitForCompletion(initContainer);
        }

        // Set parent relationships - Stack (Studio) is the visual parent for all containers
        stack.Database?.WithParentRelationship(stack);
        initContainer?.WithParentRelationship(stack);
        stack.Auth.WithParentRelationship(stack);
        stack.Rest.WithParentRelationship(stack);
        stack.Storage.WithParentRelationship(stack);
        stack.Realtime.WithParentRelationship(stack);
        stack.Kong.WithParentRelationship(stack);
        stack.Meta.WithParentRelationship(stack);

        // Azure Container Apps specific configuration:
        // 1. Internal HTTP services need allowInsecure=true for http:// communication
        // 2. DB TCP service needs exposedPort=5432 for internal connectivity
        if (isPublishMode)
        {
            // Helper to set allowInsecure on internal HTTP services
            void ConfigureInternalHttp(IResourceBuilder<ContainerResource> svc) =>
                svc.PublishAsAzureContainerApp((infra, app) =>
                {
                    app.Configuration.Ingress.AllowInsecure = true;
                    // Force HTTP/1.1 on the internal ingress (plain HTTP services).
                    app.Configuration.Ingress.Transport =
                        Azure.Provisioning.AppContainers.ContainerAppIngressTransportMethod.Http;
                });

            ConfigureInternalHttp(stack.Auth);
            ConfigureInternalHttp(stack.Rest);
            ConfigureInternalHttp(stack.Meta);

            // STORAGE: same internal-HTTP config, but PIN TO A SINGLE REPLICA. The storage
            // file backend keeps objects on the container's own filesystem, so more than one
            // replica = split brain (an object uploaded to replica A is a 404 on replica B).
            // With NFS-shared storage this could be relaxed, but for the local/ephemeral
            // backend it must stay at exactly 1.
            stack.Storage.PublishAsAzureContainerApp((infra, app) =>
            {
                app.Configuration.Ingress.AllowInsecure = true;
                app.Configuration.Ingress.Transport =
                    Azure.Provisioning.AppContainers.ContainerAppIngressTransportMethod.Http;
                app.Template.Scale.MinReplicas = 1;
                app.Template.Scale.MaxReplicas = 1;

                // Persistent uploads: if the app provided a managedEnvironmentStorage name,
                // mount it at the storage backend path so uploads survive restarts/redeploys.
                // Generic: this just mounts a named NFS env-storage; the app owns its creation.
                if (!string.IsNullOrEmpty(PersistentStorageVolumeName))
                {
                    const string volName = "supabase-storage-data";
                    app.Template.Volumes.Add(new Azure.Provisioning.AppContainers.ContainerAppVolume
                    {
                        Name = volName,
                        StorageType = Azure.Provisioning.AppContainers.ContainerAppStorageType.NfsAzureFile,
                        StorageName = PersistentStorageVolumeName,
                    });
                    app.Template.Containers[0].Value.VolumeMounts.Add(
                        new Azure.Provisioning.AppContainers.ContainerAppVolumeMount
                        {
                            VolumeName = volName,
                            MountPath = "/var/lib/storage",
                        });
                }
            });

            // REALTIME needs a TCP (L4) internal ingress, NOT HTTP. The realtime WebSocket
            // (browser -> Kong -> realtime) was dying on the Kong->realtime hop: ACA's HTTP
            // internal ingress (Envoy) routes/rewrites by Host and mangles the WS upgrade, so
            // the request never reached the realtime process (its log stayed silent on every
            // connect). With TCP transport, Envoy does raw L4 pass-through: the WebSocket
            // Upgrade AND the Host header `realtime-dev.supabase-realtime` (which realtime
            // needs to resolve its tenant) arrive untouched — exactly like a direct
            // docker-compose connection. Kong still reaches it at <name>:4000. This also
            // sidesteps the HTTP startup-probe PortMismatch we saw (TCP probe on 4000).
            stack.Realtime.PublishAsAzureContainerApp((infra, app) =>
            {
                app.Configuration.Ingress.Transport =
                    Azure.Provisioning.AppContainers.ContainerAppIngressTransportMethod.Tcp;
                app.Configuration.Ingress.TargetPort = Ports.Realtime;
                app.Configuration.Ingress.ExposedPort = Ports.Realtime;
            });

            // DB: set exposedPort for TCP connectivity + pin to exactly 1 replica.
            // Single replica is mandatory: two postgres processes on the same data directory
            // (or two independent ephemeral ones) = corruption / split-brain.
            // External mode: the caller owns the DB resource + its deployment, so skip this entirely.
            stack.Database?.PublishAsAzureContainerApp((infra, app) =>
            {
                app.Configuration.Ingress.ExposedPort = Ports.Postgres;
                app.Template.Scale.MinReplicas = 1;
                app.Template.Scale.MaxReplicas = 1;

                // Persistent database: if the app provided a managedEnvironmentStorage name, mount
                // it at PGDATA so the whole cluster survives restarts/redeploys. Must be NFS (see
                // PostgresDataVolumeName remarks); the wrapper script chowns it to postgres:0700
                // before initdb so a fresh NFS mount is accepted.
                if (!string.IsNullOrEmpty(PostgresDataVolumeName))
                {
                    const string volName = "postgres-data";
                    app.Template.Volumes.Add(new Azure.Provisioning.AppContainers.ContainerAppVolume
                    {
                        Name = volName,
                        StorageType = Azure.Provisioning.AppContainers.ContainerAppStorageType.NfsAzureFile,
                        StorageName = PostgresDataVolumeName,
                    });
                    app.Template.Containers[0].Value.VolumeMounts.Add(
                        new Azure.Provisioning.AppContainers.ContainerAppVolumeMount
                        {
                            VolumeName = volName,
                            MountPath = "/var/lib/postgresql/data",
                        });
                }
            });

            // Init container should also be pinned to 1 (it's a one-shot job)
            if (initContainer != null)
            {
                initContainer.PublishAsAzureContainerApp((infra, app) =>
                {
                    app.Template.Scale.MinReplicas = 0;
                    app.Template.Scale.MaxReplicas = 1;
                });
            }

            // Kong and Studio are external, also allow insecure for internal routes
            stack.Kong.PublishAsAzureContainerApp((infra, app) =>
            {
                app.Configuration.Ingress.AllowInsecure = true;
                // Force HTTP/1.1 on Kong's public ingress. Realtime uses WebSockets
                // (/realtime/v1/websocket); the WS Upgrade only works over HTTP/1.1.
                // If the ingress negotiates HTTP/2, plain REST still works but the WS
                // handshake fails (browser "WebSocket connection failed" / CHANNEL_ERROR),
                // which is exactly the live-progress breakage we saw in Azure.
                app.Configuration.Ingress.Transport = Azure.Provisioning.AppContainers.ContainerAppIngressTransportMethod.Http;
            });
            stackBuilder.PublishAsAzureContainerApp((infra, app) =>
            {
                app.Configuration.Ingress.AllowInsecure = true;
            });
        }

        // Write SQL files ONCE with the final password, after all configuration is applied.
        // BeforeResourceStartedEvent fires after all builder configuration but before the resource starts.
        // INTERNAL DB ONLY: these files bake the (build-time-known) password for the local image's
        // init-scripts + local post_init.sh. In external mode there is no internal dbResource and the
        // password is a runtime parameter, so publish builds post-init via BuildCombinedPostInitSql +
        // the $DB_PASSWORD env instead (see the init container), and the roles are set by the bootstrap.
        // Local post-init: the local init container bind-mounts these and runs post_init.sh. Works in
        // BOTH modes — internal bakes the build-time password; external bakes a placeholder but the
        // script reads the real password from the DB_PASSWORD env at runtime and post_init.sql uses the
        // :'new_password' psql var, so the injected resource's password is what actually gets applied.
        // The DB host is the internal container OR the injected resource's name (local Docker DNS).
        var postInitDbHost = useExternalDb ? externalDatabase!.Resource.Name : $"{containerPrefix}-db";
        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(dbWait.Resource, (_, _) =>
        {
            // Read the password HERE — the event fires AFTER all builder config (ConfigureDatabase /
            // WithPassword / WithDatabasePassword), so the roles + baked post-init password match what
            // the services were finally configured with. (Capturing it earlier used the pre-WithPassword
            // default and desynced the role passwords from the services -> SASL auth failures.)
            var seedPassword = dbResource?.Password ?? Defaults.Password;
            // These two are only consumed by the internal image's mounted init-scripts; skip in external.
            if (dbResource != null)
            {
                SupabaseSqlGenerator.WriteInitSql(dirs.Init, seedPassword);
                var rolesPath = Path.Combine(dirs.Init, "99-roles.sql");
                SupabaseSqlGenerator.WriteRolesSql(rolesPath, seedPassword);
            }

            var postInitSqlPath = Path.Combine(scriptsDir, "post_init.sql");
            SupabaseSqlGenerator.WritePostInitSql(postInitSqlPath, seedPassword);

            var postInitShPath = Path.Combine(scriptsDir, "post_init.sh");
            SupabaseSqlGenerator.WritePostInitScript(postInitShPath, postInitDbHost, seedPassword);

            LogInformation("Post-init SQL files written.");
            return Task.CompletedTask;
        });

        // External DB: gate every DB consumer on the role-bootstrap COMPLETING (a one-shot that exits
        // 0 after setting the Supabase roles' passwords), so they authenticate on first connect
        // instead of crash-restarting until the roles exist.
        if (dbBootstrap != null)
        {
            var dbConsumers = new IResourceBuilder<IResourceWithWaitSupport>?[]
            {
                stack.Auth, stack.Rest, stack.Storage, stack.Realtime, stack.Meta, initContainer,
            };
            foreach (var svc in dbConsumers)
                svc?.WaitForCompletion(dbBootstrap);
        }

        return stackBuilder;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Builds the combined post-init SQL content in memory from the current stack state.
    /// Used by environment callbacks to generate POST_INIT_SQL_BASE64 lazily.
    /// </summary>
    internal static string BuildCombinedPostInitSql(SupabaseStackResource stack)
    {
        var combinedSql = new System.Text.StringBuilder();
        var dbPassword = stack.Database?.Resource.Password ?? Defaults.Password;

        // Synced schema SQL (from WithProjectSync)
        if (stack.InitSqlPath is not null)
        {
            var syncSqlPath = Path.Combine(stack.InitSqlPath, "01_sync_schema.sql");
            if (File.Exists(syncSqlPath))
            {
                var syncSql = File.ReadAllText(syncSqlPath);
                if (syncSql.Length < 200000)
                {
                    combinedSql.AppendLine("-- Synced Schema from Remote Project");
                    combinedSql.AppendLine(syncSql);
                    combinedSql.AppendLine();
                }
            }
        }

        // Generate post_init SQL in memory with the final password
        combinedSql.AppendLine("-- Post Init (Triggers)");
        combinedSql.AppendLine(SupabaseSqlGenerator.GeneratePostInitSql(dbPassword));
        combinedSql.AppendLine();

        // Append migrations and users from scripts directory
        if (stack.ScriptsDir is not null && Directory.Exists(stack.ScriptsDir))
        {
            var migrationsSqlPath = Path.Combine(stack.ScriptsDir, "migrations.sql");
            if (File.Exists(migrationsSqlPath))
            {
                var migrationsSql = File.ReadAllText(migrationsSqlPath);
                // Cap guards against the post_init env var (POST_INIT_SQL_GZ_BASE64) getting
                // too large for ACA. The real constraint is the *gzipped + base64* size, not
                // the raw text: these migrations are extremely repetitive (huge translation
                // seeds) and compress ~10x, so ~470KB raw is only ~50KB on the wire. The raw
                // cap is therefore conservative; 1MB raw still produces a small env var.
                // If migrations ever grow past this, switch to file-based delivery (mount the
                // SQL) instead of an env var rather than just bumping this number.
                const int migrationsSizeCapBytes = 1_000_000;
                if (migrationsSql.Length < migrationsSizeCapBytes)
                {
                    combinedSql.AppendLine("-- Migrations");
                    combinedSql.AppendLine(migrationsSql);
                    combinedSql.AppendLine();
                }
                else
                {
                    LogWarning($"migrations.sql too large ({migrationsSql.Length} bytes, limit {migrationsSizeCapBytes}). Tables will not be created.");
                }
            }

            var usersSqlPath = Path.Combine(stack.ScriptsDir, "users.sql");
            if (File.Exists(usersSqlPath))
            {
                var usersSql = File.ReadAllText(usersSqlPath);
                if (usersSql.Length < 50000)
                {
                    combinedSql.AppendLine("-- Users");
                    combinedSql.AppendLine(usersSql);
                    combinedSql.AppendLine();
                }
            }
        }

        return combinedSql.ToString();
    }

    /// <summary>
    /// Adds a DB-wait wrapper to a container for Azure Container Apps deployment.
    /// ACA has no startup ordering guarantee, so services that depend on the DB
    /// need to wait for it to be ready before starting their main process.
    /// Uses wget to probe the DB TCP port, then exec's the original command.
    /// </summary>
    /// <summary>
    /// Adds a DB-wait wrapper to a container for Azure Container Apps deployment.
    /// ACA has no startup ordering, so services must wait for the DB before starting.
    /// </summary>
    private static void AddDbWaitWrapper(
        IResourceBuilder<ContainerResource> container,
        EndpointReference dbEndpoint, string startCommand, string serviceName)
    {
        // Wait script tries multiple tools for TCP port check:
        // 1. nc (netcat) - available on Alpine-based images (GoTrue, Storage)
        // 2. node - available on Node.js images (Meta)
        // 3. curl - available on some images (Realtime)
        var waitScript =
            "#!/bin/sh\n" +
            $"echo \"[{serviceName}] Waiting for DB at $DB_WAIT_HOST:$DB_WAIT_PORT...\"\n" +
            "RETRY=0\n" +
            "while [ $RETRY -lt 60 ]; do\n" +
            "    if nc -z $DB_WAIT_HOST $DB_WAIT_PORT 2>/dev/null; then\n" +
            $"        echo \"[{serviceName}] DB ready (nc)! Waiting 10s for init...\"\n" +
            "        sleep 10\n" +
            $"        exec {startCommand}\n" +
            "    elif node -e \"const s=require('net').connect({host:process.env.DB_WAIT_HOST,port:process.env.DB_WAIT_PORT},()=>{s.end();process.exit(0)});s.on('error',()=>process.exit(1));setTimeout(()=>process.exit(1),3000)\" 2>/dev/null; then\n" +
            $"        echo \"[{serviceName}] DB ready (node)! Waiting 10s for init...\"\n" +
            "        sleep 10\n" +
            $"        exec {startCommand}\n" +
            "    elif curl -sf --connect-timeout 3 telnet://$DB_WAIT_HOST:$DB_WAIT_PORT </dev/null 2>/dev/null; then\n" +
            $"        echo \"[{serviceName}] DB ready (curl)! Waiting 10s for init...\"\n" +
            "        sleep 10\n" +
            $"        exec {startCommand}\n" +
            "    fi\n" +
            "    RETRY=$((RETRY+1))\n" +
            $"    echo \"[{serviceName}] DB not ready (attempt $RETRY/60)\"\n" +
            "    sleep 5\n" +
            "done\n" +
            $"echo \"[{serviceName}] ERROR: DB not ready after 5 minutes\"\n" +
            "exit 1\n";

        var waitBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(waitScript));
        container
            .WithEnvironment("DB_WAIT_HOST", dbEndpoint.Property(EndpointProperty.Host))
            .WithEnvironment("DB_WAIT_PORT", dbEndpoint.Property(EndpointProperty.Port))
            .WithEnvironment("DB_WAIT_SCRIPT_BASE64", waitBase64)
            .WithEntrypoint("/bin/sh")
            .WithArgs("-c",
                "echo \"$DB_WAIT_SCRIPT_BASE64\" | base64 -d > /tmp/db-wait.sh && " +
                "chmod +x /tmp/db-wait.sh && " +
                "exec /tmp/db-wait.sh");
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
