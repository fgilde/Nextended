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
    /// Configures and creates the Edge Runtime container without a local Edge Functions folder.
    /// Kong routes /functions/v1/* to this runtime, so the stack behaves like a real Supabase
    /// project (health endpoint, 404 for unknown functions) even when the frontend ships no
    /// Edge Functions (e.g. TanStack Start server functions instead).
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    public static IResourceBuilder<SupabaseStackResource> WithEdgeFunctions(
        this IResourceBuilder<SupabaseStackResource> builder)
        => builder.EnsureEdgeRuntime(functionsPath: null);

    /// <summary>
    /// Configures and creates the Edge Runtime container for Edge Functions.
    /// The runtime is also created when the directory is missing or contains no functions,
    /// so Kong's /functions/v1 route stays reachable and ConfigureEdgeRuntime() keeps working.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="functionsPath">The absolute path to the supabase/functions directory.</param>
    public static IResourceBuilder<SupabaseStackResource> WithEdgeFunctions(
        this IResourceBuilder<SupabaseStackResource> builder,
        string functionsPath)
        => builder.EnsureEdgeRuntime(functionsPath);

    /// <summary>
    /// Creates the Edge Runtime container if it does not exist yet.
    /// </summary>
    internal static IResourceBuilder<SupabaseStackResource> EnsureEdgeRuntime(
        this IResourceBuilder<SupabaseStackResource> builder,
        string? functionsPath)
    {
        var stack = builder.Resource;

        if (stack.EdgeRuntime is not null)
        {
            if (functionsPath is not null)
                LogWarning("Edge Runtime already configured. Additional WithEdgeFunctions() call is ignored.");
            return builder;
        }

        var appBuilder = stack.AppBuilder;

        if (appBuilder is null)
        {
            LogError("AppBuilder not available. Was AddSupabase() called?");
            return builder;
        }

        var hasFunctionsDir = functionsPath is not null && Directory.Exists(functionsPath);
        if (functionsPath is not null && !hasFunctionsDir)
            LogWarning($"Edge Functions directory not found: {functionsPath} - Edge Runtime starts without functions");

        // List available functions (only directories with index.ts)
        var functionDirs = hasFunctionsDir
            ? Directory.GetDirectories(functionsPath!)
                .Select(d => Path.GetFileName(d))
                .Where(name => !name.StartsWith("_") && !name.StartsWith("."))
                .Where(name => File.Exists(Path.Combine(functionsPath!, name, "index.ts")))
                .ToList()
            : [];

        if (hasFunctionsDir && functionDirs.Count == 0)
            LogWarning($"No Edge Functions with index.ts found in: {functionsPath} - Edge Runtime starts without functions");
        else if (functionDirs.Count > 0)
            LogInformation($"Edge Functions found: {string.Join(", ", functionDirs)}");

        if (hasFunctionsDir)
            stack.EdgeFunctionsPath = functionsPath;
        var containerPrefix = stack.Name;

        // Mode-agnostic DB endpoint (set by AddSupabase): internal container OR injected external resource.
        var dbEndpoint = stack.DatabaseEndpoint!;

        // Read the DB password HERE — WithEdgeFunctions runs AFTER ConfigureDatabase/WithPassword in the
        // chain, so the internal dbResource password is finalized. Internal: the live password. External:
        // the injected resource's password parameter (stack.DatabasePassword). Using the early
        // stack.DatabasePassword snapshot for the internal path would be stale (pre-WithPassword default).
        var edgeDbUrl = stack.Database is not null
            ? ReferenceExpression.Create(
                $"postgresql://postgres:{stack.Database.Resource.Password}@{dbEndpoint.Property(EndpointProperty.Host)}:{dbEndpoint.Property(EndpointProperty.Port)}/postgres")
            : ReferenceExpression.Create(
                $"postgresql://postgres:{stack.DatabasePassword!}@{dbEndpoint.Property(EndpointProperty.Host)}:{dbEndpoint.Property(EndpointProperty.Port)}/postgres");

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
            .WaitFor(stack.DatabaseWaitTarget!)
            .WaitFor(stack.Kong!);

        if (isPublishMode)
        {
            // In Azure Container Apps:
            // Init containers are separate Container Apps - they CANNOT share volumes!
            // Solution: Pass function code as environment variables and write them at container startup
            // The Deno container will write the files before starting the runtime

            var edgeMountPath = "/home/deno";

            // Read main.ts content (small, no compression needed)
            var mainTsContent = File.ReadAllText(mainTsPath);
            var mainTsBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(mainTsContent));

            // Build the setup command that writes all files and then starts Deno
            var setupCommands = new List<string>
            {
                $"mkdir -p {edgeMountPath}/main {edgeMountPath}/functions",
                $"echo $MAIN_TS_BASE64 | base64 -d > {edgeMountPath}/main/main.ts"
            };

            // Read all function files, gzip+base64 encode, and add as environment variables.
            // Gzip is needed because Bicep has a 128KB literal limit per env var.
            foreach (var funcName in functionDirs)
            {
                var funcIndexPath = Path.Combine(functionsPath!, funcName, "index.ts");
                if (File.Exists(funcIndexPath))
                {
                    var funcBytes = System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(funcIndexPath));
                    using var ms = new System.IO.MemoryStream();
                    using (var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Compress, true))
                        gz.Write(funcBytes, 0, funcBytes.Length);
                    var funcGzBase64 = Convert.ToBase64String(ms.ToArray());
                    var envVarName = $"FUNC_{funcName.ToUpperInvariant().Replace("-", "_")}_GZ_B64";

                    edgeBuilder.WithEnvironment(envVarName, funcGzBase64);
                    setupCommands.Add($"mkdir -p {edgeMountPath}/functions/{funcName} && echo ${envVarName} | base64 -d | gunzip > {edgeMountPath}/functions/{funcName}/index.ts");
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
            edgeBuilder.WithBindMount(edgeDir, "/home/deno/main", isReadOnly: true);

            // Without a functions directory there is nothing to mount - the router
            // never reads /home/deno/functions when its function list is empty.
            if (hasFunctionsDir)
                edgeBuilder.WithBindMount(functionsPath!, "/home/deno/functions", isReadOnly: true);

            edgeBuilder.WithArgs("run", "--allow-all", "--unstable-worker-options", "/home/deno/main/main.ts");
        }
        stack.EdgeRuntime = edgeBuilder;

        // Azure Container Apps: set allowInsecure for internal HTTP communication
        if (appBuilder.ExecutionContext.IsPublishMode)
        {
            edgeBuilder.PublishAsAzureContainerApp((infra, app) =>
            {
                app.Configuration.Ingress.AllowInsecure = true;
            });
        }

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
        // READINESS GATE. The Supabase service containers migrate their OWN schemas to the MODERN
        // shape on their first start, in SEPARATE containers (GoTrue -> auth, storage-api -> storage).
        // The supabase/postgres image bakes ANCIENT auth/storage tables at initdb, so waiting for the
        // TABLE to exist is not enough — it exists far too early. We poll for a MODERN COLUMN that only
        // appears AFTER the service finished migrating: auth.users.email_confirmed_at (written by the
        // admin seed + needed by nearly every migration) and storage.buckets.public (needed by the
        // bucket-seed migrations + storage RLS). One combined poll (~5 min budget), then a HARD abort
        // if auth is still un-migrated; storage stays a SOFT per-file gate below so a slow/stuck Storage
        // API never blocks the ~88 non-storage migrations. NOTE: gate on a stable modern COLUMN, never
        // on a migration COUNT (that is tied to the pinned image tag and any bump would hang forever).
        combinedSql.AppendLine("DO $$");
        combinedSql.AppendLine("DECLARE");
        combinedSql.AppendLine("    retry_count integer := 0;");
        combinedSql.AppendLine("    max_retries integer := 150;  -- ~5 min at 2s/iteration");
        combinedSql.AppendLine("    auth_ok boolean;");
        combinedSql.AppendLine("    storage_ok boolean;");
        combinedSql.AppendLine("BEGIN");
        combinedSql.AppendLine("    LOOP");
        combinedSql.AppendLine("        auth_ok := EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'auth' AND table_name = 'users' AND column_name = 'email_confirmed_at');");
        combinedSql.AppendLine("        storage_ok := EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'storage' AND table_name = 'buckets' AND column_name = 'public');");
        combinedSql.AppendLine("        EXIT WHEN (auth_ok AND storage_ok) OR retry_count >= max_retries;");
        combinedSql.AppendLine("        PERFORM pg_sleep(2);");
        combinedSql.AppendLine("        retry_count := retry_count + 1;");
        combinedSql.AppendLine("        IF retry_count % 5 = 0 THEN");
        combinedSql.AppendLine("            RAISE NOTICE '[Migrations] readiness poll: auth=% storage=% (attempt %/%)', auth_ok, storage_ok, retry_count, max_retries;");
        combinedSql.AppendLine("        END IF;");
        combinedSql.AppendLine("    END LOOP;");
        combinedSql.AppendLine("    RAISE NOTICE '[Migrations] readiness poll done: auth=% storage=%', auth_ok, storage_ok;");
        combinedSql.AppendLine("END;");
        combinedSql.AppendLine("$$;");
        combinedSql.AppendLine();
        combinedSql.AppendLine("-- HARD auth gate: abort LOUDLY if GoTrue never migrated. In publish the whole file runs as one");
        combinedSql.AppendLine("-- `psql -f` inside a `set -e && ... && echo Completed` chain, so ON_ERROR_STOP makes psql exit");
        combinedSql.AppendLine("-- non-zero and the init container fail instead of running app migrations + the seed against a");
        combinedSql.AppendLine("-- half-migrated schema. Locally post_init.sh ignores the exit code (degrade, never brick).");
        combinedSql.AppendLine("\\set ON_ERROR_STOP on");
        combinedSql.AppendLine("DO $$");
        combinedSql.AppendLine("BEGIN");
        combinedSql.AppendLine("    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'auth' AND table_name = 'users' AND column_name = 'email_confirmed_at') THEN");
        combinedSql.AppendLine("        RAISE EXCEPTION '[Migrations] ABORT: auth schema not migrated (auth.users.email_confirmed_at missing) after readiness timeout - GoTrue did not finish its startup migrations. Refusing to run migrations against a half-migrated schema.';");
        combinedSql.AppendLine("    END IF;");
        combinedSql.AppendLine("END;");
        combinedSql.AppendLine("$$;");
        combinedSql.AppendLine("\\set ON_ERROR_STOP off");
        combinedSql.AppendLine();

        // ------------------------------------------------------------------
        // RUN-ONCE MIGRATION TRACKING
        // ------------------------------------------------------------------
        // post_init runs this whole file via `psql -f` on EVERY post-init pass
        // (cold start, container restart, every `azd deploy`/`azd up`). Without
        // tracking, non-idempotent migrations re-run against a persistent DB and
        // corrupt it (e.g. a row with a nullable UNIQUE column seeded as NULL gets a
        // duplicate because two NULLs are DISTINCT under a UNIQUE constraint; bare
        // `ALTER PUBLICATION ... ADD TABLE` errors on the 2nd run). Locally this
        // never shows because each `aspire run` starts a fresh DB = one pass.
        //
        // This makes the Aspire runner behave exactly like Supabase CLI / Lovable:
        // each migration file is applied at most once. We use psql's \gset + \if
        // meta-commands (NOT a wrapping DO block — that can't host raw DDL like
        // CREATE POLICY / ALTER PUBLICATION and would collide with the migrations'
        // own $$ dollar-quoting). The migration body runs as ordinary top-level
        // statements between \if/\endif.
        combinedSql.AppendLine("-- Run-once tracking table (mimics supabase_migrations.schema_migrations)");
        combinedSql.AppendLine("CREATE TABLE IF NOT EXISTS public._aspire_applied_migrations (");
        combinedSql.AppendLine("    filename   text PRIMARY KEY,");
        combinedSql.AppendLine("    applied_at timestamptz NOT NULL DEFAULT now()");
        combinedSql.AppendLine(");");
        combinedSql.AppendLine();

        // The loop runs with ON_ERROR_STOP OFF: a transactional file that errors rolls back (leaving
        // it UNMARKED) and psql continues to the next file. Only non-transactional (opt-out) files
        // scope ON_ERROR_STOP ON to themselves so they fail loud instead of being marked on error.
        combinedSql.AppendLine("\\set ON_ERROR_STOP off");
        combinedSql.AppendLine();

        // Statements that CANNOT run inside a BEGIN/COMMIT transaction. Files matching these are
        // emitted UNWRAPPED under a scoped ON_ERROR_STOP (fail loud, mark only after success) instead
        // of being transaction-wrapped. (Today only the two ALTER TYPE ... ADD VALUE migrations match;
        // they are add-only and would survive a transaction on PG15, but we opt them out to be safe
        // and future-proof.) Authors can force opt-out with a `-- aspire:no-transaction` marker.
        var nonTxRegex = new System.Text.RegularExpressions.Regex(
            @"\bCREATE\s+INDEX\s+CONCURRENTLY\b|\bDROP\s+INDEX\s+CONCURRENTLY\b|\bREINDEX\b[^;]*\bCONCURRENTLY\b|\bVACUUM\b|\bALTER\s+SYSTEM\b|\bCREATE\s+DATABASE\b|\bALTER\s+TYPE\b[\s\S]*?\bADD\s+VALUE\b|--\s*aspire:no-transaction",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        // Files whose body touches the storage schema must wait for storage-api to have migrated
        // (storage.buckets.public present). Gated PER FILE so a lagging Storage API never blocks the
        // non-storage migrations: if storage is not ready these files are skipped (unmarked) and
        // retried next pass; the final completeness assertion turns a persistent miss into a loud fail.
        var storageDepRegex = new System.Text.RegularExpressions.Regex(
            @"\bstorage\.(buckets|objects)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (var sqlFile in sqlFiles)
        {
            var fileName = Path.GetFileName(sqlFile);
            var sqlName = fileName.Replace("'", "''"); // single-quote safety for the SQL literal
            string content;
            try
            {
                content = File.ReadAllText(sqlFile);
            }
            catch (Exception ex)
            {
                LogWarning($"Could not read {fileName}: {ex.Message}");
                continue;
            }

            var isNonTx = nonTxRegex.IsMatch(content);
            var isStorageDep = storageDepRegex.IsMatch(content);

            combinedSql.AppendLine($"-- Migration: {fileName}{(isNonTx ? "  [non-transactional]" : "")}{(isStorageDep ? "  [storage-dependent]" : "")}");
            combinedSql.AppendLine("-- ----------------------------------------");
            // _run_mig := not-yet-applied (AND, for storage files, storage-api has migrated).
            // \gset must terminate the query (no trailing ';').
            if (isStorageDep)
            {
                combinedSql.AppendLine(
                    $"SELECT ((NOT EXISTS (SELECT 1 FROM public._aspire_applied_migrations WHERE filename = '{sqlName}')) " +
                    "AND EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'storage' AND table_name = 'buckets' AND column_name = 'public'))::text AS _run_mig \\gset");
            }
            else
            {
                combinedSql.AppendLine(
                    $"SELECT (NOT EXISTS (SELECT 1 FROM public._aspire_applied_migrations WHERE filename = '{sqlName}'))::text AS _run_mig \\gset");
            }
            combinedSql.AppendLine("\\if :_run_mig");

            if (isNonTx)
            {
                // Non-transactional: run unwrapped, fail loud on error, mark ONLY after success.
                combinedSql.AppendLine("\\set ON_ERROR_STOP on");
                combinedSql.AppendLine(content);
                combinedSql.AppendLine();
                combinedSql.AppendLine(
                    $"INSERT INTO public._aspire_applied_migrations (filename) VALUES ('{sqlName}') ON CONFLICT (filename) DO NOTHING;");
                combinedSql.AppendLine("\\set ON_ERROR_STOP off");
            }
            else
            {
                // Transactional: body + mark are ATOMIC. If any body statement errors the transaction
                // aborts, the mark is skipped and COMMIT becomes ROLLBACK -> the file stays UNMARKED and
                // is retried next pass; psql (ON_ERROR_STOP off) continues to the next file.
                combinedSql.AppendLine("BEGIN;");
                combinedSql.AppendLine(content);
                combinedSql.AppendLine();
                combinedSql.AppendLine(
                    $"INSERT INTO public._aspire_applied_migrations (filename) VALUES ('{sqlName}') ON CONFLICT (filename) DO NOTHING;");
                combinedSql.AppendLine("COMMIT;");
            }
            combinedSql.AppendLine("\\endif");
            combinedSql.AppendLine();
            LogInformation($"  + {fileName}{(isNonTx ? " [non-tx]" : "")}{(isStorageDep ? " [storage]" : "")}");
        }

        // FINAL completeness assertion: every expected migration must be recorded applied. If a file
        // failed (rolled back, unmarked) or was skipped (e.g. Storage API never migrated), this fails
        // LOUDLY so the pass never reports success while half-migrated. Because tracking stays truthful
        // (never falsely marked), a retry against a now-healthy DB applies exactly the missing files.
        var expectedValues = string.Join(", ",
            sqlFiles.Select(f => $"('{Path.GetFileName(f).Replace("'", "''")}')"));
        if (!string.IsNullOrEmpty(expectedValues))
        {
            combinedSql.AppendLine("\\set ON_ERROR_STOP on");
            combinedSql.AppendLine("DO $$");
            combinedSql.AppendLine("DECLARE missing text;");
            combinedSql.AppendLine("BEGIN");
            combinedSql.AppendLine($"    SELECT string_agg(e.f, ', ') INTO missing FROM (VALUES {expectedValues}) AS e(f)");
            combinedSql.AppendLine("        WHERE NOT EXISTS (SELECT 1 FROM public._aspire_applied_migrations m WHERE m.filename = e.f);");
            combinedSql.AppendLine("    IF missing IS NOT NULL THEN");
            combinedSql.AppendLine("        RAISE EXCEPTION '[Migrations] ABORT: not all migrations applied this pass (tracking left truthful for retry). Missing: %', missing;");
            combinedSql.AppendLine("    END IF;");
            combinedSql.AppendLine("    RAISE NOTICE '[Migrations] all expected migrations applied';");
            combinedSql.AppendLine("END;");
            combinedSql.AppendLine("$$;");
            combinedSql.AppendLine("\\set ON_ERROR_STOP off");
            combinedSql.AppendLine();
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
            // users.sql is appended to (a build may register several users). Clear it on the FIRST
            // registered user of THIS generation, otherwise File.AppendAllText accumulates a stale
            // seed block from every previous build/run (the file persists under infra/).
            if (builder.Resource.RegisteredUsers.Count == 1)
            {
                File.WriteAllText(userSqlPath, string.Empty);
            }
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
-- Loud seed: ON_ERROR_STOP makes a failed admin INSERT abort the init (publish) instead of being
-- silently swallowed, and a post-seed assertion confirms the user exists. (Local post_init.sh
-- ignores the exit code, so this degrades rather than bricks.)
\set ON_ERROR_STOP on
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
    ELSE
        RAISE NOTICE '[Post-Init] User already exists: {email}';
    END IF;

    -- Ensure the email identity exists (also heals users created by older versions).
    -- GoTrue >= v2 rejects password logins with "Invalid login credentials" when the
    -- user has no matching row in auth.identities.
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM auth.identities WHERE user_id = new_user_id AND provider = 'email') THEN
            INSERT INTO auth.identities (
                id, user_id, provider_id, provider, identity_data,
                last_sign_in_at, created_at, updated_at
            ) VALUES (
                extensions.uuid_generate_v4(), new_user_id, new_user_id::text, 'email',
                jsonb_build_object(
                    'sub', new_user_id::text,
                    'email', '{email}',
                    'email_verified', true,
                    'phone_verified', false
                ),
                NOW(), NOW(), NOW()
            );
            RAISE NOTICE '[Post-Init] Email identity created for: {email}';
        END IF;
    EXCEPTION WHEN OTHERS THEN
        RAISE WARNING '[Post-Init] Identity creation failed for {email}: %', SQLERRM;
    END;

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
EXCEPTION WHEN OTHERS THEN
    RAISE WARNING '[Post-Init] User creation completely failed for {email}: %', SQLERRM;
END;
$$;

-- Assert the admin user actually exists (loud if the INSERT above failed).
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM auth.users WHERE email = '{email}') THEN
        RAISE EXCEPTION '[Post-Init] ABORT: registered user {email} was not created';
    END IF;
END;
$$;
\set ON_ERROR_STOP off

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
                builder.ApplicationBuilder.ClearSupabase(true, containerPrefix);

                LogInformation("Cleanup completed. Please restart Aspire.");
                return Task.FromResult(new ExecuteCommandResult { Success = true });
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
    /// Updates the PostInitSqlBase64 cache by building the combined SQL in memory.
    /// Called after WithMigrations(), WithRegisteredUser(), or WithProjectSync().
    /// NOTE: The init container uses an environment callback that calls BuildCombinedPostInitSql()
    /// at resolution time, so the cache is only for informational purposes.
    /// </summary>
    internal static void UpdatePostInitSqlBase64(SupabaseStackResource stack)
    {
        var sql = SupabaseBuilderExtensions.BuildCombinedPostInitSql(stack);
        stack.PostInitSqlBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sql));
        LogInformation("PostInitSqlBase64 cache updated");
    }

    #endregion
}
