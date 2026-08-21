namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// The tools the studio's MCP endpoint offers, by name — so <c>WithMcpTools</c> takes a constant
/// rather than a string somebody has to spell right.
/// </summary>
public static class WebDataStudioMcpTools
{
    /// <summary>The databases the studio can reach, with their ids.</summary>
    public const string ListConnections = "list_connections";

    /// <summary>Every table and view of a connection, in one call.</summary>
    public const string ListTables = "list_tables";

    /// <summary>Walks the object tree a level at a time.</summary>
    public const string ListObjects = "list_objects";

    /// <summary>Columns, indexes, keys and triggers of one object.</summary>
    public const string DescribeObject = "describe_object";

    /// <summary>A page of rows from one table, masked and capped.</summary>
    public const string BrowseRows = "browse_rows";

    /// <summary>One reading statement, masked and capped.</summary>
    public const string RunQuery = "run_query";

    /// <summary>The query plan for a statement.</summary>
    public const string ExplainPlan = "explain_plan";

    /// <summary>The studio's own analysis of a connection or a table.</summary>
    public const string HealthReport = "health_report";

    /// <summary>What the server is running, and who waits on whom.</summary>
    public const string ServerActivity = "server_activity";

    /// <summary>One Redis key, in the shape its type has.</summary>
    public const string RedisValue = "redis_value";

    /// <summary>Splits a script and returns a hash. Runs nothing. Needs <c>allowWrite</c>.</summary>
    public const string PreviewScript = "preview_script";

    /// <summary>Runs the script a hash belongs to. Needs <c>allowWrite</c>.</summary>
    public const string ApplyScript = "apply_script";

    /// <summary>Everything that only reads — the useful default for an agent you do not fully trust.</summary>
    public static readonly string[] ReadOnly =
    [
        ListConnections, ListTables, ListObjects, DescribeObject, BrowseRows, RunQuery,
        ExplainPlan, HealthReport, ServerActivity, RedisValue,
    ];

    /// <summary>Enough to find one's way around a schema, without reading a single row.</summary>
    public static readonly string[] SchemaOnly =
    [
        ListConnections, ListTables, ListObjects, DescribeObject, ExplainPlan, HealthReport,
    ];
}
