using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.Grafana;

/// <summary>
/// Container factories for every stack component. Shared by the fluent
/// <c>With*</c> extensions and the options-based <c>AddObservabilityStack</c> so
/// both paths create byte-identical resources.
/// </summary>
internal static class StackComponents
{
    public static IResourceBuilder<GrafanaResource> AddGrafana(
        IDistributedApplicationBuilder builder, ObservabilityStackContext ctx, string name)
    {
        var grafana = builder.AddResource(new GrafanaResource(name) { Context = ctx })
            .WithImage(GrafanaStackDefaults.GrafanaImage, GrafanaStackDefaults.GrafanaImageTag)
            .WithEndpoint(targetPort: GrafanaResource.DefaultTargetPort, name: GrafanaResource.HttpEndpointName, scheme: "http")
            .WithEnvironment("GF_USERS_DEFAULT_THEME", "dark")
            .WithBindMount(ctx.GrafanaProvisioningDir, "/etc/grafana/provisioning", isReadOnly: true)
            .WithExternalHttpEndpoints();

        ctx.Grafana = grafana;
        ctx.EnsureSubscribed(builder);
        return grafana;
    }

    public static IResourceBuilder<PrometheusResource> AddPrometheus(
        IDistributedApplicationBuilder builder, ObservabilityStackContext ctx, string name)
    {
        var prometheus = builder.AddResource(new PrometheusResource(name) { Context = ctx })
            .WithImage(GrafanaStackDefaults.PrometheusImage, GrafanaStackDefaults.PrometheusImageTag)
            .WithEndpoint(targetPort: PrometheusResource.DefaultTargetPort, name: PrometheusResource.HttpEndpointName, scheme: "http")
            .WithBindMount(
                Path.Combine(ctx.GeneratedDir, "prometheus.yml"),
                "/etc/prometheus/prometheus.yml",
                isReadOnly: true)
            // Args via callback so a retention changed after this call still wins.
            .WithArgs(args =>
            {
                args.Args.Add("--config.file=/etc/prometheus/prometheus.yml");
                args.Args.Add("--storage.tsdb.path=/prometheus");
                args.Args.Add($"--storage.tsdb.retention.time={ctx.PrometheusRetention}");
                args.Args.Add("--web.enable-lifecycle");
                // Required so OTel-Collector can push via remote_write.
                args.Args.Add("--web.enable-remote-write-receiver");
            });

        ctx.Prometheus = prometheus;
        ctx.EnsureSubscribed(builder);
        return prometheus;
    }

    public static IResourceBuilder<LokiResource> AddLoki(
        IDistributedApplicationBuilder builder, ObservabilityStackContext ctx, string name)
    {
        var loki = builder.AddResource(new LokiResource(name) { Context = ctx })
            .WithImage(GrafanaStackDefaults.LokiImage, GrafanaStackDefaults.LokiImageTag)
            .WithEndpoint(targetPort: LokiResource.DefaultTargetPort, name: LokiResource.HttpEndpointName, scheme: "http")
            .WithBindMount(
                Path.Combine(ctx.GeneratedDir, "loki-config.yml"),
                "/etc/loki/local-config.yaml",
                isReadOnly: true)
            .WithArgs("-config.file=/etc/loki/local-config.yaml");

        ctx.Loki = loki;
        ctx.EnsureSubscribed(builder);
        return loki;
    }

    public static IResourceBuilder<PromtailResource> AddPromtail(
        IDistributedApplicationBuilder builder, ObservabilityStackContext ctx, string name)
    {
        var promtail = builder.AddResource(new PromtailResource(name))
            .WithImage(GrafanaStackDefaults.PromtailImage, GrafanaStackDefaults.PromtailImageTag)
            .WithBindMount(
                Path.Combine(ctx.GeneratedDir, "promtail-config.yml"),
                "/etc/promtail/config.yml",
                isReadOnly: true)
            .WithBindMount("/var/run/docker.sock", "/var/run/docker.sock", isReadOnly: true)
            .WithArgs("-config.file=/etc/promtail/config.yml");

        ctx.Promtail = promtail;
        ctx.EnsureSubscribed(builder);
        return promtail;
    }

