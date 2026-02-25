using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Helpers;
using Nextended.Aspire.Hosting.Supabase.Resources;
using static Nextended.Aspire.Hosting.Supabase.Helpers.SupabaseLogger;

namespace Nextended.Aspire.Hosting.Supabase.Builders;

/// <summary>
/// Provides extension methods for the SupabaseStackResource.
/// </summary>
public static class SupabaseStackExtensions
{
    #region Edge Functions

    /// <summary>
    /// Configures and creates the Edge Runtime container for Edge Functions.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="functionsPath">The absolute path to the supabase/functions directory.</param>
    public static IResourceBuilder<SupabaseStackResource> WithEdgeFunctions(
        this IResourceBuilder<SupabaseStackResource> builder,
        string functionsPath)
    {
        if (!Directory.Exists(functionsPath))
        {
            LogWarning($"Edge Functions directory not found: {functionsPath}");
            return builder;
        }

        var stack = builder.Resource;
        var appBuilder = stack.AppBuilder;

        if (appBuilder is null)
        {
            LogError("AppBuilder not available. Was AddSupabase() called?");
            return builder;
        }

        // List available functions (only directories with index.ts)
        var functionDirs = Directory.GetDirectories(functionsPath)
            .Select(d => Path.GetFileName(d))
            .Where(name => !name.StartsWith("_") && !name.StartsWith("."))
            .Where(name => File.Exists(Path.Combine(functionsPath, name, "index.ts")))
            .ToList();

        if (functionDirs.Count == 0)
        {
            LogWarning($"No Edge Functions with index.ts found in: {functionsPath}");
            return builder;
        }

        LogInformation($"Edge Functions found: {string.Join(", ", functionDirs)}");

        stack.EdgeFunctionsPath = functionsPath;
        var containerPrefix = stack.Name;

        var dbPassword = stack.Database!.Resource.Password;

        // Get database endpoint for dynamic service discovery (works in ACA and locally)
        var dbEndpoint = stack.Database.GetEndpoint("tcp");

        // Build database URL using Aspire's dynamic endpoint resolution
        var edgeDbUrl = ReferenceExpression.Create(
            $"postgresql://postgres:{dbPassword}@{dbEndpoint.Property(EndpointProperty.Host)}:{dbEndpoint.Property(EndpointProperty.Port)}/postgres");

        // Generate router file for multi-function support
        var infraRoot = stack.InfraRootDir ?? Path.Combine(appBuilder.AppHostDirectory, "..", "infra", "supabase");
        var edgeDir = Path.Combine(infraRoot, "edge");
        Directory.CreateDirectory(edgeDir);

        var mainTsPath = Path.Combine(edgeDir, "main.ts");
        EdgeFunctionRouter.GenerateRouter(mainTsPath, functionDirs);
        LogInformation($"Edge Router generated: {mainTsPath}");

        // Create the Edge Runtime container with typed resource
        const int EdgeRuntimePort = 9000;
        var edgeResource = new SupabaseEdgeRuntimeResource($"{containerPrefix}-edge")
        {
            Port = EdgeRuntimePort,
            FunctionsPath = functionsPath,
            Stack = stack
        };
        edgeResource.FunctionNames.AddRange(functionDirs);

        var isPublishMode = appBuilder.ExecutionContext.IsPublishMode;

        // Build Kong URL using Aspire's endpoint reference
        var edgeSupabaseUrl = stack.Kong!.GetEndpoint("http");

        // Base configuration for Edge Runtime
        var edgeBuilder = appBuilder.AddResource(edgeResource)
            .WithImage("denoland/deno", "alpine-2.1.4")
            .WithContainerName($"{containerPrefix}-edge")
            .WithEnvironment("SUPABASE_URL", edgeSupabaseUrl)
            .WithEnvironment("SUPABASE_ANON_KEY", stack.AnonKey)
            .WithEnvironment("SUPABASE_SERVICE_ROLE_KEY", stack.ServiceRoleKey)
            .WithEnvironment("SUPABASE_DB_URL", edgeDbUrl)
            .WithEnvironment("JWT_SECRET", stack.JwtSecret)
            .WithEnvironment("DENO_DIR", "/tmp/deno")
            .WithEnvironment("EDGE_RUNTIME_PORT", EdgeRuntimePort.ToString())
            .WithEndpoint(targetPort: EdgeRuntimePort, name: "http", scheme: "http", isExternal: false)
            .WaitFor(stack.Database!)
            .WaitFor(stack.Kong!);

        if (isPublishMode)
        {
            // In Azure Container Apps:
            // Init containers are separate Container Apps - they CANNOT share volumes!
            // Solution: Pass function code as environment variables and write them at container startup
            // The Deno container will write the files before starting the runtime

            var edgeMountPath = "/home/deno";

            // Read main.ts content
            var mainTsContent = File.ReadAllText(mainTsPath);
            var mainTsBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(mainTsContent));

            // Build the setup command that writes all files and then starts Deno
            var setupCommands = new List<string>
            {
                $"mkdir -p {edgeMountPath}/main {edgeMountPath}/functions",
                $"echo $MAIN_TS_BASE64 | base64 -d > {edgeMountPath}/main/main.ts"
            };

            // Read all function files and add to environment variables
            foreach (var funcName in functionDirs)
            {
                var funcIndexPath = Path.Combine(functionsPath, funcName, "index.ts");
                if (File.Exists(funcIndexPath))
                {
                    var funcContent = File.ReadAllText(funcIndexPath);
                    var funcBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(funcContent));
                    var envVarName = $"FUNC_{funcName.ToUpperInvariant().Replace("-", "_")}_BASE64";

                    edgeBuilder.WithEnvironment(envVarName, funcBase64);
                    setupCommands.Add($"mkdir -p {edgeMountPath}/functions/{funcName} && echo ${envVarName} | base64 -d > {edgeMountPath}/functions/{funcName}/index.ts");
                }
            }

