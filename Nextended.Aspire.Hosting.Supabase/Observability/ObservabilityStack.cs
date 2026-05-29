using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Resources;

namespace Nextended.Aspire.Hosting.Observability;

/// <summary>
/// Adds a self-contained observability stack to an Aspire application:
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
/// The stack is designed to be project-agnostic — all paths, image versions,
/// resource-name prefixes flow through <see cref="ObservabilityStackOptions"/>
/// so the same code can lift into a NuGet package without changes. YAML
/// configs are GENERATED at startup from the actual resource hostnames (see
/// <see cref="ObservabilityConfigGenerator"/>) and bind-mounted into the
/// containers. No hardcoded hostnames in the file tree.
///
/// Publish-mode (`azd up`): Promtail and cAdvisor are auto-skipped because
/// they rely on the host's Docker socket, which Azure Container Apps doesn't
/// expose. The rest of the stack deploys cleanly.
/// </summary>
public static class ObservabilityStack
{
    /// <summary>
    /// Convenience overload — derives Postgres connection details from the
    /// supplied Supabase stack and lets the caller tweak everything else.
    /// </summary>
    public static IDistributedApplicationBuilder AddObservabilityStack(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<SupabaseStackResource> supabase,
        Action<ObservabilityStackOptions>? configure = null)
    {
        var dbPassword = supabase.Resource.Database?.Resource.Password
            ?? throw new InvalidOperationException("Supabase database password not configured");

        var defaultConfigRoot = Path.GetFullPath(
            Path.Combine(builder.AppHostDirectory, "..", "observability"));

        var options = new ObservabilityStackOptions
        {
            ConfigRootPath = defaultConfigRoot,
            PostgresExporter = new PostgresExporterOptions
            {
                // Use the actual Database container's resource name — keeps things
                // in lock-step if the Supabase library ever changes the suffix.
                Host = supabase.Resource.Database?.Resource.Name ?? $"{supabase.Resource.Name}-db",
                Password = dbPassword,
            },
        };
        configure?.Invoke(options);

        return builder.AddObservabilityStack(options);
    }

    /// <summary>
    /// Adds the observability stack with the supplied options. This is the form
    /// a future NuGet package would expose — no Supabase dependency.
    /// </summary>
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

        // ---------------------------------------------------------------------
        // Generate all YAML configs from the actual hostnames. Written under
        // {ConfigRootPath}/.generated/ so the user can inspect what Grafana /
        // Prometheus / etc. actually loaded. Path is bind-mount-safe (stable
        // across runs).
        // ---------------------------------------------------------------------
        var generatedDir = Path.Combine(options.ConfigRootPath, ".generated");
        Directory.CreateDirectory(generatedDir);

        var hosts = new ObservabilityConfigGenerator.Hostnames(
            Prometheus: options.IncludePrometheus ? N("prometheus") : "",
            Loki: options.IncludeLoki ? N("loki") : "",
            Tempo: options.IncludeTempo ? N("tempo") : "",
            OtelCollector: options.IncludeOtelCollector ? N("otel-collector") : "",
            PostgresExporter: options.PostgresExporter is not null ? N("postgres-exporter") : "",
            CAdvisor: includeCAdvisor ? N("cadvisor") : "",
            PostgresDb: options.PostgresExporter?.Host ?? "",
            PostgresPassword: options.PostgresExporter?.Password ?? "",
            AspireDashboardOtlpEndpoint: options.AspireDashboardOtlpEndpoint);

        WriteIfNeeded(Path.Combine(generatedDir, "prometheus.yml"),
            ObservabilityConfigGenerator.GetPrometheusYaml(hosts, options.PrometheusRetention),
            options.IncludePrometheus);

        WriteIfNeeded(Path.Combine(generatedDir, "loki-config.yml"),
            ObservabilityConfigGenerator.GetLokiConfigYaml(),
            options.IncludeLoki);

        WriteIfNeeded(Path.Combine(generatedDir, "promtail-config.yml"),
            ObservabilityConfigGenerator.GetPromtailConfigYaml(hosts),
            includePromtail);

        WriteIfNeeded(Path.Combine(generatedDir, "tempo-config.yml"),
            ObservabilityConfigGenerator.GetTempoConfigYaml(hosts),
            options.IncludeTempo);

        WriteIfNeeded(Path.Combine(generatedDir, "otel-collector-config.yml"),
            ObservabilityConfigGenerator.GetOtelCollectorConfigYaml(hosts),
            options.IncludeOtelCollector);

