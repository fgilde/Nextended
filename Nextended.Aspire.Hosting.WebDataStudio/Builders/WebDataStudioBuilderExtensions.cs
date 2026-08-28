using System.Globalization;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.WebDataStudio.Resources;
using Nextended.Core.Helper;

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

        var resource = new WebDataStudioResource(name) { Title = name };

        var studio = builder.AddResource(resource)
            .WithImage(image ?? WebDataStudioResource.DefaultImage, tag ?? WebDataStudioResource.DefaultTag)
            // The default tag is a rolling "latest"; re-pull so a stale local image cannot pin an old build.
            .WithImagePullPolicy(ImagePullPolicy.Always)
            .WithHttpEndpoint(port: port, targetPort: WebDataStudioResource.DefaultTargetPort,
                name: WebDataStudioResource.HttpEndpointName)
            .WithEnvironment("ASPNETCORE_URLS", urls)
            .WithEnvironment("DB_PATH", "/data/webdatastudio.db")
            // The resource name is what the dashboard calls this studio; showing the same name in
            // the studio itself is what tells three of them apart.
            .WithEnvironment("WDS_TITLE", name)
            .WithHttpHealthCheck("/api/auth/me", endpointName: WebDataStudioResource.HttpEndpointName);

        // A named volume locally, nothing when published. Aspire turns a volume into an Azure
        // Files share on Container Apps, and the studio keeps its connections, history and
        // layouts in SQLite — which on an SMB share either crawls or blocks outright, taking
        // every request that touches it with it. A deployed studio therefore starts with an
        // empty, container-local /data; connections attached here come from the environment on
        // every start anyway. Ask for persistence explicitly with WithDataVolume() if the share
        // is known to behave.
        if (builder.ExecutionContext.IsRunMode)
            studio.WithVolume($"webdatastudio-data-{name}", "/data");

        WarnAboutAnonymousPublish(builder, resource);

        return studio;
    }

    /// A studio reachable from the internet without a login would hand every visitor full access
    /// to the databases behind it. Nothing here blocks the deploy — that is the app host's call —
    /// but it must not happen quietly.
    private static void WarnAboutAnonymousPublish(
        IDistributedApplicationBuilder builder, WebDataStudioResource resource)
    {
        if (!builder.ExecutionContext.IsPublishMode) return;

        builder.Eventing.Subscribe<BeforeStartEvent>((_, _) =>
        {
            var hasLogin = resource.Username is not null;
            var isExternal = resource.Annotations.OfType<EndpointAnnotation>()
                .Any(endpoint => endpoint.IsExternal);

            if (!hasLogin && isExternal)
                Console.Error.WriteLine(
                    $"WebDataStudio '{resource.Name}' is published with an external endpoint and no " +
                    "login. Anyone who finds the URL gets full access to every attached database — " +
                    "add WithLogin(user, password) (and WithReadOnly() if that is enough).");

            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Guards the studio with a login. Without one there is no login screen at all, which is the
    /// sensible default while the studio only listens on your machine.
    /// </summary>
    /// <remarks>
    /// Chaining this adds accounts rather than replacing them: two calls mean two people can sign
    /// in. The first account is an <c>admin</c>; use <see cref="WithUser(IResourceBuilder{WebDataStudioResource}, string, string, string, string[])"/>
    /// for anything else. Calling it twice with the same name replaces that one account.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithLogin(
        this IResourceBuilder<WebDataStudioResource> builder, string username, string password)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return builder.WithAccount(new StudioAccount(username, StudioRoles.Admin, []), password);
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

        return builder.WithAccount(
            new StudioAccount(username, StudioRoles.Admin, []), password.Resource);
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

        // A parameterised name cannot be part of a WDS_USERS entry — the whole entry is one string,
        // and the studio's own single-account variables are the honest way to carry it.
        builder.Resource.Username = username.Resource.Name;
        return builder
            .WithEnvironment("WDS_USER", username)
            .WithEnvironment("WDS_PASSWORD", password);
    }

    /// <summary>
    /// Adds one account with a role, and optionally the connections it may see. Empty means all of
    /// them; a connection an account may not see does not exist for it at all.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="username">The login name.</param>
    /// <param name="password">The password, in clear text. Use the parameter overload to keep it out of source control.</param>
    /// <param name="role">
    /// <see cref="StudioRoles.Admin"/>, <see cref="StudioRoles.Editor"/> or
    /// <see cref="StudioRoles.Viewer"/>. Defaults to <c>viewer</c>, because that is the role that
    /// cannot do damage if the call was a guess.
    /// </param>
    /// <param name="connections">Names of the connections this account may see. None means all.</param>
    public static IResourceBuilder<WebDataStudioResource> WithUser(
        this IResourceBuilder<WebDataStudioResource> builder, string username, string password,
        string role = StudioRoles.Viewer, params string[] connections)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return builder.WithAccount(
            new StudioAccount(username, Role(role), connections ?? []), password);
    }

    /// <summary>Adds one account with a role and a password from an Aspire parameter.</summary>
    public static IResourceBuilder<WebDataStudioResource> WithUser(
        this IResourceBuilder<WebDataStudioResource> builder, string username,
        IResourceBuilder<ParameterResource> password,
        string role = StudioRoles.Viewer, params string[] connections)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        return builder.WithAccount(
            new StudioAccount(username, Role(role), connections ?? []), password.Resource);
    }

    private static string Role(string role)
    {
        var normalised = role?.Trim().ToLowerInvariant();

        // A typo must fail loudly here rather than quietly become a viewer in the container.
        if (!StudioRoles.All.Contains(normalised))
            throw new ArgumentOutOfRangeException(nameof(role), role,
                $"role has to be one of {string.Join(", ", StudioRoles.All)}");

        return normalised!;
    }

    /// <summary>
    /// Records an account and makes sure the environment is written once, from the whole list.
    /// The variables cannot be set per call: a second account has to turn <c>WDS_USER</c> into a
    /// <c>WDS_USERS</c> entry, and an environment variable already appended cannot be taken back.
    /// </summary>
    private static IResourceBuilder<WebDataStudioResource> WithAccount(
        this IResourceBuilder<WebDataStudioResource> builder, StudioAccount account, object secret)
    {
        builder.Resource.AddAccount(account, secret);

        if (!builder.Resource.ClaimEnvironmentHook()) return builder;

        return builder.WithEnvironment(context =>
        {
            var accounts = builder.Resource.AccountsWithSecrets;
            if (accounts.Count == 0) return;

            // One plain admin account is what WDS_USER/WDS_PASSWORD were made for, and what every
            // existing app host already writes.
            if (accounts.Count == 1
                && accounts[0].Account.Role == StudioRoles.Admin
                && accounts[0].Account.Connections.Count == 0)
            {
                context.EnvironmentVariables["WDS_USER"] = accounts[0].Account.Name;
                context.EnvironmentVariables["WDS_PASSWORD"] = Secret(accounts[0].Secret);
                return;
            }

            // Several accounts, or one with a role: name:role:secret[:conn,conn], separated by ';'.
            // Plain passwords compose into a plain string; a parameter has to stay a reference, so
            // that it is resolved at start and never lands in the manifest.
            var parameterised = accounts.Any(entry => entry.Secret is ParameterResource);

            if (!parameterised)
            {
                context.EnvironmentVariables["WDS_USERS"] = string.Join(";",
                    accounts.Select(entry => Entry(entry.Account, (string)entry.Secret)));
                return;
            }

            var users = new ReferenceExpressionBuilder();
            var first = true;

            foreach (var (studioAccount, secretValue) in accounts)
            {
                if (!first) users.AppendLiteral(";");
                first = false;

                users.AppendLiteral($"{studioAccount.Name}:{studioAccount.Role}:");

                if (secretValue is ParameterResource parameter) users.AppendFormatted(parameter);
                else users.AppendLiteral((string)secretValue);

                if (studioAccount.Connections.Count > 0)
                    users.AppendLiteral($":{string.Join(",", studioAccount.Connections)}");
            }

            context.EnvironmentVariables["WDS_USERS"] = users.Build();
        });
    }

    /// One `name:role:secret[:conn,conn]` entry.
    private static string Entry(StudioAccount account, string secret) =>
        $"{account.Name}:{account.Role}:{secret}" +
        (account.Connections.Count > 0 ? $":{string.Join(",", account.Connections)}" : "");

    private static object Secret(object secret) =>
        secret is ParameterResource parameter ? parameter : (string)secret;

    /// <summary>
    /// Sets the name the studio shows in its header and browser tab. Without this it uses the
    /// resource name; pass <c>null</c> for a studio with no name at all.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithTitle(
        this IResourceBuilder<WebDataStudioResource> builder, string? title)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Resource.Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();

        // An empty value is what the studio reads as "no name", so it is set either way — the
        // default from AddWebDataStudio has to be overridable.
        return builder.WithEnvironment("WDS_TITLE", builder.Resource.Title ?? "");
    }

    /// <summary>
    /// Sets the theme the studio comes up in — one of the studio's own, by name.
    /// </summary>
    /// <remarks>
    /// Prefer the <see cref="WithTheme(IResourceBuilder{WebDataStudioResource}, WebDataStudioTheme)"/>
    /// overload; this one exists for a theme a newer studio has and this package does not know yet.
    /// An id the studio does not recognise is ignored by it, with a line in its log — a stack does
    /// not fail to start over a colour scheme.
    /// </remarks>
    /// <param name="builder">The studio resource.</param>
    /// <param name="theme">A theme id, e.g. <c>ocean</c>. Null or empty leaves the studio's default.</param>
    public static IResourceBuilder<WebDataStudioResource> WithTheme(
        this IResourceBuilder<WebDataStudioResource> builder, string? theme)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Resource.Theme = string.IsNullOrWhiteSpace(theme) ? null : theme.Trim();

        // Empty is what the studio reads as "no preference", so it is written either way: a second
        // call has to be able to take the first one back.
        return builder.WithEnvironment("WDS_THEME", builder.Resource.Theme ?? "");
    }

    /// <summary>
    /// Sets the theme the studio comes up in: <c>WithTheme(WebDataStudioTheme.Ocean)</c>.
    /// </summary>
    /// <remarks>
    /// The initial theme only. Whoever opens the studio may pick another, and that choice is theirs
    /// from then on.
    /// </remarks>
    /// <param name="builder">The studio resource.</param>
    /// <param name="theme">One of the studio's themes.</param>
    public static IResourceBuilder<WebDataStudioResource> WithTheme(
        this IResourceBuilder<WebDataStudioResource> builder, WebDataStudioTheme theme)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // The enum's Description is the studio's id, read by the helper that already exists for it.
        return builder.WithTheme(Enum<WebDataStudioTheme>.DescriptionFor(theme));
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
    /// How long a transaction a query tab holds open may sit untouched before the studio rolls it
    /// back. Fifteen minutes by default.
    /// </summary>
    /// <remarks>
    /// A held transaction keeps a session and whatever locks its statements took. A browser that is
    /// closed outright would otherwise leave both behind, so the studio ends one nobody came back
    /// to — shorter on a busy production database, longer where people think between statements.
    /// </remarks>
    /// <param name="builder">The studio resource.</param>
    /// <param name="idle">How long an untouched transaction lives. Has to be positive.</param>
    public static IResourceBuilder<WebDataStudioResource> WithTransactionTimeout(
        this IResourceBuilder<WebDataStudioResource> builder, TimeSpan idle)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (idle <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(idle), idle, "the timeout has to be positive");

        return builder.WithEnvironment("WDS_TRANSACTION_IDLE_SECONDS",
            ((int)idle.TotalSeconds).ToString(CultureInfo.InvariantCulture));
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