            setupCommands.Add($"echo 'Edge functions ready:' && find {edgeMountPath} -name '*.ts'");
            setupCommands.Add($"exec deno run --allow-all --unstable-worker-options {edgeMountPath}/main/main.ts");

            var startupCommand = string.Join(" && ", setupCommands);

            // Use /bin/sh to run a startup script that writes files and then starts Deno
            edgeBuilder
                .WithEnvironment("MAIN_TS_BASE64", mainTsBase64)
                .WithEntrypoint("/bin/sh")
                .WithArgs("-c", startupCommand);
        }
        else
        {
            // Local development: Use bind mounts and standard Deno command
            edgeBuilder
                .WithBindMount(edgeDir, "/home/deno/main", isReadOnly: true)
                .WithBindMount(functionsPath, "/home/deno/functions", isReadOnly: true)
                .WithArgs("run", "--allow-all", "--unstable-worker-options", "/home/deno/main/main.ts");
        }
        stack.EdgeRuntime = edgeBuilder;

        // Set parent relationship to the stack (which IS the Studio)
        stack.EdgeRuntime.WithParentRelationship(stack);

        LogInformation($"Edge Runtime container created with {functionDirs.Count} functions");
        return builder;
    }

    #endregion

    #region Migrations

    /// <summary>
    /// Applies database migrations from SQL files in the specified directory.
    /// Migrations are executed in alphabetical order by filename AFTER GoTrue starts.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="migrationsPath">The absolute path to the supabase/migrations directory.</param>
    public static IResourceBuilder<SupabaseStackResource> WithMigrations(
        this IResourceBuilder<SupabaseStackResource> builder,
        string migrationsPath)
    {
        if (!Directory.Exists(migrationsPath))
        {
            LogWarning($"Migrations directory not found: {migrationsPath}");
            return builder;
        }

        var stack = builder.Resource;

        if (stack.InitSqlPath is null)
        {
            LogError("InitSqlPath not set. Was AddSupabase() called?");
            return builder;
        }

        // Find all SQL files and sort by name
        var sqlFiles = Directory.GetFiles(migrationsPath, "*.sql")
            .OrderBy(f => Path.GetFileName(f))
            .ToList();

        if (sqlFiles.Count == 0)
        {
            LogInformation($"No migrations found in: {migrationsPath}");
            return builder;
        }

        LogInformation($"{sqlFiles.Count} migrations found");

        // Create combined migrations file
        var combinedSql = new System.Text.StringBuilder();
        combinedSql.AppendLine("-- ============================================");
        combinedSql.AppendLine("-- SUPABASE MIGRATIONS (auto-generated)");
        combinedSql.AppendLine($"-- Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        combinedSql.AppendLine($"-- Source: {migrationsPath}");
        combinedSql.AppendLine("-- ============================================");
        combinedSql.AppendLine();
        combinedSql.AppendLine("-- Wait for auth.users table (GoTrue must start first)");
        combinedSql.AppendLine("DO $$");
        combinedSql.AppendLine("DECLARE");
        combinedSql.AppendLine("    retry_count integer := 0;");
        combinedSql.AppendLine("    max_retries integer := 30;");
        combinedSql.AppendLine("BEGIN");
        combinedSql.AppendLine("    WHILE NOT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'auth' AND tablename = 'users') AND retry_count < max_retries LOOP");
        combinedSql.AppendLine("        PERFORM pg_sleep(1);");
        combinedSql.AppendLine("        retry_count := retry_count + 1;");
        combinedSql.AppendLine("        RAISE NOTICE '[Migrations] Waiting for auth.users... (Attempt %/%)', retry_count, max_retries;");
        combinedSql.AppendLine("    END LOOP;");
        combinedSql.AppendLine("    IF NOT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'auth' AND tablename = 'users') THEN");
        combinedSql.AppendLine("        RAISE EXCEPTION '[Migrations] auth.users was not found after % attempts', max_retries;");
        combinedSql.AppendLine("    END IF;");
        combinedSql.AppendLine("    RAISE NOTICE '[Migrations] auth.users found, starting migrations';");
        combinedSql.AppendLine("END;");
        combinedSql.AppendLine("$$;");
        combinedSql.AppendLine();

        foreach (var sqlFile in sqlFiles)
        {
            var fileName = Path.GetFileName(sqlFile);
            combinedSql.AppendLine($"-- Migration: {fileName}");
            combinedSql.AppendLine($"-- ----------------------------------------");

            try
            {
                var content = File.ReadAllText(sqlFile);
                combinedSql.AppendLine(content);
                combinedSql.AppendLine();
                LogInformation($"  + {fileName}");
            }
            catch (Exception ex)
            {
                LogWarning($"Could not read {fileName}: {ex.Message}");
            }
        }

        combinedSql.AppendLine("-- ============================================");
        combinedSql.AppendLine("-- END MIGRATIONS");
        combinedSql.AppendLine("-- ============================================");

        // Write to scripts directory (executed by post_init.sh, AFTER GoTrue start)
        var scriptsDir = Path.Combine(Path.GetDirectoryName(stack.InitSqlPath)!, "scripts");
        Directory.CreateDirectory(scriptsDir);
        var migrationsOutputPath = Path.Combine(scriptsDir, "migrations.sql");
        File.WriteAllText(migrationsOutputPath, combinedSql.ToString());

        // For publish mode: Update PostInitSqlBase64 to include migrations
        if (stack.AppBuilder?.ExecutionContext.IsPublishMode == true)
        {
            UpdatePostInitSqlBase64(stack);
        }

        LogInformation($"Migrations written to: {migrationsOutputPath}");
        return builder;
    }

    #endregion

    #region User Registration

    /// <summary>
    /// Registers a development user that will be created on startup.
    /// The user will have a profile and admin role automatically.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="displayName">Optional display name (defaults to email).</param>
    public static IResourceBuilder<SupabaseStackResource> WithRegisteredUser(
        this IResourceBuilder<SupabaseStackResource> builder,
        string email,
        string password,
        string? displayName = null)
    {
        var user = new RegisteredUser(email, password, displayName ?? email);
        builder.Resource.RegisteredUsers.Add(user);

        var scriptsDir = builder.Resource.InitSqlPath != null
            ? Path.Combine(Path.GetDirectoryName(builder.Resource.InitSqlPath)!, "scripts")
            : null;

        if (scriptsDir is not null)
        {
            Directory.CreateDirectory(scriptsDir);
            var userSqlPath = Path.Combine(scriptsDir, "users.sql");
            AppendUserSql(userSqlPath, user);

            // For publish mode: Update PostInitSqlBase64 to include new user
            if (builder.Resource.AppBuilder?.ExecutionContext.IsPublishMode == true)
            {
                UpdatePostInitSqlBase64(builder.Resource);
            }

            LogInformation($"User registered: {email} -> {userSqlPath}");
        }
        else
        {
            LogWarning("InitSqlPath is null, user SQL cannot be created!");
        }

        return builder;
    }

    private static void AppendUserSql(string path, RegisteredUser user)
    {
        var email = user.Email.Replace("'", "''");
        var displayName = user.DisplayName.Replace("'", "''");
        var password = user.Password.Replace("'", "''");

        var appMetaData = @"{""provider"": ""email"", ""providers"": [""email""]}";
        var userMetaData = @"{""display_name"": """ + displayName + @"""}";

        var sql = $"""
-- User: {user.Email}
DO $$
DECLARE
    new_user_id uuid;
    hashed_password text;
BEGIN
    -- Check if user already exists
    SELECT id INTO new_user_id FROM auth.users WHERE email = '{email}';

    IF new_user_id IS NULL THEN
        -- Hash password
        hashed_password := extensions.crypt('{password}', extensions.gen_salt('bf', 10));

        -- Create user in auth.users
        INSERT INTO auth.users (
            instance_id, id, aud, role, email, encrypted_password,
            email_confirmed_at, raw_app_meta_data, raw_user_meta_data,
            created_at, updated_at, confirmation_token, email_change,
            email_change_token_new, recovery_token
        ) VALUES (
            '00000000-0000-0000-0000-000000000000',
            extensions.uuid_generate_v4(),
            'authenticated', 'authenticated', '{email}', hashed_password,
            NOW(), '{appMetaData}'::jsonb, '{userMetaData}'::jsonb,
            NOW(), NOW(), '', '', '', ''
        )
        RETURNING id INTO new_user_id;

        RAISE NOTICE '[Post-Init] User created: {email} (ID: %)', new_user_id;

        -- Create profile (with exception handling)
        BEGIN
            IF EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'profiles') THEN
                IF NOT EXISTS (SELECT 1 FROM public.profiles WHERE user_id = new_user_id) THEN
                    INSERT INTO public.profiles (user_id, email, display_name, is_disabled, created_at, updated_at)
                    VALUES (new_user_id, '{email}', '{displayName}', false, NOW(), NOW());
                END IF;
            END IF;
        EXCEPTION WHEN OTHERS THEN
            RAISE WARNING '[Post-Init] Profile creation failed for {email}: %', SQLERRM;
        END;

        -- Create admin role (with exception handling)
        BEGIN
            IF EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'user_roles') THEN
                IF NOT EXISTS (SELECT 1 FROM public.user_roles WHERE user_id = new_user_id) THEN
                    INSERT INTO public.user_roles (user_id, role, created_at)
                    VALUES (new_user_id, 'admin', NOW());
                END IF;
            END IF;
        EXCEPTION WHEN OTHERS THEN
            RAISE WARNING '[Post-Init] Role creation failed for {email}: %', SQLERRM;
        END;
    ELSE
        RAISE NOTICE '[Post-Init] User already exists: {email}';
    END IF;
