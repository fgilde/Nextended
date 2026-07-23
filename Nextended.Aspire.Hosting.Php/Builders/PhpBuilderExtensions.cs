using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.Php;

/// <summary>
/// Fluent API for running PHP endpoints inside your Aspire stack. Start with
/// <see cref="AddPhp"/>, then optionally tune php.ini via
/// <see cref="WithPhpIni(IResourceBuilder{PhpResource}, string, string)"/> or
/// <see cref="WithPhpIniFile"/>.
/// </summary>
public static class PhpBuilderExtensions
{
    /// <summary>
    /// Adds a PHP app served by PHP's built-in web server in the official <c>php:cli</c> container.
    /// <paramref name="path"/> is either a folder (served as docroot — each <c>.php</c> file becomes
    /// an endpoint) or a single <c>.php</c> file (used as router script — every request is handed to
    /// it). Relative paths resolve against the AppHost directory.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">Resource name.</param>
    /// <param name="path">Host path to a folder or a <c>.php</c> file.</param>
    /// <param name="port">Optional fixed host port (default: auto-assigned).</param>
    /// <param name="image">Override the container image (default <c>php</c>).</param>
    /// <param name="tag">Override the image tag (default <c>8.4-cli</c>).</param>
    public static IResourceBuilder<PhpResource> AddPhp(
        this IDistributedApplicationBuilder builder,
        string name,
        string path,
        int? port = null,
        string? image = null,
        string? tag = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path, builder.AppHostDirectory);
        var resource = new PhpResource(name);
        if (Path.GetExtension(fullPath).Equals(".php", StringComparison.OrdinalIgnoreCase))
            resource.RouterScript = Path.GetFileName(fullPath);

        var mountTarget = resource.RouterScript is { } script
            ? $"{PhpResource.AppDirectory}/{script}"
            : PhpResource.AppDirectory;

        return builder.AddResource(resource)
            .WithImage(image ?? PhpResource.DefaultImage, tag ?? PhpResource.DefaultTag)
            .WithHttpEndpoint(port: port, targetPort: PhpResource.DefaultTargetPort, name: PhpResource.HttpEndpointName)
            .WithBindMount(fullPath, mountTarget)
            // The built-in server handles one request per worker; without workers a PHP endpoint
            // calling another endpoint on the same server would deadlock. Override via WithEnvironment.
            .WithEnvironment("PHP_CLI_SERVER_WORKERS", "8")
            // Args via callback so WithPhpIni calls made after AddPhp still end up in the command line.
            .WithArgs(ctx =>
            {
                ctx.Args.Add("php");
                foreach (var (key, value) in resource.IniSettings)
                {
                    ctx.Args.Add("-d");
                    ctx.Args.Add($"{key}={value}");
                }
                ctx.Args.Add("-S");
                ctx.Args.Add($"0.0.0.0:{PhpResource.DefaultTargetPort}");
                ctx.Args.Add("-t");
                ctx.Args.Add(PhpResource.AppDirectory);
                if (resource.RouterScript is { } router)
                    ctx.Args.Add($"{PhpResource.AppDirectory}/{router}");
            });
    }

    /// <summary>
    /// Sets a php.ini directive (passed as <c>php -d key=value</c>), e.g.
    /// <c>WithPhpIni("memory_limit", "256M")</c> or <c>WithPhpIni("display_errors", "1")</c>.
    /// Later calls win over earlier ones for the same key.
    /// </summary>
    public static IResourceBuilder<PhpResource> WithPhpIni(
        this IResourceBuilder<PhpResource> builder, string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        builder.Resource.IniSettings[key] = value;
        return builder;
    }

    /// <summary>Sets multiple php.ini directives at once (see <see cref="WithPhpIni(IResourceBuilder{PhpResource}, string, string)"/>).</summary>
    public static IResourceBuilder<PhpResource> WithPhpIni(
        this IResourceBuilder<PhpResource> builder, IReadOnlyDictionary<string, string> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        foreach (var (key, value) in settings)
            builder.Resource.IniSettings[key] = value;
        return builder;
    }

    /// <summary>
    /// Mounts a complete ini file into PHP's <c>conf.d</c> scan directory (loaded after the base
    /// php.ini, so its values override). Relative paths resolve against the AppHost directory.
    /// </summary>
    public static IResourceBuilder<PhpResource> WithPhpIniFile(
        this IResourceBuilder<PhpResource> builder, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path, builder.ApplicationBuilder.AppHostDirectory);
        // "zzz-" prefix: conf.d files load alphabetically; this keeps the mounted file last so it wins.
        return builder.WithBindMount(fullPath, $"/usr/local/etc/php/conf.d/zzz-{Path.GetFileName(fullPath)}", isReadOnly: true);
    }
}
