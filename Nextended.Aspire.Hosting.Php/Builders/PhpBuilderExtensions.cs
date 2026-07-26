using System.Globalization;
using System.Reflection;
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
        var resource = new PhpResource(name) { SourcePath = fullPath };
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
            // Args via callback so WithPhpIni/WithPhpExtensions calls made after AddPhp still
            // end up in the command line.
            .WithArgs(ctx =>
            {
                var phpArgs = new List<string>();
                foreach (var (key, value) in resource.IniSettings)
                {
                    phpArgs.Add("-d");
                    phpArgs.Add($"{key}={value}");
                }
                phpArgs.Add("-S");
                phpArgs.Add($"0.0.0.0:{PhpResource.DefaultTargetPort}");
                phpArgs.Add("-t");
                phpArgs.Add(PhpResource.AppDirectory);
                if (resource.RouterScript is { } router)
                    phpArgs.Add($"{PhpResource.AppDirectory}/{router}");

                if (resource.Extensions.Count == 0)
                {
                    ctx.Args.Add("php");
                    foreach (var arg in phpArgs)
                        ctx.Args.Add(arg);
                    return;
                }

                // ponytail: extensions compile on every container start (~20-40s); bake a custom
                // image and pass it via AddPhp(image:) when that gets annoying.
                var install = "docker-php-ext-install -j\"$(nproc)\" " + string.Join(' ', resource.Extensions.Select(ShQuote));
                var run = "exec php " + string.Join(' ', phpArgs.Select(ShQuote));
                ctx.Args.Add("sh");
                ctx.Args.Add("-c");
                ctx.Args.Add($"{install} && {run}");
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
    /// Typed variant of <see cref="WithPhpIni(IResourceBuilder{PhpResource}, string, string)"/>:
    /// sets php.ini directives via a configuration object, e.g.
    /// <c>WithPhpIniConfiguration(a =&gt; a.DisplayErrors = true)</c>. Only assigned (non-null)
    /// properties are applied. Use your own <typeparamref name="T"/> subclass (plus optional
    /// <see cref="PhpIniKeyAttribute"/>) for directives <see cref="PhpIniConfiguration"/> doesn't cover.
    /// </summary>
    public static IResourceBuilder<PhpResource> WithPhpIniConfiguration<T>(
        this IResourceBuilder<PhpResource> builder, Action<T> configure) where T : PhpIniConfiguration, new()
    {
        ArgumentNullException.ThrowIfNull(configure);
        var config = new T();
        configure(config);
        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetValue(config) is not { } value) continue;
            var key = prop.GetCustomAttribute<PhpIniKeyAttribute>()?.Key ?? ToSnakeCase(prop.Name);
            builder.WithPhpIni(key, value switch
            {
                bool b => b ? "1" : "0",
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty,
            });
        }
        return builder;
    }

    /// <summary>Convenience overload of <see cref="WithPhpIniConfiguration{T}"/> using the built-in <see cref="PhpIniConfiguration"/>.</summary>
    public static IResourceBuilder<PhpResource> WithPhpIniConfiguration(
        this IResourceBuilder<PhpResource> builder, Action<PhpIniConfiguration> configure)
        => builder.WithPhpIniConfiguration<PhpIniConfiguration>(configure);

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

    /// <summary>
    /// Installs PHP extensions at container start via <c>docker-php-ext-install</c> (e.g.
    /// <c>WithPhpExtensions("mysqli", "pdo_mysql")</c>). Works for extensions that compile without
    /// extra system libraries (mysqli, pdo_mysql, pcntl, bcmath, sockets, exif, …); heavier ones
    /// like gd/intl/zip need a custom image (<c>AddPhp(..., image: ...)</c>). Compilation adds
    /// roughly 20–40s to every container start.
    /// </summary>
    public static IResourceBuilder<PhpResource> WithPhpExtensions(
        this IResourceBuilder<PhpResource> builder, params string[] extensions)
    {
        ArgumentNullException.ThrowIfNull(extensions);
        foreach (var extension in extensions)
            if (!string.IsNullOrWhiteSpace(extension) && !builder.Resource.Extensions.Contains(extension))
                builder.Resource.Extensions.Add(extension);
        return builder;
    }

    /// <summary>
    /// Sets the number of parallel request workers of PHP's built-in server
    /// (<c>PHP_CLI_SERVER_WORKERS</c>; default 8).
    /// </summary>
    public static IResourceBuilder<PhpResource> WithWorkers(
        this IResourceBuilder<PhpResource> builder, int workers)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(workers, 1);
        // Later env callbacks win, so this overrides the default set in AddPhp.
        return builder.WithEnvironment("PHP_CLI_SERVER_WORKERS", workers.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Runs <c>composer install</c> against the mounted folder before PHP starts, using the official
    /// <c>composer</c> image on the same bind mount (vendor/ appears on the host, like npm install).
    /// Requires folder mode with a <c>composer.json</c> in the folder. The composer cache is kept on
    /// a named volume so subsequent runs are fast.
    /// </summary>
    /// <param name="builder">The PHP resource builder.</param>
    /// <param name="image">Override the composer image (default <c>composer</c>).</param>
    /// <param name="tag">Override the composer image tag (default <c>2</c>).</param>
    public static IResourceBuilder<PhpResource> WithComposer(
        this IResourceBuilder<PhpResource> builder, string? image = null, string? tag = null)
    {
        var resource = builder.Resource;
        if (resource.RouterScript is not null || resource.SourcePath is not { } source)
            throw new InvalidOperationException(
                "WithComposer requires folder mode — call AddPhp with a folder containing composer.json, not a single .php file.");

        var composer = builder.ApplicationBuilder
            .AddContainer($"{resource.Name}-composer", image ?? "composer", tag ?? "2")
            .WithBindMount(source, "/app")
            .WithVolume($"{resource.Name}-composer-cache", "/tmp/cache")
            .WithArgs("install", "--no-interaction")
            .WithParentRelationship(resource);

        return builder.WaitForCompletion(composer);
    }

    private static string ToSnakeCase(string name)
        => string.Concat(name.Select((c, i) => char.IsUpper(c) ? (i > 0 ? "_" : "") + char.ToLowerInvariant(c) : c.ToString()));

    private static string ShQuote(string s) => "'" + s.Replace("'", "'\\''") + "'";
}