EXCEPTION WHEN OTHERS THEN
    RAISE WARNING '[Post-Init] User creation completely failed for {email}: %', SQLERRM;
END;
$$;

""";
        File.AppendAllText(path, sql);
    }

    #endregion

    #region Clear Command

    /// <summary>
    /// Adds a "Clear All Data" command to the Kong container in the Aspire dashboard.
    /// This stops all Supabase containers and deletes all data for a fresh start.
    /// </summary>
     public static IResourceBuilder<SupabaseStackResource> WithClearCommand(this IResourceBuilder<SupabaseStackResource> builder)
    {
        var containerPrefix = builder.Resource.Name;
        var infraPath = builder.Resource.InitSqlPath != null
            ? Path.GetDirectoryName(Path.GetDirectoryName(builder.Resource.InitSqlPath))
            : null;
        CommandOptions options = new()
        {
            IconName = "Delete",
            IconVariant = IconVariant.Filled,
            UpdateState = context => ResourceCommandState.Enabled
        };
        builder.WithCommand(
            name: "clear-supabase",
            displayName: "Clear All Supabase Data",
            executeCommand: _ =>
            {
                LogInformation("Deleting all data...");

                var containerNames = new[]
                {
                        $"{containerPrefix}-db",
                        $"{containerPrefix}-auth",
                        $"{containerPrefix}-rest",
                        $"{containerPrefix}-storage",
                        $"realtime-dev.{containerPrefix}-realtime",
                        $"{containerPrefix}-kong",
                        $"{containerPrefix}-meta",
                        $"{containerPrefix}-studio",
                        $"{containerPrefix}-edge",
                        $"{containerPrefix}-init"
                };

                foreach (var container in containerNames)
                {
                    try
                    {
                        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "docker",
                            Arguments = $"rm -f {container}",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                        process?.WaitForExit(10000);
                        LogInformation($"Container removed: {container}");
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"{container} - {ex.Message}");
                    }
                }

                if (!string.IsNullOrEmpty(infraPath) && Directory.Exists(infraPath))
                {
                    try
                    {
                        Directory.Delete(infraPath, recursive: true);
                        LogInformation($"Directory deleted: {infraPath}");
                    }
                    catch (Exception ex)
                    {
                        LogWarning(ex.Message);
                    }
                }

                LogInformation("Cleanup completed. Please restart Aspire.");
                return Task.FromResult(new ExecuteCommandResult() { Success = true});
            }, options
            );

        return builder;
    }

    #endregion

    #region Getters

    /// <summary>
    /// Gets the Kong API Gateway container resource.
    /// </summary>
    public static IResourceBuilder<ContainerResource>? GetKong(this IResourceBuilder<SupabaseStackResource> builder)
        => builder.Resource.Kong;

    /// <summary>
    /// Gets the PostgreSQL Database container resource.
    /// </summary>
    public static IResourceBuilder<ContainerResource>? GetDatabase(this IResourceBuilder<SupabaseStackResource> builder)
        => builder.Resource.Database;

    /// <summary>
    /// Gets the Auth (GoTrue) container resource.
    /// </summary>
    public static IResourceBuilder<ContainerResource>? GetAuth(this IResourceBuilder<SupabaseStackResource> builder)
        => builder.Resource.Auth;

    /// <summary>
    /// Gets the REST (PostgREST) container resource.
    /// </summary>
    public static IResourceBuilder<ContainerResource>? GetRest(this IResourceBuilder<SupabaseStackResource> builder)
        => builder.Resource.Rest;

    /// <summary>
    /// Gets the Storage API container resource.
    /// </summary>
    public static IResourceBuilder<ContainerResource>? GetStorage(this IResourceBuilder<SupabaseStackResource> builder)
        => builder.Resource.Storage;

    /// <summary>
    /// Gets the Postgres-Meta container resource.
    /// </summary>
    public static IResourceBuilder<ContainerResource>? GetMeta(this IResourceBuilder<SupabaseStackResource> builder)
        => builder.Resource.Meta;

    /// <summary>
    /// Gets the Anon Key for client-side authentication.
    /// </summary>
    public static string GetAnonKey(this IResourceBuilder<SupabaseStackResource> builder)
        => builder.Resource.AnonKey;

    /// <summary>
    /// Gets the Service Role Key for server-side authentication.
    /// </summary>
    public static string GetServiceRoleKey(this IResourceBuilder<SupabaseStackResource> builder)
        => builder.Resource.ServiceRoleKey;

    /// <summary>
    /// Gets the API URL for environment variable injection.
    /// </summary>
    public static string GetApiUrl(this IResourceBuilder<SupabaseStackResource> builder)
        => builder.Resource.GetApiUrl();

    #endregion

    #region JWT Configuration

    /// <summary>
    /// Configures the JWT secret used for token signing.
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithJwtSecret(
        this IResourceBuilder<SupabaseStackResource> builder,
        string secret)
    {
        builder.Resource.JwtSecret = secret;
        return builder;
    }

    /// <summary>
    /// Configures the anonymous key for public API access.
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithAnonKey(
        this IResourceBuilder<SupabaseStackResource> builder,
        string anonKey)
    {
        builder.Resource.AnonKey = anonKey;
        return builder;
    }

    /// <summary>
    /// Configures the service role key for admin API access.
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithServiceRoleKey(
        this IResourceBuilder<SupabaseStackResource> builder,
        string serviceRoleKey)
    {
        builder.Resource.ServiceRoleKey = serviceRoleKey;
        return builder;
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// Updates the PostInitSqlBase64 by combining all SQL files from the scripts and init directories.
    /// Called after WithMigrations(), WithRegisteredUser(), or WithProjectSync() to ensure all SQL is included.
    /// </summary>
    internal static void UpdatePostInitSqlBase64(SupabaseStackResource stack)
    {
        if (stack.InitSqlPath is null) return;

        var initDir = stack.InitSqlPath;
        var scriptsDir = Path.Combine(Path.GetDirectoryName(initDir)!, "scripts");

        var combinedSql = new System.Text.StringBuilder();

        // Append synced schema SQL if exists (from WithProjectSync)
        // This must come FIRST as it creates tables that migrations might reference
        var syncSqlPath = Path.Combine(initDir, "01_sync_schema.sql");
        if (File.Exists(syncSqlPath))
        {
            var syncSql = File.ReadAllText(syncSqlPath);
            if (syncSql.Length < 200000) // 200KB limit for synced schema
            {
                combinedSql.AppendLine("-- Synced Schema from Remote Project");
                combinedSql.AppendLine(syncSql);
                combinedSql.AppendLine();
            }
            else
            {
                LogWarning("01_sync_schema.sql is too large for Azure deployment. Synced schema will not be applied.");
            }
        }

        // Read base post_init.sql (triggers and profiles)
        if (Directory.Exists(scriptsDir))
        {
            var postInitSqlPath = Path.Combine(scriptsDir, "post_init.sql");
            if (File.Exists(postInitSqlPath))
            {
                combinedSql.AppendLine("-- Post Init (Triggers)");
                combinedSql.AppendLine(File.ReadAllText(postInitSqlPath));
                combinedSql.AppendLine();
            }

            // Append migrations.sql if exists
            var migrationsSqlPath = Path.Combine(scriptsDir, "migrations.sql");
            if (File.Exists(migrationsSqlPath))
            {
                var migrationsSql = File.ReadAllText(migrationsSqlPath);
                if (migrationsSql.Length < 100000) // 100KB limit for migrations
                {
                    combinedSql.AppendLine("-- Migrations");
                    combinedSql.AppendLine(migrationsSql);
                    combinedSql.AppendLine();
                }
                else
                {
                    LogWarning("migrations.sql is too large for Azure deployment. Migrations will not run automatically.");
                }
            }

            // Append users.sql if exists
            var usersSqlPath = Path.Combine(scriptsDir, "users.sql");
            if (File.Exists(usersSqlPath))
            {
                var usersSql = File.ReadAllText(usersSqlPath);
                if (usersSql.Length < 50000) // 50KB limit for users
                {
                    combinedSql.AppendLine("-- Users");
                    combinedSql.AppendLine(usersSql);
                    combinedSql.AppendLine();
                }
                else
                {
                    LogWarning("users.sql is too large for Azure deployment. Users will not be created automatically.");
                }
            }
        }

        // Update the base64 content
        stack.PostInitSqlBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(combinedSql.ToString()));

        // Also update the init container environment variable if it exists
        stack.InitContainer?.WithEnvironment("POST_INIT_SQL_BASE64", stack.PostInitSqlBase64);

        LogInformation("PostInitSqlBase64 updated for publish mode");
    }

    #endregion
}
