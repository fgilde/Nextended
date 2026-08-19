using System.Globalization;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// Fluent API for running WebDataStudio inside your Aspire stack. Either add it yourself with
/// <see cref="AddWebDataStudio"/> and attach databases with <c>WithReference</c>, or start from a
/// database resource and call
/// <see cref="WebDataStudioAttachExtensions.WithWebDataStudio{T}(IResourceBuilder{T}, Action{IResourceBuilder{WebDataStudioResource}}?, string?, string?, WebDataStudioEngine?)"/>.
/// </summary>
public static class WebDataStudioBuilderExtensions
{
    /// <summary>
    /// Adds WebDataStudio as a container: the studio over HTTP, with its own named volume for the
    /// connections, query history and layouts a user creates in the UI.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">Resource name (default <c>webdatastudio</c>). Two calls with the same name are the same studio — see <c>WithWebDataStudio</c>.</param>
    /// <param name="port">Optional fixed host port (default: auto-assigned).</param>
    /// <param name="image">Override the container image (default <c>ghcr.io/fgilde/webdatastudio</c>).</param>
    /// <param name="tag">Override the image tag (default <c>latest</c>).</param>
    public static IResourceBuilder<WebDataStudioResource> AddWebDataStudio(
        this IDistributedApplicationBuilder builder,
        string name = WebDataStudioResource.DefaultResourceName,
        int? port = null,
        string? image = null,
        string? tag = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // Precomputed: an interpolated string would bind to the ReferenceExpression overload of
        // WithEnvironment, which cannot format an int.
        var urls = "http://+:" + WebDataStudioResource.DefaultTargetPort.ToString(CultureInfo.InvariantCulture);

        return builder.AddResource(new WebDataStudioResource(name))
            .WithImage(image ?? WebDataStudioResource.DefaultImage, tag ?? WebDataStudioResource.DefaultTag)
            // The default tag is a rolling "latest"; re-pull so a stale local image cannot pin an old build.
            .WithImagePullPolicy(ImagePullPolicy.Always)
            .WithHttpEndpoint(port: port, targetPort: WebDataStudioResource.DefaultTargetPort,
                name: WebDataStudioResource.HttpEndpointName)
            .WithEnvironment("ASPNETCORE_URLS", urls)
            .WithEnvironment("DB_PATH", "/data/webdatastudio.db")
            // Per-instance volume: two studios in one stack must not share saved connections.
            .WithVolume($"webdatastudio-data-{name}", "/data")
            .WithHttpHealthCheck("/api/auth/me", endpointName: WebDataStudioResource.HttpEndpointName);
    }

