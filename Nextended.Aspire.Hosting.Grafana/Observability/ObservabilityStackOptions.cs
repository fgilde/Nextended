using Nextended.Aspire.Hosting.Grafana;

namespace Nextended.Aspire.Hosting.Observability;

/// <summary>
/// Options for <c>AddObservabilityStack</c> — the one-call, batteries-included way
/// to get the full stack. For piecemeal composition use the fluent
/// <c>AddGrafana().WithPrometheus()…</c> API from
/// <see cref="Nextended.Aspire.Hosting.Grafana.GrafanaBuilderExtensions"/>;
/// this options class drives exactly those building blocks.
/// </summary>
public sealed class ObservabilityStackOptions
{
    /// <summary>
    /// Working directory where the stack writes its generated YAML configs and
    /// (by default) reads dashboard JSONs from. Required. The stack creates a
    /// <c>.generated/</c> sub-folder here at runtime for the Prometheus / Loki /
    /// Promtail / Tempo / OTel-Collector / Grafana-Provisioning YAMLs.
    /// </summary>
    public required string ConfigRootPath { get; set; }

    /// <summary>
    /// Folder containing the Grafana dashboard JSON files (one file = one
    /// dashboard, auto-loaded by Grafana's provisioning). When <c>null</c>,
    /// defaults to <c>{ConfigRootPath}/grafana/dashboards</c>. Set explicitly
    /// if dashboards live outside the config tree.
    /// </summary>
    public string? DashboardsPath { get; set; }

    /// <summary>
    /// Name prefix used for every Aspire resource the stack creates. Final names look
    /// like <c>{ResourceNamePrefix}-prometheus</c>, <c>{ResourceNamePrefix}-grafana</c>,
    /// etc. Defaults to <c>"monitoring"</c>. Set to <c>""</c> to keep bare names.
    /// </summary>
    public string ResourceNamePrefix { get; set; } = "monitoring";

    /// <summary>
    /// Postgres-Exporter config. When <c>null</c>, no exporter container is added —
    /// useful if the consuming app doesn't use Postgres or already has metrics from
    /// elsewhere.
    /// </summary>
    public PostgresExporterOptions? PostgresExporter { get; set; }

    // ---- Component toggles -----------------------------------------------------
    // Default everything except heavy tracing pieces. Tempo/OTel-Collector are
    // off-by-default because they're only useful if downstream services actually
    // emit OTLP — turning them on without instrumentation just spins idle containers.

    public bool IncludePrometheus { get; set; } = true;
    public bool IncludeGrafana { get; set; } = true;
    public bool IncludeLoki { get; set; } = true;
    public bool IncludePromtail { get; set; } = true;
    public bool IncludeTempo { get; set; } = false;
    public bool IncludeOtelCollector { get; set; } = false;

    /// <summary>cAdvisor — per-container CPU / Memory / Network metrics scraped from the Docker socket.</summary>
    public bool IncludeCAdvisor { get; set; } = true;

    // ---- Image versions --------------------------------------------------------
    // Pinning images here gives consumers a single seam to upgrade without touching
    // the extension code. Defaults are shared with the fluent API.

    public string PrometheusImage { get; set; } = GrafanaStackDefaults.PrometheusImage;
    public string PrometheusImageTag { get; set; } = GrafanaStackDefaults.PrometheusImageTag;

    public string GrafanaImage { get; set; } = GrafanaStackDefaults.GrafanaImage;
    public string GrafanaImageTag { get; set; } = GrafanaStackDefaults.GrafanaImageTag;

    public string LokiImage { get; set; } = GrafanaStackDefaults.LokiImage;
    public string LokiImageTag { get; set; } = GrafanaStackDefaults.LokiImageTag;

    public string PromtailImage { get; set; } = GrafanaStackDefaults.PromtailImage;
    public string PromtailImageTag { get; set; } = GrafanaStackDefaults.PromtailImageTag;

    public string TempoImage { get; set; } = GrafanaStackDefaults.TempoImage;
    public string TempoImageTag { get; set; } = GrafanaStackDefaults.TempoImageTag;

    public string OtelCollectorImage { get; set; } = GrafanaStackDefaults.OtelCollectorImage;
    public string OtelCollectorImageTag { get; set; } = GrafanaStackDefaults.OtelCollectorImageTag;

    public string PostgresExporterImage { get; set; } = GrafanaStackDefaults.PostgresExporterImage;
    public string PostgresExporterImageTag { get; set; } = GrafanaStackDefaults.PostgresExporterImageTag;

    public string CAdvisorImage { get; set; } = GrafanaStackDefaults.CAdvisorImage;
    public string CAdvisorImageTag { get; set; } = GrafanaStackDefaults.CAdvisorImageTag;

    // ---- Grafana auth ----------------------------------------------------------

    /// <summary>
    /// When true (default), Grafana is started with anonymous Admin access — no
    /// login form. Convenient for local dev. Set false for shared/deployed setups
    /// and supply <see cref="GrafanaAdminUser"/>/<see cref="GrafanaAdminPassword"/>.
    /// </summary>
    public bool GrafanaAnonymousAdmin { get; set; } = true;

    public string GrafanaAdminUser { get; set; } = "admin";

    /// <summary>
    /// Admin password used when <see cref="GrafanaAnonymousAdmin"/> is false. Should
    /// come from a secret in production scenarios.
    /// </summary>
    public string? GrafanaAdminPassword { get; set; }

    /// <summary>Grafana retention for Prometheus (<c>--storage.tsdb.retention.time</c>). Default 15 days.</summary>
    public string PrometheusRetention { get; set; } = GrafanaStackDefaults.PrometheusRetention;

    /// <summary>
    /// Grafana folder name under which auto-provisioned dashboards appear in the
    /// sidebar. Default <c>"Application"</c>. Change to e.g. your app's name to
    /// brand the experience without touching dashboard JSON files.
    /// </summary>
    public string GrafanaDashboardsFolder { get; set; } = "Application";

    /// <summary>
    /// Aspire-dashboard OTLP endpoint that the OTel-Collector mirrors traces to.
    /// Default points at <c>host.docker.internal:18889</c> — Aspire 13's standard
    /// local-dev port. Set to <c>""</c> to disable the mirror exporter.
    /// </summary>
    public string AspireDashboardOtlpEndpoint { get; set; } = GrafanaStackDefaults.AspireDashboardOtlpEndpoint;
}

/// <summary>
/// Connection options for the <c>postgres_exporter</c> sidecar.
/// </summary>
public sealed class PostgresExporterOptions
{
    /// <summary>Hostname or container name of the Postgres instance (Docker DNS). Required.</summary>
    public required string Host { get; set; }

    public int Port { get; set; } = 5432;
    public string Database { get; set; } = "postgres";
    public string Username { get; set; } = "postgres";

    /// <summary>Password for the exporter's read connection. Required.</summary>
    public required string Password { get; set; }

    /// <summary>SSL mode. Default <c>"disable"</c> for local Docker setups.</summary>
    public string SslMode { get; set; } = "disable";

    /// <summary>Builds the libpq-style DSN that postgres_exporter expects.</summary>
    internal string ToDataSourceName() =>
        $"postgresql://{Username}:{Password}@{Host}:{Port}/{Database}?sslmode={SslMode}";
}
