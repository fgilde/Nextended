using System.Globalization;
using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.SigNoz;

/// <summary>
/// Shared state of one SigNoz stack. The fluent calls only record settings; the files the containers read —
/// ClickHouse's configuration, the collector's, the provisioning scripts, dashboards and alert rules — are written
/// once when the app host starts (<see cref="BeforeStartEvent"/>), so the calls work in any order.
/// </summary>
internal sealed class SigNozStack
{
    /// <summary>Exposed ports in a deployment: a TCP port is unique per Container Apps environment, and Grafana's collector already takes 4317/4318.</summary>
    public const int PublishedClickHousePort = 19000;
    public const int PublishedOtlpHttpPort = 14318;
    public const int PublishedOpAmpPort = 14320;

    public const string ClickHouseConfigDir = "/etc/clickhouse-server/signoz";
    public const string CollectorConfigDir = "/etc/signoz-collector";
    public const string ProvisioningDir = "/etc/signoz-provisioning";

    public required string Name { get; init; }
    public required string ConfigRootPath { get; init; }
    public bool IsPublishMode { get; init; }

    public string GeneratedDir => Path.Combine(ConfigRootPath, ".generated", "signoz", Name);

    public IResourceBuilder<SigNozResource> SigNoz { get; set; } = null!;
    public IResourceBuilder<SigNozClickHouseResource> ClickHouse { get; set; } = null!;
    public IResourceBuilder<SigNozCollectorResource> Collector { get; set; } = null!;
    public IResourceBuilder<ContainerResource>? SignIn { get; set; }

    /// <summary>Folders of the app host with more dashboards (SigNoz JSON, schema v6) and alert rules.</summary>
    public List<string> DashboardFolders { get; } = [];
    public List<string> AlertFolders { get; } = [];

    /// <summary>The dashboards and alert rules of this package.</summary>
    public bool BuiltInContent { get; set; } = true;

    /// <summary>The collector takes its remote configuration (log pipelines, ...) from SigNoz.</summary>
    public bool OpAmp => !IsPublishMode;

    private readonly Dictionary<string, (IResourceBuilder<ContainerResource> Builder, List<(string Source, string Target)> Files)> _baked = new();

    /// <summary>Run mode: read-only bind mount. Publish mode: COPY'd into an image built FROM the component's image — Container Apps has no host to mount from.</summary>
    public IResourceBuilder<T> MountConfig<T>(IResourceBuilder<T> resource, string source, string target)
        where T : ContainerResource
    {
        if (!IsPublishMode)
        {
            return resource.WithBindMount(source, target, isReadOnly: true);
        }

        if (!_baked.TryGetValue(resource.Resource.Name, out var entry))
        {
            _baked[resource.Resource.Name] = entry = (resource, []);
        }

        entry.Files.Add((source, target));
        return resource;
    }

    public void Subscribe(IDistributedApplicationBuilder builder) =>
        builder.Eventing.Subscribe<BeforeStartEvent>((_, _) =>
        {
            WriteConfigs();
            WireRelationships();
            return Task.CompletedTask;
        });

    internal void WriteConfigs()
    {
        var clickHouse = Recreate(Path.Combine(GeneratedDir, "clickhouse"));
        Write(clickHouse, "config.yaml", Content("clickhouse/config.yaml"));
        Write(clickHouse, "functions.yaml", Content("clickhouse/functions.yaml"));
        Write(clickHouse, "start.sh", Content("clickhouse/start.sh"));

        var collector = Recreate(Path.Combine(GeneratedDir, "collector"));
        Write(collector, "config.yaml", Content("collector/config.yaml"));
        var opAmpPort = IsPublishMode ? PublishedOpAmpPort : SigNozResource.OpAmpTargetPort;
        Write(collector, "opamp.yaml", $"server_endpoint: ws://{SigNoz.Resource.Name}:{opAmpPort}/v1/opamp\n");

        var provisioning = Recreate(Path.Combine(GeneratedDir, "signoz"));
        Write(provisioning, "start.sh", Content("signoz/start.sh"));
        Write(provisioning, "provision.sh", Content("signoz/provision.sh"));
        var dashboards = Directory.CreateDirectory(Path.Combine(provisioning, "dashboards")).FullName;
        var alerts = Directory.CreateDirectory(Path.Combine(provisioning, "alerts")).FullName;
        if (BuiltInContent)
        {
            foreach (var (path, content) in EmbeddedContent())
            {
                if (path.StartsWith("dashboards/", StringComparison.Ordinal))
                {
                    Write(dashboards, path["dashboards/".Length..], content);
                }
                else if (path.StartsWith("alerts/", StringComparison.Ordinal))
                {
                    Write(alerts, path["alerts/".Length..], content);
                }
            }
        }

        CopyJson(DashboardFolders, dashboards);
        CopyJson(AlertFolders, alerts);

        if (IsPublishMode)
        {
            BakeImages();
        }
    }

