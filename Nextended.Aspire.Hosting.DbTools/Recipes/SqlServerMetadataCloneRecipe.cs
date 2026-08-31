namespace Nextended.Aspire.Hosting.DbTools;

/// SQL Server, cloned out of what a reader can see.
///
/// The other SQL Server recipe asks sqlpackage, and sqlpackage asks the server for `VIEW DEFINITION`
/// before it does anything. A login that may read every row and every definition but does not hold
/// that one permission — an application login on a shared development database, which is the usual
/// case — is refused outright:
///
///     The reverse engineering operation cannot continue because you do not have View Definition
///     permission on the '…' database.
///
/// So this recipe does not ask the schema tools. SMO reads the metadata the reader is allowed to read
/// and writes the DDL out of it; `SqlBulkCopy` streams the rows straight from one server to the
/// other, with the constraints switched off for the duration and identity values kept. Both need
/// SELECT and nothing else.
///
/// What it costs: the container installs SMO from nuget.org on its first start, like the sqlpackage
/// recipe does, and the copy is statement by statement rather than one archive — minutes for a
/// schema of a few hundred tables.
internal static class SqlServerMetadataCloneRecipe
{
    internal const int DefaultPort = 1433;
    internal const string DefaultImage = "mcr.microsoft.com/dotnet/sdk";
    internal const string DefaultTag = "8.0";

    internal static string Script() =>
        Shell
            .Replace("{{PROJECT}}", Project)
            .Replace("{{PROGRAM}}", Program);

    private const string Shell = """
        set -e

        mkdir -p /work
        cd /work

        cat > clone.csproj <<'PROJECT_EOF'
        {{PROJECT}}
        PROJECT_EOF

        cat > Program.cs <<'PROGRAM_EOF'
        {{PROGRAM}}
        PROGRAM_EOF

        # The two connection strings the copier works with. TrustServerCertificate, because a
        # development server's certificate is self-signed and nobody can fix that from here.
        export CLONE_SOURCE_CS="Server=$CLONE_SOURCE_HOST,$CLONE_SOURCE_PORT;Database=$CLONE_SOURCE_DB;User Id=$CLONE_SOURCE_USER;Password=$CLONE_SOURCE_PASSWORD;TrustServerCertificate=True;Connect Timeout=60"
        export CLONE_TARGET_CS="Server=$CLONE_TARGET_HOST,$CLONE_TARGET_PORT;Database=$CLONE_TARGET_DB;User Id=$CLONE_TARGET_USER;Password=$CLONE_TARGET_PASSWORD;TrustServerCertificate=True;Connect Timeout=60"

        echo "building the copier (SMO comes from nuget.org, once per container)"
        dotnet build -c Release --nologo -v q >/tmp/build.log 2>&1 || { tail -20 /tmp/build.log; exit 1; }

        # timeout, because a stalled query is the failure mode here and silence is indistinguishable
        # from work. The copier says what it is doing as it goes.
        timeout "${CLONE_TIMEOUT:-3600}" dotnet run -c Release --no-build
        status=$?

        [ "$status" = "124" ] && echo "gave up after ${CLONE_TIMEOUT:-3600}s (DbCloneOptions.TimeoutSeconds)"
        exit $status
        """;

    private const string Project = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net8.0</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
            <AssemblyName>clone</AssemblyName>
            <RootNamespace>Clone</RootNamespace>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Microsoft.SqlServer.SqlManagementObjects" Version="172.76.0" />
            <PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.2" />
          </ItemGroup>
        </Project>
        """;

    /// The copier. Written into the container rather than shipped as a binary, so the package stays
    /// a package and the reader can see exactly what runs against their database.
    private const string Program = """
        using Microsoft.Data.SqlClient;
        using Microsoft.SqlServer.Management.Common;
        using Microsoft.SqlServer.Management.Smo;

        var sourceCs = Env("CLONE_SOURCE_CS");
        var targetCs = Env("CLONE_TARGET_CS");
        var schemaOnly = Env("CLONE_SCHEMA_ONLY", "0") == "1";
        var dataOnly = Env("CLONE_DATA_ONLY", "0") == "1";
        var onlyWhenEmpty = Env("CLONE_ONLY_WHEN_EMPTY", "1") == "1";
        var overwrite = Env("CLONE_OVERWRITE", "0") == "1";

        var target = new SqlConnectionStringBuilder(targetCs) { TrustServerCertificate = true };
        var targetDb = target.InitialCatalog;
        var master = new SqlConnectionStringBuilder(target.ConnectionString) { InitialCatalog = "master" };

        var started = DateTime.UtcNow;
        void Say(string what) => Console.WriteLine($"[{DateTime.UtcNow - started:mm\\:ss}] {what}");