    /// <summary>
    /// Guards the studio with a login. Without one there is no login screen at all, which is the
    /// sensible default while the studio only listens on your machine.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithLogin(
        this IResourceBuilder<WebDataStudioResource> builder, string username, string password)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        builder.Resource.Username = username;
        return builder
            .WithEnvironment("WDS_USER", username)
            .WithEnvironment("WDS_PASSWORD", password);
    }

    /// <summary>
    /// Guards the studio with a login whose password comes from an Aspire parameter, so it stays
    /// out of the manifest and out of source control.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithLogin(
        this IResourceBuilder<WebDataStudioResource> builder, string username,
        IResourceBuilder<ParameterResource> password)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        builder.Resource.Username = username;
        return builder
            .WithEnvironment("WDS_USER", username)
            .WithEnvironment("WDS_PASSWORD", password);
    }

    /// <summary>Guards the studio with a login where both halves come from Aspire parameters.</summary>
    public static IResourceBuilder<WebDataStudioResource> WithLogin(
        this IResourceBuilder<WebDataStudioResource> builder,
        IResourceBuilder<ParameterResource> username,
        IResourceBuilder<ParameterResource> password)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(username);
        ArgumentNullException.ThrowIfNull(password);

        builder.Resource.Username = username.Resource.Name;
        return builder
            .WithEnvironment("WDS_USER", username)
            .WithEnvironment("WDS_PASSWORD", password);
    }

    /// <summary>
    /// Makes every connection read-only, whatever each one says for itself. The studio enforces
    /// this in the driver, not only by hiding buttons.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithReadOnly(
        this IResourceBuilder<WebDataStudioResource> builder, bool readOnly = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithEnvironment("WDS_READONLY", readOnly ? "true" : "false");
    }

    /// <summary>Sets the default statement timeout (the studio's own default is five minutes).</summary>
    public static IResourceBuilder<WebDataStudioResource> WithQueryTimeout(
        this IResourceBuilder<WebDataStudioResource> builder, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "the timeout has to be positive");

        return builder.WithEnvironment("WDS_QUERY_TIMEOUT_SECONDS",
            ((int)timeout.TotalSeconds).ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>Sets how many rows a result fetches before it stops (the studio's default is 1000).</summary>
    public static IResourceBuilder<WebDataStudioResource> WithMaxRows(
        this IResourceBuilder<WebDataStudioResource> builder, int maxRows)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRows);

        return builder.WithEnvironment("WDS_MAX_ROWS", maxRows.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Caps how many sessions one connection may hold at once and how long an unused one stays
    /// open. Both are optional; the studio defaults to eight sessions and five minutes.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithSessionLimits(
        this IResourceBuilder<WebDataStudioResource> builder, int? maxSessions = null,
        TimeSpan? idleTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (maxSessions is { } sessions)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sessions);
            builder.WithEnvironment("WDS_MAX_SESSIONS", sessions.ToString(CultureInfo.InvariantCulture));
        }

        if (idleTimeout is { } idle)
        {
            if (idle <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(idleTimeout), idle, "the timeout has to be positive");

            builder.WithEnvironment("WDS_IDLE_TIMEOUT_SECONDS",
                ((int)idle.TotalSeconds).ToString(CultureInfo.InvariantCulture));
        }

        return builder;
    }

    /// <summary>
    /// Sets the key the studio encrypts stored connection secrets with (base64, 32 bytes). Without
    /// one it generates a key into its data volume — fine locally, but a fixed key is what keeps
    /// stored connections readable when the volume is recreated.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithSecretKey(
        this IResourceBuilder<WebDataStudioResource> builder, string base64Key)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(base64Key);

        return builder.WithEnvironment("WDS_SECRET_KEY", base64Key);
    }

    /// <summary>Sets the encryption key from an Aspire parameter, keeping it out of the manifest.</summary>
    public static IResourceBuilder<WebDataStudioResource> WithSecretKey(
        this IResourceBuilder<WebDataStudioResource> builder, IResourceBuilder<ParameterResource> key)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(key);

        return builder.WithEnvironment("WDS_SECRET_KEY", key);
    }

    /// <summary>
    /// Replaces the default data volume with one of your own name, or with an anonymous volume
    /// when no name is given.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithDataVolume(
        this IResourceBuilder<WebDataStudioResource> builder, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithoutDefaultDataVolume().WithVolume(name!, "/data");
    }

    /// <summary>
    /// Keeps the studio's data in a folder on your machine instead of a volume — handy when you
    /// want to look at, back up or delete it by hand.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithDataBindMount(
        this IResourceBuilder<WebDataStudioResource> builder, string path)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return builder.WithoutDefaultDataVolume().WithBindMount(path, "/data");
    }

    /// <summary>
    /// Attaches a resource that has a connection string, so the studio comes up with it already
    /// configured. This is the <c>WithReference</c> you want on a studio: Aspire's own one would
    /// write a <c>ConnectionStrings__*</c> variable, which the studio does not read.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="source">Any resource with a connection string — a database, a cache, a server.</param>
    /// <param name="connectionName">Label in the studio (default: the resource's name, upper-cased).</param>
    /// <param name="engine">Which engine it is (default: worked out from the resource type).</param>
    /// <param name="readOnly">Opens this connection read-only.</param>
    /// <param name="group">Groups the connection in the explorer.</param>
    /// <param name="color">Tints the connection in the explorer, e.g. <c>#e03131</c> for production.</param>
    public static IResourceBuilder<WebDataStudioResource> WithReference<T>(
        this IResourceBuilder<WebDataStudioResource> builder,
        IResourceBuilder<T> source,
        string? connectionName = null,
        WebDataStudioEngine? engine = null,
        bool readOnly = false,
        string? group = null,
        string? color = null)
        where T : IResourceWithConnectionString
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        var name = WebDataStudioNaming.ToVariableSuffix(connectionName ?? source.Resource.Name);
        var resolved = engine ?? WebDataStudioEngineDetection.Detect(source.Resource);

        if (builder.Resource.HasConnection(name)) return builder;
        builder.Resource.TrackConnection(name);

        builder.WithEnvironment($"WDS_CONN_{name}", source.Resource.ConnectionStringExpression);
        return Configure(builder, name, resolved, readOnly, group, color);
    }

    /// <summary>
    /// Attaches a connection the app host already knows as a plain string — a database outside the
    /// stack, for instance. The engine is required here because there is no resource to read it
    /// from.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithConnection(
        this IResourceBuilder<WebDataStudioResource> builder,
        string connectionName,
        string connectionString,
        WebDataStudioEngine engine,
        bool readOnly = false,
        string? group = null,
        string? color = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var name = WebDataStudioNaming.ToVariableSuffix(connectionName);

        if (builder.Resource.HasConnection(name)) return builder;
        builder.Resource.TrackConnection(name);

        builder.WithEnvironment($"WDS_CONN_{name}", connectionString);
        return Configure(builder, name, engine, readOnly, group, color);
    }

    /// <summary>
    /// Attaches a connection whose string is only known once the stack runs — an endpoint of
    /// another resource, say. Build it with <see cref="ReferenceExpression.Create"/>.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithConnection(
        this IResourceBuilder<WebDataStudioResource> builder,
        string connectionName,
        ReferenceExpression connectionString,
        WebDataStudioEngine engine,
        bool readOnly = false,
        string? group = null,
        string? color = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        var name = WebDataStudioNaming.ToVariableSuffix(connectionName);

        if (builder.Resource.HasConnection(name)) return builder;
        builder.Resource.TrackConnection(name);

        builder.WithEnvironment($"WDS_CONN_{name}", connectionString);
        return Configure(builder, name, engine, readOnly, group, color);
    }

    private static IResourceBuilder<WebDataStudioResource> Configure(
        IResourceBuilder<WebDataStudioResource> builder, string name, WebDataStudioEngine? engine,
        bool readOnly, string? group, string? color)
    {
        // No engine means the studio guesses from the connection string. That works for the
        // common providers and skips the connection when it cannot tell — better than attaching
        // it to the wrong driver.
        if (engine is { } known) builder.WithEnvironment($"WDS_CONN_{name}_ENGINE", known.ToEngineId());
        if (readOnly) builder.WithEnvironment($"WDS_CONN_{name}_READONLY", "true");
        if (!string.IsNullOrWhiteSpace(group)) builder.WithEnvironment($"WDS_CONN_{name}_GROUP", group);
        if (!string.IsNullOrWhiteSpace(color)) builder.WithEnvironment($"WDS_CONN_{name}_COLOR", color);

        return builder;
    }

    /// <summary>
    /// Drops the volume <see cref="AddWebDataStudio"/> mounted, so a caller can put their own
    /// storage at /data without ending up with two mounts on the same path.
    /// </summary>
    private static IResourceBuilder<WebDataStudioResource> WithoutDefaultDataVolume(
        this IResourceBuilder<WebDataStudioResource> builder)
    {
        foreach (var mount in builder.Resource.Annotations.OfType<ContainerMountAnnotation>()
                     .Where(m => m.Target == "/data").ToList())
            builder.Resource.Annotations.Remove(mount);

        return builder;
    }
}
