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