    private void WireRelationships()
    {
        foreach (var child in new IResourceBuilder<ContainerResource>?[] { ClickHouse, Collector, SignIn })
        {
            child?.WithParentRelationship(SigNoz.Resource);
        }
    }

    // app host folders win over the built-in files of the same name
    private static void CopyJson(IEnumerable<string> folders, string target)
    {
        foreach (var folder in folders.Where(Directory.Exists))
        {
            foreach (var file in Directory.EnumerateFiles(folder, "*.json"))
            {
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
            }
        }
    }

    private static string Recreate(string dir)
    {
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }

        return Directory.CreateDirectory(dir).FullName;
    }

    // LF only: a git checkout on Windows may hand the scripts over with CRLF, which sh does not run
    private static void Write(string dir, string file, string content) =>
        File.WriteAllText(Path.Combine(dir, file), content.Replace("\r\n", "\n", StringComparison.Ordinal), new UTF8Encoding(false));

    internal static string Content(string path) =>
        EmbeddedContent().First(c => c.Path == path).Content;

    internal static IEnumerable<(string Path, string Content)> EmbeddedContent()
    {
        var assembly = typeof(SigNozStack).Assembly;
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith("SigNoz/", StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            yield return (name["SigNoz/".Length..].Replace('\\', '/'), reader.ReadToEnd());
        }
    }

    /// <summary>
    /// One build context per component under <c>.generated/signoz/{name}/publish/{resource}</c>: a Dockerfile FROM the component's
    /// final image (so <c>WithImage</c> overrides still apply) that COPYs every folder <see cref="MountConfig{T}"/> would have bind-mounted.
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

            var context = Recreate(Path.Combine(publishRoot, name));
            var dockerfile = new StringBuilder().Append("FROM ").AppendLine(from);
            for (var i = 0; i < files.Count; i++)
            {
                var (source, target) = files[i];
                var staged = Path.Combine(context, "files", i.ToString(CultureInfo.InvariantCulture));
                CopyDirectory(source, staged);
                dockerfile.AppendLine(CultureInfo.InvariantCulture, $"COPY files/{i}/ {target.TrimEnd('/')}/");
            }

            File.WriteAllText(Path.Combine(context, "Dockerfile"), dockerfile.ToString());
            builder.WithDockerfile(context);
        }
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, dir)));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(target, Path.GetRelativePath(source, file)), overwrite: true);
        }

        // an empty folder would leave COPY without a source
        if (!Directory.EnumerateFileSystemEntries(target).Any())
        {
            File.WriteAllText(Path.Combine(target, ".keep"), "");
        }
    }
}

/// <summary>Images and versions, from SigNoz's own deployment (github.com/SigNoz/foundry, docs/examples/docker/compose).</summary>
internal static class SigNozDefaults
{
    public const string SigNozImage = "signoz/signoz";
    public const string SigNozTag = "v0.151.0";

    public const string CollectorImage = "signoz/signoz-otel-collector";
    public const string CollectorTag = "v0.144.18";

    public const string ClickHouseImage = "clickhouse/clickhouse-server";
    public const string ClickHouseTag = "25.12.5";

    public const string SignInImage = "oauth2-proxy/oauth2-proxy";
    public const string SignInTag = "v7.8.1";
    public const string SignInRegistry = "quay.io";

    public const string AdminEmail = "admin@signoz.local";
}
