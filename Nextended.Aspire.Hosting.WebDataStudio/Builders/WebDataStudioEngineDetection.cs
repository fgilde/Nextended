using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// Works out which engine an Aspire resource speaks. Matching happens on the resource's type name
/// rather than by referencing every hosting package: this integration would otherwise drag in
/// PostgreSQL, MySQL, SQL Server, Oracle, MongoDB and Redis just to read a type. Anything it does
/// not recognise is passed to the studio without an engine, which then guesses from the
/// connection string — or you name the engine yourself.
/// </summary>
internal static class WebDataStudioEngineDetection
{
    private static readonly (string Fragment, WebDataStudioEngine Engine)[] Known =
    [
        ("postgres", WebDataStudioEngine.PostgreSql),
        ("npgsql", WebDataStudioEngine.PostgreSql),
        ("mysql", WebDataStudioEngine.MySql),
        ("mariadb", WebDataStudioEngine.MySql),
        ("sqlserver", WebDataStudioEngine.SqlServer),
        ("mssql", WebDataStudioEngine.SqlServer),
        ("azuresql", WebDataStudioEngine.SqlServer),
        ("sqlite", WebDataStudioEngine.Sqlite),
        ("oracle", WebDataStudioEngine.Oracle),
        ("duckdb", WebDataStudioEngine.DuckDb),
        ("clickhouse", WebDataStudioEngine.ClickHouse),
        ("mongo", WebDataStudioEngine.MongoDb),
        ("redis", WebDataStudioEngine.Redis),
        ("valkey", WebDataStudioEngine.Redis),
        ("garnet", WebDataStudioEngine.Redis),
    ];

    internal static WebDataStudioEngine? Detect(IResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        // The type name is the reliable signal: a database resource is called
        // PostgresDatabaseResource, MySqlDatabaseResource, MongoDBDatabaseResource and so on.
        // The resource's own name is a distant second guess and only used when the type says
        // nothing, because "orders" tells us nothing but "orders-postgres" does.
        return Match(resource.GetType().Name) ?? Match(resource.Name);
    }

    private static WebDataStudioEngine? Match(string candidate)
    {
        foreach (var (fragment, engine) in Known)
            if (candidate.Contains(fragment, StringComparison.OrdinalIgnoreCase)) return engine;

        return null;
    }
}