        // What the target resource shows while this runs: read by the app host, not by a person.
        void Progress(int percent, string what) => Console.WriteLine($"##progress {percent} {what}");

        // --- the target database --------------------------------------------------------------------
        await using (var admin = new SqlConnection(master.ConnectionString))
        {
            await Wait(admin, Say);

            if (overwrite)
            {
                Say($"dropping {targetDb}");
                await Exec(admin,
                    "IF DB_ID(@name) IS NOT NULL BEGIN "
                    + $"ALTER DATABASE {Quote(targetDb)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; "
                    + $"DROP DATABASE {Quote(targetDb)}; END",
                    ("@name", targetDb));
            }

            await Exec(admin, $"IF DB_ID(@name) IS NULL CREATE DATABASE {Quote(targetDb)}", ("@name", targetDb));
        }

        await using (var probe = new SqlConnection(target.ConnectionString))
        {
            await probe.OpenAsync();
            var already = Convert.ToInt32(await Scalar(probe, "SELECT COUNT(*) FROM sys.tables") ?? 0);

            if (already > 0 && onlyWhenEmpty && !overwrite && !dataOnly)
            {
                Say($"{targetDb} already has {already} table(s); nothing was copied");
                Progress(100, "Cloned earlier");
                return 0;
            }
        }

        // --- the source, as the reader sees it ------------------------------------------------------
        var source = new SqlConnectionStringBuilder(sourceCs) { TrustServerCertificate = true };
        var server = new Server(new ServerConnection(new SqlConnection(source.ConnectionString)));
        var db = server.Databases[source.InitialCatalog];

        if (db is null)
        {
            Console.Error.WriteLine($"the source has no database called {source.InitialCatalog}");
            return 1;
        }

        var tables = db.Tables.Cast<Table>().Where(t => !t.IsSystemObject).ToList();
        var views = db.Views.Cast<View>().Where(v => !v.IsSystemObject).ToList();
        var procedures = db.StoredProcedures.Cast<StoredProcedure>().Where(p => !p.IsSystemObject).ToList();
        var functions = db.UserDefinedFunctions.Cast<UserDefinedFunction>().Where(f => !f.IsSystemObject).ToList();

        Say($"source: {tables.Count} table(s), {views.Count} view(s), {procedures.Count} procedure(s), "
            + $"{functions.Count} function(s)");

        if (!dataOnly)
        {
            var scripter = new Scripter(server)
            {
                Options =
                {
                    ScriptSchema = true, ScriptData = false, SchemaQualify = true, Indexes = true,
                    DriAll = true, Triggers = true, ScriptDrops = false, IncludeIfNotExists = false,
                    WithDependencies = true, ContinueScriptingOnError = false, AllowSystemObjects = false,
                },
            };

            var urns = tables.Select(t => t.Urn)
                .Concat(views.Select(v => v.Urn))
                .Concat(procedures.Select(p => p.Urn))
                .Concat(functions.Select(f => f.Urn))
                .ToArray();

            Say("reading the schema");
            Progress(5, $"reading the schema of {tables.Count} table(s)");

            // One call that takes minutes on a real database, and says nothing while it does. The
            // heartbeat is the difference between "working" and "hung" for whoever is watching the
            // resource in the dashboard.
            List<string> batches;

            using (new Timer(_ => Say("still reading the schema"), null,
                       TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30)))
            {
                batches = scripter.EnumScript(urns).ToList();
            }

            Say($"{batches.Count} statement(s) to run");
            Progress(25, $"applying {batches.Count} statement(s)");

            await using var write = new SqlConnection(target.ConnectionString);
            await write.OpenAsync();

            // The schemas first: SMO scripts objects, not the schemas they live in.
            foreach (var name in tables.Select(t => t.Schema).Concat(views.Select(v => v.Schema))
                         .Concat(procedures.Select(p => p.Schema)).Concat(functions.Select(f => f.Schema))
                         .Distinct()
                         .Where(n => !string.IsNullOrEmpty(n) && n != "dbo"))
            {
                await Exec(write, $"IF SCHEMA_ID(@name) IS NULL EXEC('CREATE SCHEMA {Quote(name)}')", ("@name", name));
            }

            var skipped = 0;

            foreach (var batch in batches)
            {
                if (string.IsNullOrWhiteSpace(batch)) continue;

                try
                {
                    await Exec(write, batch);
                }
                catch (SqlException e)
                {
                    skipped++;
                    Console.WriteLine("  skipped: " + e.Message.Split('\n')[0]);
                }
            }

