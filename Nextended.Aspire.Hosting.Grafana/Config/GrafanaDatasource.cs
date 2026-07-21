namespace Nextended.Aspire.Hosting.Grafana;

/// <summary>
/// A Grafana datasource that gets written into the generated provisioning YAML.
/// The stack auto-creates datasources for Prometheus / Loki / Tempo / Postgres;
/// use <c>WithDatasource</c> for anything else Grafana supports (MySQL, MSSQL,
/// Elasticsearch, InfluxDB, …) — <see cref="Type"/> takes the Grafana datasource
/// type id, <see cref="JsonData"/>/<see cref="SecureJsonData"/> map 1:1 to the
/// provisioning schema.
/// </summary>
public sealed class GrafanaDatasource
{
    /// <summary>Display name in Grafana. Required.</summary>
    public required string Name { get; set; }

    /// <summary>Grafana datasource type id, e.g. <c>prometheus</c>, <c>loki</c>, <c>tempo</c>, <c>postgres</c>, <c>mysql</c>. Required.</summary>
    public required string Type { get; set; }

    /// <summary>Target URL, e.g. <c>http://prometheus:9090</c> or <c>my-db:5432</c>. Required.</summary>
    public required string Url { get; set; }

    /// <summary>Stable uid used by dashboards to reference this datasource. Defaults to a slug of <see cref="Name"/>.</summary>
    public string? Uid { get; set; }

    /// <summary>Access mode. <c>proxy</c> (server-side, default) or <c>direct</c>.</summary>
    public string Access { get; set; } = "proxy";

    /// <summary>Marks this datasource as Grafana's default.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Whether the datasource may be edited in the Grafana UI. Default false (provisioning owns it).</summary>
    public bool Editable { get; set; }

    /// <summary>Login user for datasources that need one (e.g. SQL databases).</summary>
    public string? User { get; set; }

    /// <summary>
    /// Non-secret settings, serialized under <c>jsonData:</c>. Values may be scalars,
    /// nested <see cref="Dictionary{TKey,TValue}"/> (string → object) or lists.
    /// </summary>
    public Dictionary<string, object?> JsonData { get; } = [];

    /// <summary>
    /// Secret settings, serialized under <c>secureJsonData:</c>. Prefer env-var
    /// references like <c>${MY_VAR}</c> over literals — the generated YAML lands on
    /// disk, Grafana expands the variable on load.
    /// </summary>
    public Dictionary<string, object?> SecureJsonData { get; } = [];

    internal string ResolvedUid => Uid ?? Name.ToLowerInvariant().Replace(' ', '-');
}

/// <summary>A Prometheus scrape job pointing at one static target.</summary>
/// <param name="JobName">Prometheus <c>job_name</c>.</param>
/// <param name="Target">host:port of the metrics endpoint (Docker DNS — use the resource name as host).</param>
/// <param name="MetricsPath">Metrics path when it differs from <c>/metrics</c>.</param>
public sealed record PrometheusScrapeJob(string JobName, string Target, string? MetricsPath = null)
{
    /// <summary>Optional raw YAML appended to the job (relabel configs etc.), indented two spaces relative to the job.</summary>
    public string? ExtraYaml { get; init; }
}
