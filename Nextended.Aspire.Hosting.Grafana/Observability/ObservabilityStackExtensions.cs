using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Grafana;

namespace Nextended.Aspire.Hosting.Observability;

/// <summary>
/// One-call observability stack for an Aspire application:
///
///   • <b>Grafana</b>          — dashboards UI (acts as the parent resource that
///                               all other monitoring containers nest under in the
///                               Aspire dashboard)
///   • <b>Prometheus</b>       — metrics scraping
///   • <b>postgres_exporter</b> — Postgres internals as Prometheus metrics
///   • <b>Loki</b>             — log aggregation
///   • <b>Promtail</b>         — Docker container log shipper → Loki (local-only)
///   • <b>cAdvisor</b>         — per-container metrics (local-only)
///   • <b>Tempo</b>             — traces backend (opt-in)
///   • <b>OTel-Collector</b>    — OTLP receiver / fan-out (opt-in)
///
/// This is a thin composition over the fluent building blocks in
/// <see cref="GrafanaBuilderExtensions"/> — use those directly for piecemeal
/// setups. YAML configs are GENERATED at startup from the actual resource
/// hostnames and bind-mounted into the containers; no hardcoded hostnames in
/// the file tree.
///
/// Publish-mode (`azd up`): Promtail and cAdvisor are auto-skipped because
/// they rely on the host's Docker socket, which Azure Container Apps doesn't
/// expose. The rest of the stack deploys cleanly.
/// </summary>
public static class ObservabilityStackExtensions
{
    /// <summary>Adds the observability stack with the supplied options.</summary>
    public static IDistributedApplicationBuilder AddObservabilityStack(
        this IDistributedApplicationBuilder builder,
        ObservabilityStackOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ConfigRootPath);

        var prefix = string.IsNullOrEmpty(options.ResourceNamePrefix)
            ? ""
            : $"{options.ResourceNamePrefix}-";

        string N(string name) => $"{prefix}{name}";

        // Auto-skip docker-socket-dependent containers in publish mode (Azure
        // Container Apps doesn't expose a Docker daemon). The user gets a clean
        // partial stack instead of a deploy-time failure.
        var isPublishMode = builder.ExecutionContext.IsPublishMode;
        var includeCAdvisor = options.IncludeCAdvisor && !isPublishMode;
        var includePromtail = options.IncludePromtail && !isPublishMode;

        var ctx = new ObservabilityStackContext
        {
            ConfigRootPath = options.ConfigRootPath,
            PrometheusRetention = options.PrometheusRetention,
            AspireDashboardOtlpEndpoint = options.AspireDashboardOtlpEndpoint,
            DashboardsFolderName = options.GrafanaDashboardsFolder,
        };
        ctx.EnsureSubscribed(builder);

        if (options.PostgresExporter is { } pgOpt)
        {
            StackComponents.AddPostgresExporter(builder, ctx, N("postgres-exporter"), pgOpt.ToDataSourceName())
                .WithImage(options.PostgresExporterImage, options.PostgresExporterImageTag);
        }

        if (includeCAdvisor)
        {
            StackComponents.AddCAdvisor(builder, ctx, N("cadvisor"))
                .WithImage(options.CAdvisorImage, options.CAdvisorImageTag);
        }

        if (options.IncludePrometheus)
        {
            StackComponents.AddPrometheus(builder, ctx, N("prometheus"))
                .WithImage(options.PrometheusImage, options.PrometheusImageTag);
        }

        if (options.IncludeLoki)
        {
            StackComponents.AddLoki(builder, ctx, N("loki"))
                .WithImage(options.LokiImage, options.LokiImageTag);
        }

        if (includePromtail && options.IncludeLoki)
        {
            StackComponents.AddPromtail(builder, ctx, N("promtail"))
                .WithImage(options.PromtailImage, options.PromtailImageTag);
        }

        if (options.IncludeTempo)
        {
            StackComponents.AddTempo(builder, ctx, N("tempo"))
                .WithImage(options.TempoImage, options.TempoImageTag);
        }

        if (options.IncludeOtelCollector)
        {
            StackComponents.AddOtelCollector(builder, ctx, N("otel-collector"))
                .WithImage(options.OtelCollectorImage, options.OtelCollectorImageTag);
        }

        if (options.IncludeGrafana)
        {
            var grafana = StackComponents.AddGrafana(builder, ctx, N("grafana"))
                .WithImage(options.GrafanaImage, options.GrafanaImageTag)
                .WithDashboards(
                    options.DashboardsPath ?? Path.Combine(options.ConfigRootPath, "grafana", "dashboards"),
                    options.GrafanaDashboardsFolder);

            if (options.GrafanaAnonymousAdmin)
            {
                grafana.WithAnonymousAdmin();
            }
            else if (!string.IsNullOrEmpty(options.GrafanaAdminPassword))
            {
                grafana.WithAdminUser(options.GrafanaAdminUser, options.GrafanaAdminPassword);
            }

            if (options.PostgresExporter is { } pg)
            {
                grafana
                    .WithEnvironment("APP_DB_HOST", pg.Host)
                    .WithEnvironment("APP_DB_PASSWORD", pg.Password)
                    .WithDatasource(GrafanaBuilderExtensions.PostgresDatasource(
                        "Postgres", $"{pg.Host}:{pg.Port}", pg.Database,
                        user: pg.Username, passwordRef: "${APP_DB_PASSWORD}", sslMode: pg.SslMode));
            }
        }

        return builder;
    }
}