            Say($"schema applied{(skipped > 0 ? $", {skipped} statement(s) skipped" : "")}");
            Progress(schemaOnly ? 100 : 40, schemaOnly ? "Cloned" : "schema applied");
            Say("in the copy: " + await Inventory(write));
        }

        if (schemaOnly)
        {
            Say($"done: the schema of {source.InitialCatalog} is in {targetDb}");
            return 0;
        }

        // --- the rows -------------------------------------------------------------------------------
        await using (var write = new SqlConnection(target.ConnectionString))
        {
            await write.OpenAsync();

            // Constraints off while loading, so the order the tables arrive in does not matter.
            foreach (var table in tables)
            {
                try { await Exec(write, $"ALTER TABLE {Quote(table.Schema)}.{Quote(table.Name)} NOCHECK CONSTRAINT ALL"); }
                catch (SqlException) { }
            }

            var copied = 0L;
            var done = 0;
            var failed = 0;

            foreach (var table in tables)
            {
                var name = $"{Quote(table.Schema)}.{Quote(table.Name)}";
                done++;

                await using var read = new SqlConnection(source.ConnectionString);
                await read.OpenAsync();

                await using var command = new SqlCommand($"SELECT * FROM {name}", read) { CommandTimeout = 0 };
                await using var reader = await command.ExecuteReaderAsync();

                using var bulk = new SqlBulkCopy(target.ConnectionString,
                    SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.KeepNulls | SqlBulkCopyOptions.TableLock)
                {
                    DestinationTableName = name,
                    BulkCopyTimeout = 0,
                    BatchSize = 5000,
                    NotifyAfter = 200000,
                };

                bulk.SqlRowsCopied += (_, e) => Console.WriteLine($"    {name}: {e.RowsCopied:N0} rows so far");

                try
                {
                    await bulk.WriteToServerAsync(reader);
                    copied += bulk.RowsCopied;
                    if (bulk.RowsCopied > 0) Console.WriteLine($"  {name}: {bulk.RowsCopied:N0} row(s)");
                }
                catch (Exception e)
                {
                    failed++;
                    Console.WriteLine($"  {name}: {e.Message.Split('\n')[0]}");
                }

                // The schema was the first 40%; the rows are the rest.
                Progress(40 + (int)(done * 60.0 / Math.Max(tables.Count, 1)),
                    $"copying rows ({done} of {tables.Count} tables, {copied:N0} so far)");

                if (done % 25 == 0) Say($"{done} of {tables.Count} table(s)");
            }

            foreach (var table in tables)
            {
                try { await Exec(write, $"ALTER TABLE {Quote(table.Schema)}.{Quote(table.Name)} WITH CHECK CHECK CONSTRAINT ALL"); }
                catch (SqlException e) { Console.WriteLine($"  constraint not re-checked on {table.Name}: {e.Message.Split('\n')[0]}"); }
            }

            Progress(100, "Cloned");
            Say($"done: {tables.Count} table(s), {copied:N0} row(s) in {targetDb}"
                + (failed > 0 ? $", {failed} table(s) not copied" : ""));

            return failed > 0 ? 1 : 0;
        }

        // What is actually in the copy, so the log answers "did it work" without anybody opening a
        // client. The studio's tree shows it after a refresh; this says it right away.
        static async Task<string> Inventory(SqlConnection connection)
        {
            var sql =
                "SELECT CAST((SELECT COUNT(*) FROM sys.tables) AS varchar(10)) + ' table(s), '"
                + " + CAST((SELECT COUNT(*) FROM sys.views WHERE is_ms_shipped = 0) AS varchar(10)) + ' view(s), '"
                + " + CAST((SELECT COUNT(*) FROM sys.objects WHERE type IN (''P'',''FN'',''IF'',''TF'')) AS varchar(10)) + ' routine(s), '"
                + " + CAST((SELECT COUNT(*) FROM sys.indexes WHERE index_id > 0) AS varchar(10)) + ' index(es), '"
                + " + CAST((SELECT COUNT(*) FROM sys.foreign_keys) AS varchar(10)) + ' foreign key(s)'";

            return (await Scalar(connection, sql.Replace("''", "'")))?.ToString() ?? "nothing";
        }

        static string Env(string name, string? fallback = null) =>
            Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
                ? value
                : fallback ?? throw new InvalidOperationException($"{name} is not set");

        static string Quote(string name) => "[" + name.Replace("]", "]]") + "]";

        // The target may still be starting: a stack's SQL Server takes its time, and there is no
        // client here to ask with other than this one.
        static async Task Wait(SqlConnection connection, Action<string> say)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await connection.OpenAsync();
                    return;
                }
                catch (SqlException) when (attempt < 60)
                {
                    if (attempt == 1) say("waiting for the target");
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
        }

        static async Task Exec(SqlConnection connection, string sql, params (string Name, object Value)[] parameters)
        {
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 0 };
            foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
            await command.ExecuteNonQueryAsync();
        }

        static async Task<object?> Scalar(SqlConnection connection, string sql)
        {
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 0 };
            return await command.ExecuteScalarAsync();
        }
        """;
}
