namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// The database engines WebDataStudio can talk to. The value tells the studio which driver to
/// open a connection string with, and is passed as <c>WDS_CONN_&lt;NAME&gt;_ENGINE</c>.
/// </summary>
public enum WebDataStudioEngine
{
    /// <summary>PostgreSQL (and anything speaking its wire protocol).</summary>
    PostgreSql,

    /// <summary>MySQL and MariaDB.</summary>
    MySql,

    /// <summary>Microsoft SQL Server and Azure SQL.</summary>
    SqlServer,

    /// <summary>SQLite, backed by a file the container can reach.</summary>
    Sqlite,

    /// <summary>Oracle Database.</summary>
    Oracle,

    /// <summary>DuckDB, backed by a file the container can reach.</summary>
    DuckDb,

    /// <summary>ClickHouse.</summary>
    ClickHouse,

    /// <summary>MongoDB.</summary>
    MongoDb,

    /// <summary>Redis and Valkey.</summary>
    Redis,
}

/// <summary>Maps <see cref="WebDataStudioEngine"/> to the identifiers WebDataStudio expects.</summary>
public static class WebDataStudioEngineExtensions
{
    /// <summary>The engine id as WebDataStudio spells it in <c>WDS_CONN_&lt;NAME&gt;_ENGINE</c>.</summary>
    public static string ToEngineId(this WebDataStudioEngine engine) => engine switch
    {
        WebDataStudioEngine.PostgreSql => "postgresql",
        WebDataStudioEngine.MySql => "mysql",
        WebDataStudioEngine.SqlServer => "sqlserver",
        WebDataStudioEngine.Sqlite => "sqlite",
        WebDataStudioEngine.Oracle => "oracle",
        WebDataStudioEngine.DuckDb => "duckdb",
        WebDataStudioEngine.ClickHouse => "clickhouse",
        WebDataStudioEngine.MongoDb => "mongodb",
        WebDataStudioEngine.Redis => "redis",
        _ => throw new ArgumentOutOfRangeException(nameof(engine), engine, "unknown engine"),
    };
}
