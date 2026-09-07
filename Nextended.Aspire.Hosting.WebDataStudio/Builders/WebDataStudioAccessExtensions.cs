using System.Globalization;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>Where a connection somebody makes in the studio goes.</summary>
public enum ConnectionScope
{
    /// <summary>
    /// The studio's connection store: written down, kept across restarts, visible to everybody who
    /// may see it. What every studio did before this setting existed.
    /// </summary>
    Stored,

    /// <summary>
    /// The browser that made it, and nobody else. Nothing on disk, gone when the container restarts
    /// or the session expires — a studio handed out as a viewer wants this one.
    /// </summary>
    Session,
}

/// <summary>
/// What people may do in the studio: where a connection they make goes, and which ways in are open.
/// </summary>
/// <remarks>
/// Two questions rather than one pile of switches, because they are independent. A studio can keep
/// the form open and hold what it makes for one browser, or close the form and keep the links.
/// <para>
/// Every default is what the studio does without any of this, so a stack that says nothing notices
/// nothing. The negative names are for the gates that are on by default, the way
/// <c>WithoutAssistantTools</c> reads.
/// </para>
/// </remarks>
public static class WebDataStudioAccessExtensions
{
    /// <summary>
    /// Says where a connection somebody makes in the studio goes.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="scope">
    /// <see cref="ConnectionScope.Stored"/> writes it down for everybody, which is the default;
    /// <see cref="ConnectionScope.Session"/> holds it for the browser that made it and writes
    /// nothing.
    /// </param>
    /// <remarks>
    /// In <see cref="ConnectionScope.Session"/> the form, an upload, an import and a <c>?u=</c> link
    /// all land in the same place: that browser's own list. The studio says so on its connections
    /// page and offers a button that throws the lot away.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithConnectionScope(
        this IResourceBuilder<WebDataStudioResource> builder, ConnectionScope scope)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEnvironment("WDS_CONNECTION_SCOPE",
            scope == ConnectionScope.Session ? "session" : "stored");
    }

    /// <summary>
    /// Closes the form: nobody using the studio may type, paste, import or test a connection string.
    /// </summary>
    /// <remarks>
    /// The one to reach for when the connections come from the app host and should stay that way. It
    /// covers testing a connection too — that opens whatever it is given and keeps nothing, which
    /// makes it the cheapest door of the four.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithoutAddingConnections(
        this IResourceBuilder<WebDataStudioResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEnvironment("WDS_ALLOW_ADD_CONNECTION", "false");
    }

    /// <summary>Closes the upload: no database file may be sent from a browser.</summary>
    public static IResourceBuilder<WebDataStudioResource> WithoutFileUpload(
        this IResourceBuilder<WebDataStudioResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEnvironment("WDS_ALLOW_FILE_UPLOAD", "false");
    }

    /// <summary>
    /// Closes the server browser: the folders of <see cref="WebDataStudioUrlExtensions.WithDatabaseFiles"/>
    /// stay out of sight.
    /// </summary>
    /// <remarks>
    /// Worth closing on a studio strangers can reach even when nothing secret is mounted: a listing
    /// of a container's folders is a description of the deployment.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithoutFileBrowse(
        this IResourceBuilder<WebDataStudioResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEnvironment("WDS_ALLOW_FILE_BROWSE", "false");
    }

    /// <summary>
    /// The hosts the studio may open a connection to at all, whatever the connection string says.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="hosts">
    /// Host names; <c>*.example.com</c> matches one level of subdomain. Nothing means no restriction.
    /// </param>
    /// <remarks>
    /// Checked wherever a connection came from: the form, a test, a link, the store, the app host's
    /// own connections. A studio anybody may type a connection string into is otherwise an outbound
    /// connector from wherever it runs — a visitor can reach the addresses only the container can.
    /// <para>
    /// A connection whose host is outside the list is not offered at all, which includes the ones
    /// this app host wrote down: check the list against your own resources before deploying, or the
    /// studio comes up with fewer connections than the stack describes.
    /// </para>
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithConnectHosts(
        this IResourceBuilder<WebDataStudioResource> builder, params string[] hosts)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var allowed = (hosts ?? []).Where(host => !string.IsNullOrWhiteSpace(host))
            .Select(host => host.Trim())
            .ToArray();

        if (allowed.Length == 0)
            throw new ArgumentException(
                "WithConnectHosts with no hosts would refuse every connection this studio has; name "
                + "the hosts it may reach, or leave the call out", nameof(hosts));

        return builder.WithEnvironment("WDS_CONNECT_HOSTS", string.Join(',', allowed));
    }

    /// <summary>How long a browser's connections live, and how many it may hold.</summary>
    /// <param name="builder">The studio.</param>
    /// <param name="minutes">
    /// How long a session may go quiet before its connections and the files behind them are dropped.
    /// <c>0</c> never expires, which is right for a studio one person runs and wrong for one anybody
    /// can reach.
    /// </param>
    /// <param name="maxConnections">How many connections one browser may hold at once.</param>
    public static IResourceBuilder<WebDataStudioResource> WithSessionLifetime(
        this IResourceBuilder<WebDataStudioResource> builder, int minutes = 240,
        int maxConnections = 25)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfNegative(minutes);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConnections, 1);

        return builder
            .WithEnvironment("WDS_SESSION_TTL_MINUTES", minutes.ToString(CultureInfo.InvariantCulture))
            .WithEnvironment("WDS_SESSION_MAX_CONNECTIONS",
                maxConnections.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>The largest database file somebody may send to this studio.</summary>
    public static IResourceBuilder<WebDataStudioResource> WithUploadLimit(
        this IResourceBuilder<WebDataStudioResource> builder, int megabytes)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfLessThan(megabytes, 1);

        return builder.WithEnvironment("WDS_UPLOAD_MAX_MB",
            megabytes.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// A studio anybody may use: no connections of its own, everything a visitor brings belongs to
    /// their browser, and nothing outlives them.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="connectionStrings">
    /// Whether a visitor may bring a whole connection string — typed into the form, or in a
    /// <c>?u=</c> link. Off by default: it is a password in browser history, and it points the studio
    /// at whatever address it names.
    /// </param>
    /// <param name="upload">Whether a database file may be sent from a browser.</param>
    /// <param name="hosts">
    /// The hosts the studio may reach. Required when <paramref name="connectionStrings"/> is on,
    /// because that is the switch that lets a stranger choose the address.
    /// </param>
    /// <param name="minutes">How long a session may go quiet. Two hours by default.</param>
    /// <remarks>
    /// The whole set in one call, because a deployment that wants this wants all of it: session
    /// scope, no server browser, <c>?u=</c> for files, read-only, a lifetime, a ceiling and an upload
    /// limit. Add <see cref="WebDataStudioUrlExtensions.WithDatabaseFiles"/> for a folder of sample
    /// databases if visitors should find something to look at.
    /// <para>
    /// Two things this cannot do for you. There is no rate limiting, so put a public studio behind a
    /// proxy that has some. And a host list is a list: run it somewhere with restricted egress as
    /// well, because a network is a boundary and a setting is not.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.AddWebDataStudio("viewer")
    ///     .AsPublicViewer(connectionStrings: true, hosts: ["db.example", "*.example.com"]);
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">
    /// Connection strings are allowed without a host list — or this studio also has accounts or
    /// connections of its own, which a viewer cannot have.
    /// </exception>
    public static IResourceBuilder<WebDataStudioResource> AsPublicViewer(
        this IResourceBuilder<WebDataStudioResource> builder, bool connectionStrings = false,
        bool upload = true, IEnumerable<string>? hosts = null, int minutes = 120)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var allowed = (hosts ?? []).Where(host => !string.IsNullOrWhiteSpace(host))
            .Select(host => host.Trim())
            .ToArray();

        if (connectionStrings && allowed.Length == 0)
            throw new ArgumentException(
                "a public studio that takes connection strings needs the hosts it may connect to: "
                + "without them a visitor can point it at any address this container can reach",
                nameof(hosts));

        // A viewer with accounts and shared connections is a contradiction, and the app host is
        // where to find that out rather than the running container. The resource keeps both facts
        // itself, so this reads them rather than guessing from environment variables.
        if (builder.Resource.Username is { Length: > 0 } || builder.Resource.Accounts.Count > 0)
            throw new ArgumentException(
                "AsPublicViewer is for a studio anybody may use, and this one has a login "
                + "(WithLogin/WithUser). Say what you mean with WithConnectionScope and the "
                + "WithoutX methods instead", nameof(builder));

        if (builder.Resource.ConnectionNames.Count > 0)
            throw new ArgumentException(
                "AsPublicViewer is for a studio with no connections of its own, and this one has "
                + $"{builder.Resource.ConnectionNames.Count} (WithConnection/WithReference). Use "
                + "WithConnectionScope and the WithoutX methods to say exactly what you mean "
                + "instead", nameof(builder));

        var kinds = connectionStrings ? "file,connection-string" : "file";

        builder
            .WithConnectionScope(ConnectionScope.Session)
            .WithoutFileBrowse()
            .WithReadOnly()
            .WithSessionLifetime(minutes)
            .WithUploadLimit(50)
            .WithEnvironment("WDS_OPEN_FROM_URL", kinds);

        if (!upload) builder.WithoutFileUpload();
        if (allowed.Length > 0) builder.WithConnectHosts(allowed);

        return builder;
    }
}
