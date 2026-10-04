using System.Globalization;
using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Observability;

namespace Nextended.Aspire.Hosting.Grafana;

/// <summary>
/// Shared state of one observability stack. The fluent <c>With*</c> extensions only
/// record components and settings here; all cross-component work — YAML config
/// generation, WaitFor ordering, dashboard parent grouping — happens once in a
/// <see cref="BeforeStartEvent"/> subscription. That makes the fluent calls
/// order-independent: <c>WithOtelCollector()</c> before <c>WithTempo()</c> still
/// wires the collector's Tempo exporter correctly.
/// </summary>
internal sealed class ObservabilityStackContext
{
    public required string ConfigRootPath { get; init; }

    /// <summary>
    /// Publish mode bakes configs into images instead of bind-mounting them and publishes the
    /// internal components as TCP — see <see cref="MountConfig{T}"/> and <see cref="InternalScheme"/>.
    /// </summary>
    public bool IsPublishMode { get; init; }

    public string GeneratedDir => Path.Combine(ConfigRootPath, ".generated");
    public string GrafanaProvisioningDir => Path.Combine(GeneratedDir, "grafana", "provisioning");

    public IResourceBuilder<GrafanaResource>? Grafana { get; set; }
    public IResourceBuilder<PrometheusResource>? Prometheus { get; set; }
    public IResourceBuilder<LokiResource>? Loki { get; set; }
    public IResourceBuilder<TempoResource>? Tempo { get; set; }
    public IResourceBuilder<OtelCollectorResource>? OtelCollector { get; set; }
    public IResourceBuilder<PromtailResource>? Promtail { get; set; }
    public IResourceBuilder<CAdvisorResource>? CAdvisor { get; set; }
    public IResourceBuilder<PostgresExporterResource>? PostgresExporter { get; set; }

    /// <summary>Explicitly added datasources (via <c>WithDatasource</c> / <c>WithPostgresDatasource</c>).</summary>
    public List<GrafanaDatasource> Datasources { get; } = [];

    /// <summary>Scrape jobs beyond the Prometheus self-scrape (exporters, cAdvisor, user targets).</summary>
    public List<PrometheusScrapeJob> ScrapeJobs { get; } = [];

    public string PrometheusRetention { get; set; } = GrafanaStackDefaults.PrometheusRetention;
    public string AspireDashboardOtlpEndpoint { get; set; } = GrafanaStackDefaults.AspireDashboardOtlpEndpoint;

    /// <summary>Host folder with dashboard JSONs that gets bind-mounted into Grafana; null = no dashboards mount.</summary>
    public string? DashboardsMountPath { get; set; }

    /// <summary>Sidebar folder name for auto-provisioned dashboards.</summary>
    public string DashboardsFolderName { get; set; } = "Application";

    /// <summary>S3 bucket for Loki; null keeps chunks on the container filesystem.</summary>
    public S3StorageOptions? LokiStorage { get; set; }

    /// <summary>S3 bucket for Tempo; null keeps trace blocks on the container filesystem.</summary>
    public S3StorageOptions? TempoStorage { get; set; }

    /// <summary>
    /// Scheme for the internal components' endpoints. Locally plain HTTP. In publish mode TCP:
    /// Azure Container Apps routes an HTTP ingress only through 443 as
    /// <c>https://{name}.internal.{env-domain}</c>, while TCP ingress keeps
    /// <c>{name}:{port}</c> — the Docker-network addresses every generated config already uses.
    /// </summary>
    public string InternalScheme => IsPublishMode ? "tcp" : "http";

    /// <summary>Fixed port in publish mode (TCP ingress exposes the target port as is), Aspire's choice locally.</summary>
    public int? InternalPort(int targetPort) => IsPublishMode ? targetPort : null;

    private readonly Dictionary<string, (IResourceBuilder<ContainerResource> Builder, List<(string Source, string Target)> Files)> _baked = new();

    /// <summary>
    /// Run mode: read-only bind mount. Publish mode: the file or folder is COPY'd into an image
    /// built FROM the component's final image (see <see cref="BakeImages"/>) — Azure Container Apps
    /// has no host to mount from, and azd fails the bicep step on a bind mount.
    /// </summary>
    public IResourceBuilder<T> MountConfig<T>(IResourceBuilder<T> resource, string source, string target)
        where T : ContainerResource
    {
        if (!IsPublishMode) return resource.WithBindMount(source, target, isReadOnly: true);

        if (!_baked.TryGetValue(resource.Resource.Name, out var entry))
            _baked[resource.Resource.Name] = entry = (resource, []);
        entry.Files.Add((source, target));
        return resource;
    }

    private bool _subscribed;

    /// <summary>Subscribes the config-generation/wiring hook exactly once per stack.</summary>
    public void EnsureSubscribed(IDistributedApplicationBuilder builder)
    {
        if (_subscribed) return;
        _subscribed = true;

        builder.Eventing.Subscribe<BeforeStartEvent>((_, _) =>
        {
            WriteConfigs();
            WireRelationships();
            return Task.CompletedTask;
        });
    }

