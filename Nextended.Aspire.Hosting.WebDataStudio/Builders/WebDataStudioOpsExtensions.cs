using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// The operational side of the studio: what it watches, and who it tells. The studio already runs
/// the analysis behind its health report — these calls arrange for somebody to hear about it.
/// </summary>
public static class WebDataStudioOpsExtensions
{
    /// <summary>
    /// Posts new health findings — missing indexes, tables without a primary key, bloat — to a
    /// webhook. Slack, Mattermost, Discord and Teams all read the <c>text</c> field it sends; the
    /// structured findings ride along for anything that wants more.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="webhook">The URL to post to.</param>
    /// <param name="interval">How often to look. Default one hour.</param>
    /// <param name="minSeverity">
    /// <c>info</c>, <c>warning</c> (the default) or <c>critical</c>: the floor for what is worth a
    /// message.
    /// </param>
    /// <param name="connections">
    /// Connection names to watch. None means all of them.
    /// </param>
    /// <remarks>
    /// Only findings that are new since the last sweep are sent: an alert that repeats every hour
    /// is one people filter into a folder they never open.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithAlertWebhook(
        this IResourceBuilder<WebDataStudioResource> builder, string webhook,
        TimeSpan? interval = null, string? minSeverity = null, params string[] connections)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(webhook);

        builder.WithEnvironment("WDS_ALERT_WEBHOOK", webhook);

        return builder.WithAlertSettings(interval, minSeverity, connections);
    }

    /// <summary>The same, with the webhook URL from an Aspire parameter.</summary>
    public static IResourceBuilder<WebDataStudioResource> WithAlertWebhook(
        this IResourceBuilder<WebDataStudioResource> builder,
        IResourceBuilder<ParameterResource> webhook, TimeSpan? interval = null,
        string? minSeverity = null, params string[] connections)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(webhook);

        builder.WithEnvironment("WDS_ALERT_WEBHOOK", webhook);

        return builder.WithAlertSettings(interval, minSeverity, connections);
    }

    /// <summary>
    /// Writes a snapshot of every connection's schema on start and says what moved since the last
    /// one — a column added by hand, an index a migration dropped on the way past.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="path">
    /// Directory inside the container. Defaults to <c>/data/snapshots</c>, which is on the studio's
    /// own volume and therefore survives a restart.
    /// </param>
    /// <remarks>
    /// The drift is on <c>GET /api/schema/{connection}/drift</c>, in the log, and — when
    /// <see cref="WithAlertWebhook(IResourceBuilder{WebDataStudioResource}, string, TimeSpan?, string?, string[])"/>
    /// is configured — in a message.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithSchemaSnapshots(
        this IResourceBuilder<WebDataStudioResource> builder, string path = "/data/snapshots")
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        builder.Resource.SchemaSnapshotPath = path;
        return builder.WithEnvironment("WDS_SCHEMA_SNAPSHOT_DIR", path);
    }

    /// <summary>
    /// Lets people keep a result and share it as a link — "here is what I am seeing", without a
    /// screenshot. Off unless this is called.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="ttl">How long a link lives. Default seven days.</param>
    /// <param name="isPublic">
    /// <c>true</c> lets anybody with the link open it, without signing in — which is the point of a
    /// link, and a decision worth making on purpose. <c>false</c> (the default) keeps it behind the
    /// studio's login.
    /// </param>
    /// <param name="maxRows">Rows a link keeps. Default 1000, capped at 10000.</param>
    /// <remarks>
    /// A link is a snapshot: it shows the rows as they were, cannot run anything, and cannot show
    /// more than the person who made it could see — masked columns are masked before the rows are
    /// stored. Only reading statements can be shared.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithSharedResults(
        this IResourceBuilder<WebDataStudioResource> builder, TimeSpan? ttl = null,
        bool isPublic = false, int? maxRows = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Resource.SharingEnabled = true;
        builder.Resource.SharingIsPublic = isPublic;

        builder.WithEnvironment("WDS_SHARE_ENABLED", "true");
        if (isPublic) builder.WithEnvironment("WDS_SHARE_PUBLIC", "true");

        if (ttl is { } lifetime)
        {
            if (lifetime < TimeSpan.FromHours(1))
                throw new ArgumentOutOfRangeException(nameof(ttl), lifetime,
                    "a link that expires within the hour is not a link anybody can use");

            builder.WithEnvironment("WDS_SHARE_TTL_HOURS",
                ((int)lifetime.TotalHours).ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (maxRows is { } rows)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
            builder.WithEnvironment("WDS_SHARE_MAX_ROWS",
                rows.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return builder;
    }

    private static IResourceBuilder<WebDataStudioResource> WithAlertSettings(
        this IResourceBuilder<WebDataStudioResource> builder, TimeSpan? interval,
        string? minSeverity, string[] connections)
    {
        if (interval is { } every)
        {
            if (every < TimeSpan.FromMinutes(1))
                throw new ArgumentOutOfRangeException(nameof(interval), every,
                    "a sweep runs the analysis on every connection; once a minute is already often");

            builder.WithEnvironment("WDS_ALERT_INTERVAL_MINUTES",
                ((int)every.TotalMinutes).ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (minSeverity is { Length: > 0 })
        {
            var severity = minSeverity.Trim().ToLowerInvariant();

            if (severity is not ("info" or "warning" or "critical"))
                throw new ArgumentOutOfRangeException(nameof(minSeverity), minSeverity,
                    "severity has to be info, warning or critical");

            builder.WithEnvironment("WDS_ALERT_MIN_SEVERITY", severity);
        }

        var named = (connections ?? []).Where(name => !string.IsNullOrWhiteSpace(name)).ToList();
        if (named.Count > 0)
            builder.WithEnvironment("WDS_ALERT_CONNECTIONS", string.Join(",", named));

        return builder;
    }
}