        // Grafana provisioning needs both files in the same tree.
        var grafanaProvisioningDir = Path.Combine(generatedDir, "grafana", "provisioning");
        Directory.CreateDirectory(Path.Combine(grafanaProvisioningDir, "datasources"));
        Directory.CreateDirectory(Path.Combine(grafanaProvisioningDir, "dashboards"));
        File.WriteAllText(
            Path.Combine(grafanaProvisioningDir, "datasources", "datasources.yml"),
            ObservabilityConfigGenerator.GetGrafanaDatasourcesYaml(hosts));
        File.WriteAllText(
            Path.Combine(grafanaProvisioningDir, "dashboards", "dashboards.yml"),
            ObservabilityConfigGenerator.GetGrafanaDashboardsProvisioningYaml(options.GrafanaDashboardsFolder));

        // ---------------------------------------------------------------------
        // postgres_exporter (only when explicitly configured)
        // ---------------------------------------------------------------------
        IResourceBuilder<ContainerResource>? postgresExporter = null;
        if (options.PostgresExporter is { } pgOpt)
        {
            postgresExporter = builder
                .AddContainer(N("postgres-exporter"), options.PostgresExporterImage, options.PostgresExporterImageTag)
                .WithEndpoint(targetPort: 9187, name: "metrics", scheme: "http")
                .WithEnvironment("DATA_SOURCE_NAME", pgOpt.ToDataSourceName());
        }

        // ---------------------------------------------------------------------
        // cAdvisor (local-only — needs Docker socket access)
        // ---------------------------------------------------------------------
        IResourceBuilder<ContainerResource>? cadvisor = null;
        if (includeCAdvisor)
        {
            cadvisor = builder
                .AddContainer(N("cadvisor"), options.CAdvisorImage, options.CAdvisorImageTag)
                .WithEndpoint(targetPort: 8080, name: "metrics", scheme: "http")
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
        }

        // ---------------------------------------------------------------------
        // Prometheus
        // ---------------------------------------------------------------------
        IResourceBuilder<ContainerResource>? prometheus = null;
        if (options.IncludePrometheus)
        {
            prometheus = builder
                .AddContainer(N("prometheus"), options.PrometheusImage, options.PrometheusImageTag)
                .WithEndpoint(targetPort: 9090, name: "http", scheme: "http")
                .WithBindMount(
                    Path.Combine(generatedDir, "prometheus.yml"),
                    "/etc/prometheus/prometheus.yml",
                    isReadOnly: true)
                .WithArgs("--config.file=/etc/prometheus/prometheus.yml",
                          "--storage.tsdb.path=/prometheus",
                          $"--storage.tsdb.retention.time={options.PrometheusRetention}",
                          "--web.enable-lifecycle",
                          // Required so OTel-Collector can push via remote_write.
                          "--web.enable-remote-write-receiver");
            if (postgresExporter is not null) prometheus = prometheus.WaitFor(postgresExporter);
        }

        // ---------------------------------------------------------------------
        // Loki
        // ---------------------------------------------------------------------
        IResourceBuilder<ContainerResource>? loki = null;
        if (options.IncludeLoki)
        {
            loki = builder
                .AddContainer(N("loki"), options.LokiImage, options.LokiImageTag)
                .WithEndpoint(targetPort: 3100, name: "http", scheme: "http")
                .WithBindMount(
                    Path.Combine(generatedDir, "loki-config.yml"),
                    "/etc/loki/local-config.yaml",
                    isReadOnly: true)
                .WithArgs("-config.file=/etc/loki/local-config.yaml");
        }

        // ---------------------------------------------------------------------
        // Promtail (local-only — needs Docker socket)
        // ---------------------------------------------------------------------
        IResourceBuilder<ContainerResource>? promtail = null;
        if (includePromtail && loki is not null)
        {
            promtail = builder
                .AddContainer(N("promtail"), options.PromtailImage, options.PromtailImageTag)
                .WithBindMount(
                    Path.Combine(generatedDir, "promtail-config.yml"),
                    "/etc/promtail/config.yml",
                    isReadOnly: true)
                .WithBindMount("/var/run/docker.sock", "/var/run/docker.sock", isReadOnly: true)
                .WithArgs("-config.file=/etc/promtail/config.yml")
                .WaitFor(loki);
        }