    // -------------------------------------------------------------------------
    // Config generation — everything lands under {ConfigRootPath}/.generated so
    // users can inspect what the containers actually loaded. Stale files from a
    // previously-enabled component are deleted (bind mounts are registered only
    // for present components, but a leftover file would be confusing).
    // -------------------------------------------------------------------------
    private void WriteConfigs()
    {
        Directory.CreateDirectory(GeneratedDir);

        WriteIfNeeded(Path.Combine(GeneratedDir, "prometheus.yml"),
            () => StackConfigGenerator.GetPrometheusYaml(ScrapeJobs, PrometheusRetention),
            Prometheus is not null);

        WriteIfNeeded(Path.Combine(GeneratedDir, "loki-config.yml"),
            () => StackConfigGenerator.GetLokiConfigYaml(LokiStorage),
            Loki is not null);

        WriteIfNeeded(Path.Combine(GeneratedDir, "promtail-config.yml"),
            () => StackConfigGenerator.GetPromtailConfigYaml(Loki!.Resource.Name),
            Promtail is not null && Loki is not null);

        WriteIfNeeded(Path.Combine(GeneratedDir, "tempo-config.yml"),
            () => StackConfigGenerator.GetTempoConfigYaml(Prometheus?.Resource.Name, TempoStorage),
            Tempo is not null);

        // The default mirror target is the local Aspire dashboard on host.docker.internal, which
        // does not exist once deployed; only an explicitly configured endpoint survives publishing.
        var aspireMirror = IsPublishMode && AspireDashboardOtlpEndpoint == GrafanaStackDefaults.AspireDashboardOtlpEndpoint
            ? null
            : AspireDashboardOtlpEndpoint;

        WriteIfNeeded(Path.Combine(GeneratedDir, "otel-collector-config.yml"),
            () => StackConfigGenerator.GetOtelCollectorConfigYaml(
                Tempo?.Resource.Name, Loki?.Resource.Name, Prometheus?.Resource.Name, aspireMirror,
                debugVerbosity: IsPublishMode ? "basic" : "detailed"),
            OtelCollector is not null);

        if (Grafana is not null)
        {
            Directory.CreateDirectory(Path.Combine(GrafanaProvisioningDir, "datasources"));
            Directory.CreateDirectory(Path.Combine(GrafanaProvisioningDir, "dashboards"));

            File.WriteAllText(
                Path.Combine(GrafanaProvisioningDir, "datasources", "datasources.yml"),
                StackConfigGenerator.GetGrafanaDatasourcesYaml(AllDatasources()));
            File.WriteAllText(
                Path.Combine(GrafanaProvisioningDir, "dashboards", "dashboards.yml"),
                StackConfigGenerator.GetGrafanaDashboardsProvisioningYaml(DashboardsFolderName));

            // Bind-mount sources must exist when the container starts.
            if (DashboardsMountPath is not null)
                Directory.CreateDirectory(DashboardsMountPath);
        }

        if (IsPublishMode) BakeImages();
    }