    public static IResourceBuilder<TempoResource> AddTempo(
        IDistributedApplicationBuilder builder, ObservabilityStackContext ctx, string name)
    {
        var tempo = builder.AddResource(new TempoResource(name) { Context = ctx })
            .WithImage(GrafanaStackDefaults.TempoImage, GrafanaStackDefaults.TempoImageTag)
            .WithEndpoint(targetPort: TempoResource.DefaultTargetPort, name: TempoResource.HttpEndpointName, scheme: "http")
            .WithEndpoint(targetPort: 4317, name: TempoResource.OtlpGrpcEndpointName, scheme: "http")
            .WithBindMount(
                Path.Combine(ctx.GeneratedDir, "tempo-config.yml"),
                "/etc/tempo/tempo.yml",
                isReadOnly: true)
            .WithArgs("-config.file=/etc/tempo/tempo.yml");

        ctx.Tempo = tempo;
        ctx.EnsureSubscribed(builder);
        return tempo;
    }

    public static IResourceBuilder<OtelCollectorResource> AddOtelCollector(
        IDistributedApplicationBuilder builder, ObservabilityStackContext ctx, string name)
    {
        var otelCollector = builder.AddResource(new OtelCollectorResource(name) { Context = ctx })
            .WithImage(GrafanaStackDefaults.OtelCollectorImage, GrafanaStackDefaults.OtelCollectorImageTag)
            .WithEndpoint(targetPort: OtelCollectorResource.OtlpGrpcTargetPort, name: OtelCollectorResource.OtlpGrpcEndpointName, scheme: "http")
            .WithEndpoint(targetPort: OtelCollectorResource.OtlpHttpTargetPort, name: OtelCollectorResource.OtlpHttpEndpointName, scheme: "http")
            .WithBindMount(
                Path.Combine(ctx.GeneratedDir, "otel-collector-config.yml"),
                "/etc/otelcol-contrib/config.yaml",
                isReadOnly: true);

        ctx.OtelCollector = otelCollector;
        ctx.EnsureSubscribed(builder);
        return otelCollector;
    }

    public static IResourceBuilder<CAdvisorResource> AddCAdvisor(
        IDistributedApplicationBuilder builder, ObservabilityStackContext ctx, string name)
    {
        var cadvisor = builder.AddResource(new CAdvisorResource(name))
            .WithImage(GrafanaStackDefaults.CAdvisorImage, GrafanaStackDefaults.CAdvisorImageTag)
            .WithEndpoint(targetPort: CAdvisorResource.DefaultTargetPort, name: "metrics", scheme: "http")
            .WithBindMount("/var/run", "/var/run", isReadOnly: true)
            .WithBindMount("/sys", "/sys", isReadOnly: true)
            .WithArgs(
                "--docker_only=true",
                "--housekeeping_interval=10s",
                "--store_container_labels=false",
                "--disable_metrics=disk,diskIO,tcp,udp,percpu,sched,process,hugetlb,referenced_memory,resctrl,cpu_topology,memory_numa,advtcp,cpuset")
            // `--device=/dev/kmsg` was removed: it triggers "bind source path does not
            // exist" on Docker Desktop Windows. cAdvisor logs a warning about it but
            // still collects CPU/Memory/Network metrics fine.
            .WithContainerRuntimeArgs("--privileged");

        ctx.CAdvisor = cadvisor;
        ctx.ScrapeJobs.Add(new PrometheusScrapeJob("cadvisor", $"{name}:{CAdvisorResource.DefaultTargetPort}")
        {
            ExtraYaml = """
                metric_relabel_configs:
                  - source_labels: [__name__]
                    regex: 'container_(memory_failures_total|tasks_state)'
                    action: drop
                """,
        });
        ctx.EnsureSubscribed(builder);
        return cadvisor;
    }

    public static IResourceBuilder<PostgresExporterResource> AddPostgresExporter(
        IDistributedApplicationBuilder builder, ObservabilityStackContext ctx, string name, object dataSourceName)
    {
        var exporter = builder.AddResource(new PostgresExporterResource(name))
            .WithImage(GrafanaStackDefaults.PostgresExporterImage, GrafanaStackDefaults.PostgresExporterImageTag)
            .WithEndpoint(targetPort: PostgresExporterResource.DefaultTargetPort, name: "metrics", scheme: "http");

        exporter = dataSourceName switch
        {
            ReferenceExpression expr => exporter.WithEnvironment("DATA_SOURCE_NAME", expr),
            string dsn => exporter.WithEnvironment("DATA_SOURCE_NAME", dsn),
            _ => throw new ArgumentException("DSN must be a string or ReferenceExpression", nameof(dataSourceName)),
        };

        ctx.PostgresExporter = exporter;
        ctx.ScrapeJobs.Add(new PrometheusScrapeJob("postgres", $"{name}:{PostgresExporterResource.DefaultTargetPort}"));
        ctx.EnsureSubscribed(builder);
        return exporter;
    }
}
