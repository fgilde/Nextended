namespace Nextended.Aspire.Hosting.Observability;

/// <summary>
/// Options for <c>AddObservabilityStack</c>. Designed to be self-contained so the
/// stack can later be extracted into a reusable NuGet package without depending on
/// any specific project layout.
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
    /// if dashboards live outside the config tree — required for the NuGet
    /// scenario where the consumer wants to point at their own folder.
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
    // the extension code. All values are sane defaults at the time of writing.

    public string PrometheusImage { get; set; } = "prom/prometheus";
    public string PrometheusImageTag { get; set; } = "v2.55.1";

    public string GrafanaImage { get; set; } = "grafana/grafana";
    public string GrafanaImageTag { get; set; } = "11.5.0";

    public string LokiImage { get; set; } = "grafana/loki";
    public string LokiImageTag { get; set; } = "3.3.0";

    public string PromtailImage { get; set; } = "grafana/promtail";
    public string PromtailImageTag { get; set; } = "3.3.0";

    public string TempoImage { get; set; } = "grafana/tempo";
    public string TempoImageTag { get; set; } = "2.6.0";

    public string OtelCollectorImage { get; set; } = "otel/opentelemetry-collector-contrib";
    // 0.111.0 is the last release where the binary sat at `/otelcol-contrib`.
    // Starting with 0.112+, it moved to `/usr/local/bin/otelcol-contrib`, which
    // breaks docker-startup on some setups with: `exec /otelcol-contrib: no such
    // file or directory`. Stay on the older binary layout until that settles.
    public string OtelCollectorImageTag { get; set; } = "0.111.0";

    public string PostgresExporterImage { get; set; } = "quay.io/prometheuscommunity/postgres-exporter";
    public string PostgresExporterImageTag { get; set; } = "v0.16.0";

    public string CAdvisorImage { get; set; } = "gcr.io/cadvisor/cadvisor";
    public string CAdvisorImageTag { get; set; } = "v0.49.1";

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
    public string PrometheusRetention { get; set; } = "15d";

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
    public string AspireDashboardOtlpEndpoint { get; set; } = "host.docker.internal:18889";
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