    /// <summary>
    /// One build context per component under <c>.generated/publish/{name}</c>: a Dockerfile FROM the
    /// component's final image (so <c>WithImage</c> overrides still apply) that COPYs every file
    /// <see cref="MountConfig{T}"/> would have bind-mounted. Runs after the configs are written.
    /// </summary>
    private void BakeImages()
    {
        var publishRoot = Path.Combine(GeneratedDir, "publish");
        foreach (var (name, (builder, files)) in _baked)
        {
            var image = builder.Resource.Annotations.OfType<ContainerImageAnnotation>().LastOrDefault()
                ?? throw new InvalidOperationException($"{name}: no container image to build from.");
            var from = string.IsNullOrEmpty(image.Registry) ? image.Image : $"{image.Registry}/{image.Image}";
            from += string.IsNullOrEmpty(image.SHA256) ? $":{image.Tag ?? "latest"}" : $"@sha256:{image.SHA256}";

            var context = Path.Combine(publishRoot, name);
            if (Directory.Exists(context)) Directory.Delete(context, recursive: true);
            Directory.CreateDirectory(context);

            var dockerfile = new StringBuilder().Append("FROM ").AppendLine(from);
            for (var i = 0; i < files.Count; i++)
            {
                var (source, target) = files[i];
                var staged = Path.Combine(context, "files", i.ToString(CultureInfo.InvariantCulture));
                if (Directory.Exists(source))
                {
                    CopyDirectory(source, staged);
                    dockerfile.AppendLine(CultureInfo.InvariantCulture, $"COPY files/{i}/ {target.TrimEnd('/')}/");
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(staged)!);
                    File.Copy(source, staged);
                    dockerfile.AppendLine(CultureInfo.InvariantCulture, $"COPY files/{i} {target}");
                }
            }

            File.WriteAllText(Path.Combine(context, "Dockerfile"), dockerfile.ToString());
            builder.WithDockerfile(context);
        }
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, dir)));
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            File.Copy(file, Path.Combine(target, Path.GetRelativePath(source, file)), overwrite: true);
        // An empty folder would leave COPY without a source.
        if (!Directory.EnumerateFileSystemEntries(target).Any())
            File.WriteAllText(Path.Combine(target, ".keep"), "");
    }

    /// <summary>Auto-datasources for present components, then user-supplied ones.</summary>
    private IEnumerable<GrafanaDatasource> AllDatasources()
    {
        var userHasDefault = Datasources.Any(d => d.IsDefault);

        if (Prometheus is not null)
        {
            yield return new GrafanaDatasource
            {
                Name = "Prometheus",
                Uid = "prometheus",
                Type = "prometheus",
                Url = $"http://{Prometheus.Resource.Name}:{PrometheusResource.DefaultTargetPort}",
                IsDefault = !userHasDefault,
            };
        }

        if (Loki is not null)
        {
            yield return new GrafanaDatasource
            {
                Name = "Loki",
                Uid = "loki",
                Type = "loki",
                Url = $"http://{Loki.Resource.Name}:{LokiResource.DefaultTargetPort}",
            };
        }

        if (Tempo is not null)
        {
            var tempo = new GrafanaDatasource
            {
                Name = "Tempo",
                Uid = "tempo",
                Type = "tempo",
                Url = $"http://{Tempo.Resource.Name}:{TempoResource.DefaultTargetPort}",
            };
            if (Loki is not null)
            {
                tempo.JsonData["tracesToLogsV2"] = new Dictionary<string, object?>
                {
                    ["datasourceUid"] = "loki",
                    ["spanStartTimeShift"] = "-1h",
                    ["spanEndTimeShift"] = "1h",
                    ["filterByTraceID"] = false,
                    ["filterBySpanID"] = false,
                    ["tags"] = new List<object?>
                    {
                        new Dictionary<string, object?> { ["key"] = "service.name", ["value"] = "container" },
                    },
                };
            }
            if (Prometheus is not null)
                tempo.JsonData["serviceMap"] = new Dictionary<string, object?> { ["datasourceUid"] = "prometheus" };
            tempo.JsonData["nodeGraph"] = new Dictionary<string, object?> { ["enabled"] = true };

            yield return tempo;
        }

        foreach (var ds in Datasources)
            yield return ds;
    }

    // -------------------------------------------------------------------------
    // Start ordering + dashboard grouping. Done here (not in the With* calls) so
    // the relationships exist regardless of registration order.
    // -------------------------------------------------------------------------
    private void WireRelationships()
    {
        if (Prometheus is not null && PostgresExporter is not null) Prometheus.WaitFor(PostgresExporter);
        if (Promtail is not null && Loki is not null) Promtail.WaitFor(Loki);

        if (OtelCollector is not null)
        {
            if (Tempo is not null) OtelCollector.WaitFor(Tempo);
            if (Loki is not null) OtelCollector.WaitFor(Loki);
            if (Prometheus is not null) OtelCollector.WaitFor(Prometheus);
        }

        if (Grafana is null) return;

        if (Prometheus is not null) Grafana.WaitFor(Prometheus);
        if (Loki is not null) Grafana.WaitFor(Loki);

        // Group everything under Grafana in the Aspire dashboard.
        foreach (var child in new IResourceBuilder<ContainerResource>?[]
                 { PostgresExporter, Prometheus, Loki, Promtail, Tempo, OtelCollector, CAdvisor })
        {
            child?.WithParentRelationship(Grafana.Resource);
        }
    }

    /// <summary>Writes the config only when the component is enabled; deletes stale files otherwise.</summary>
    private static void WriteIfNeeded(string path, Func<string> content, bool enabled)
    {
        if (enabled)
            File.WriteAllText(path, content());
        else if (File.Exists(path))
            File.Delete(path);
    }
}

/// <summary>Single source for image versions and stack defaults — options and fluent API share these.</summary>
internal static class GrafanaStackDefaults
{
    public const string GrafanaImage = "grafana/grafana";
    public const string GrafanaImageTag = "11.5.0";

    public const string PrometheusImage = "prom/prometheus";
    public const string PrometheusImageTag = "v2.55.1";

    public const string LokiImage = "grafana/loki";
    public const string LokiImageTag = "3.3.0";

    public const string PromtailImage = "grafana/promtail";
    public const string PromtailImageTag = "3.3.0";

    public const string TempoImage = "grafana/tempo";
    public const string TempoImageTag = "2.6.0";

    public const string OtelCollectorImage = "otel/opentelemetry-collector-contrib";
    // 0.111.0 is the last release where the binary sat at `/otelcol-contrib`.
    // Starting with 0.112+, it moved to `/usr/local/bin/otelcol-contrib`, which
    // breaks docker-startup on some setups with: `exec /otelcol-contrib: no such
    // file or directory`. Stay on the older binary layout until that settles.
    public const string OtelCollectorImageTag = "0.111.0";

    public const string PostgresExporterImage = "quay.io/prometheuscommunity/postgres-exporter";
    public const string PostgresExporterImageTag = "v0.16.0";

    public const string CAdvisorImage = "gcr.io/cadvisor/cadvisor";
    public const string CAdvisorImageTag = "v0.49.1";

    public const string PrometheusRetention = "15d";
    public const string AspireDashboardOtlpEndpoint = "host.docker.internal:18889";
}