        // ---------------------------------------------------------------------
        // Tempo (opt-in)
        // ---------------------------------------------------------------------
        IResourceBuilder<ContainerResource>? tempo = null;
        if (options.IncludeTempo)
        {
            tempo = builder
                .AddContainer(N("tempo"), options.TempoImage, options.TempoImageTag)
                .WithEndpoint(targetPort: 3200, name: "http", scheme: "http")
                .WithEndpoint(targetPort: 4317, name: "otlp-grpc", scheme: "http")
                .WithBindMount(
                    Path.Combine(generatedDir, "tempo-config.yml"),
                    "/etc/tempo/tempo.yml",
                    isReadOnly: true)
                .WithArgs("-config.file=/etc/tempo/tempo.yml");
        }

        // ---------------------------------------------------------------------
        // OTel-Collector (opt-in)
        // ---------------------------------------------------------------------
        IResourceBuilder<ContainerResource>? otelCollector = null;
        if (options.IncludeOtelCollector)
        {
            otelCollector = builder
                .AddContainer(N("otel-collector"), options.OtelCollectorImage, options.OtelCollectorImageTag)
                .WithEndpoint(targetPort: 4317, name: "otlp-grpc", scheme: "http")
                .WithEndpoint(targetPort: 4318, name: "otlp-http", scheme: "http")
                .WithBindMount(
                    Path.Combine(generatedDir, "otel-collector-config.yml"),
                    "/etc/otelcol-contrib/config.yaml",
                    isReadOnly: true);
            if (tempo is not null) otelCollector = otelCollector.WaitFor(tempo);
            if (loki is not null) otelCollector = otelCollector.WaitFor(loki);
            if (prometheus is not null) otelCollector = otelCollector.WaitFor(prometheus);
        }

        // ---------------------------------------------------------------------
        // Grafana — UI / dashboards. Becomes the parent of every other monitoring
        // resource so the Aspire dashboard shows the stack as one card.
        // ---------------------------------------------------------------------
        if (options.IncludeGrafana)
        {
            var grafana = builder
                .AddContainer(N("grafana"), options.GrafanaImage, options.GrafanaImageTag)
                .WithEndpoint(targetPort: 3000, name: "http", scheme: "http")
                .WithEnvironment("GF_USERS_DEFAULT_THEME", "dark")
                .WithBindMount(
                    grafanaProvisioningDir,
                    "/etc/grafana/provisioning",
                    isReadOnly: true)
                // Dashboards path is explicitly configurable so consumers of the
                // package (NuGet scenario) can point at any folder they like —
                // no implicit "must live under ConfigRootPath" assumption.
                .WithBindMount(
                    options.DashboardsPath ?? Path.Combine(options.ConfigRootPath, "grafana", "dashboards"),
                    "/var/lib/grafana/dashboards",
                    isReadOnly: true)
                .WithExternalHttpEndpoints();

            if (options.GrafanaAnonymousAdmin)
            {
                grafana = grafana
                    .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
                    .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin")
                    .WithEnvironment("GF_AUTH_DISABLE_LOGIN_FORM", "true");
            }
            else if (!string.IsNullOrEmpty(options.GrafanaAdminPassword))
            {
                grafana = grafana
                    .WithEnvironment("GF_SECURITY_ADMIN_USER", options.GrafanaAdminUser)
                    .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", options.GrafanaAdminPassword);
            }

            if (options.PostgresExporter is { } pg)
            {
                grafana = grafana
                    .WithEnvironment("APP_DB_HOST", pg.Host)
                    .WithEnvironment("APP_DB_PASSWORD", pg.Password);
            }

            if (prometheus is not null) grafana = grafana.WaitFor(prometheus);
            if (loki is not null) grafana = grafana.WaitFor(loki);

            // Group everything under Grafana in the Aspire dashboard.
            if (postgresExporter is not null) postgresExporter.WithParentRelationship(grafana.Resource);
            if (prometheus is not null) prometheus.WithParentRelationship(grafana.Resource);
            if (loki is not null) loki.WithParentRelationship(grafana.Resource);
            if (promtail is not null) promtail.WithParentRelationship(grafana.Resource);
            if (tempo is not null) tempo.WithParentRelationship(grafana.Resource);
            if (otelCollector is not null) otelCollector.WithParentRelationship(grafana.Resource);
            if (cadvisor is not null) cadvisor.WithParentRelationship(grafana.Resource);
        }

        return builder;
    }

    /// <summary>Writes the YAML only when the corresponding component is enabled.
    /// Keeps stale configs out of the generated tree when a feature is toggled off.</summary>
    private static void WriteIfNeeded(string path, string content, bool enabled)
    {
        if (enabled)
        {
            File.WriteAllText(path, content);
        }
        else if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
