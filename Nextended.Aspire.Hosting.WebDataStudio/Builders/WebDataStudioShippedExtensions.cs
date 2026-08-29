using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// One dashboard the deployment ships: a page of statements everybody who opens the studio sees.
/// </summary>
/// <param name="Name">What the dashboard is called.</param>
/// <param name="Tiles">The boxes on it.</param>
/// <param name="RefreshSeconds">How often the tiles run themselves. 0 means only when asked; below 10 is rounded up.</param>
public sealed record StudioDashboard(string Name, IReadOnlyList<StudioTile> Tiles,
    int RefreshSeconds = 0);

/// <summary>
/// One box on a dashboard.
/// </summary>
/// <param name="Title">What the box is called.</param>
/// <param name="Connection">The connection to run on, by the name the studio shows.</param>
/// <param name="Sql">The statement.</param>
/// <param name="View"><c>number</c>, <c>table</c> or <c>chart</c>.</param>
/// <param name="Width">How many of the four columns it takes, 1 to 4.</param>
public sealed record StudioTile(string Title, string Connection, string Sql,
    string View = "number", int Width = 1);

/// <summary>
/// One editor snippet the deployment ships. <c>${1:name}</c> is a tab stop, the way the studio's
/// own snippets are written.
/// </summary>
public sealed record StudioSnippet(string Prefix, string Label, string Body, string? Description = null);

/// <summary>
/// A connection the studio should have that is not a resource in this stack — a legacy server, a
/// read-only replica somebody else runs.
/// </summary>
public sealed record StudioConnectionEntry(string Name, string Engine, string ConnectionString,
    bool ReadOnly = false, string? Color = null, string? Group = null);

/// <summary>
/// One backup the studio takes on its own, without anybody remembering to.
/// </summary>
/// <param name="Name">What the job is called. It also names the files it writes.</param>
/// <param name="Connection">Which connection to dump, by the name the studio shows.</param>
/// <param name="EveryMinutes">Every so many minutes, or null for a daily one.</param>
/// <param name="DailyAtUtc">Once a day at this time in UTC, e.g. <c>02:00</c>.</param>
/// <param name="Format">plain, custom or tar, where the engine's tool has a choice.</param>
/// <param name="SchemaOnly">The shape without the rows.</param>
/// <param name="Keep">How many files of this job to keep. The oldest go, because a volume that
/// fills up is how a backup schedule stops being one.</param>
public sealed record StudioBackup(string Name, string Connection,
    int? EveryMinutes = null, string? DailyAtUtc = null, string? Format = null,
    bool SchemaOnly = false, int Keep = 7);

/// <summary>
/// The rest of what a deployment can bring with it: connections that are not resources, the masking
/// baseline, dashboards, editor snippets, and the preferences a studio starts with.
/// </summary>
/// <remarks>
/// Same deal as the queries and the quality rules: written here or read from a file, both at once
/// where that is what you want, and what the deployment ships belongs to it — the studio shows it
/// and cannot change or delete it.
/// </remarks>
public static class WebDataStudioShippedExtensions
{
    private const string BackupScheduleFolder = "/data/backup-schedule-inline";
    private const string BackupScheduleTarget = "/data/backup-schedule";

    private const string ConnectionsFolder = "/data/connections-inline";
    private const string MaskingFolder = "/data/masking-inline";
    private const string DashboardsFolder = "/data/dashboards-inline";
    private const string SnippetsFolder = "/data/snippets-inline";
    private const string PreferencesFolder = "/data/preferences-inline";

    private const string ConnectionsTarget = "/data/connections";
    private const string MaskingTarget = "/data/masking";
    private const string DashboardsTarget = "/data/dashboards";
    private const string SnippetsTarget = "/data/snippets";

    // --- connections ------------------------------------------------------------------------------

    /// <summary>
    /// Adds connections that are not resources in this stack — ten legacy servers are a file
    /// somebody reviews rather than ten <c>WithConnection</c> calls.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="connections">The connections.</param>
    /// <remarks>
    /// These are environment connections like any other: read-only in the UI, and a redeploy is what
    /// changes them. A connection string with a password in it belongs in a parameter — see
    /// <c>WithConnection</c>, which takes one.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithConnections(
        this IResourceBuilder<WebDataStudioResource> builder,
        params StudioConnectionEntry[] connections)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var written = (connections ?? []).Where(one => one is not null).ToList();

        foreach (var connection in written)
            if (string.IsNullOrWhiteSpace(connection.Name)
                || string.IsNullOrWhiteSpace(connection.Engine)
                || string.IsNullOrWhiteSpace(connection.ConnectionString))
                throw new ArgumentException(
                    "a connection needs a name, an engine and a connection string", nameof(connections));

        if (written.Count == 0) return builder;

        return WebDataStudioInlineFiles.AddJson(builder, "WDS_CONNECTIONS_FILE", ConnectionsFolder,
            "connections.json",
            written.Select(one => new
            {
                name = one.Name,
                engine = one.Engine,
                connectionString = one.ConnectionString,
                readOnly = one.ReadOnly,
                color = one.Color,
                group = one.Group,
            }));
    }

    /// <summary>
    /// Reads connections from a JSON file in your repository — the same array
    /// <see cref="WithConnections"/> writes.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithConnectionsFromFile(
        this IResourceBuilder<WebDataStudioResource> builder, string path)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var target = $"{ConnectionsTarget}/{Path.GetFileName(path)}";
        builder.WithBindMount(path, target, isReadOnly: true);

        return WebDataStudioInlineFiles.Mounted(builder, "WDS_CONNECTIONS_FILE", target);
    }

    // --- masking ----------------------------------------------------------------------------------

    /// <summary>
    /// Reads the masking baseline from a JSON file:
    /// <c>{ "maskByDefault": true, "extra": [...], "never": [...] }</c>.
    /// </summary>
    /// <remarks>
    /// Three variables are fine for three columns; a long list is a file a review can catch.
    /// <c>WithMaskedColumns</c> and <c>WithUnmaskedColumns</c> still work, and both count.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithMaskingFromFile(
        this IResourceBuilder<WebDataStudioResource> builder, string path)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var target = $"{MaskingTarget}/{Path.GetFileName(path)}";
        builder.WithBindMount(path, target, isReadOnly: true);

        return WebDataStudioInlineFiles.Mounted(builder, "WDS_MASK_FILE", target);
    }

    // --- dashboards -------------------------------------------------------------------------------

    /// <summary>
    /// Ships a dashboard with the stack: the numbers somebody asks for every morning, on a page that
    /// is there the first time anybody opens the studio.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="dashboards">The dashboards.</param>
    /// <remarks>
    /// A dashboard that comes with the deployment belongs to it: the studio shows it and cannot
    /// change or delete it. Somebody who wants it different saves a copy under another name.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithDashboards(
        this IResourceBuilder<WebDataStudioResource> builder, params StudioDashboard[] dashboards)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var written = (dashboards ?? []).Where(one => one is not null).ToList();

        foreach (var dashboard in written)
        {
            if (string.IsNullOrWhiteSpace(dashboard.Name))
                throw new ArgumentException("a dashboard needs a name", nameof(dashboards));

            foreach (var tile in dashboard.Tiles ?? [])
                if (string.IsNullOrWhiteSpace(tile.Sql) || string.IsNullOrWhiteSpace(tile.Connection))
                    throw new ArgumentException(
                        $"a tile on '{dashboard.Name}' says no statement or no connection",
                        nameof(dashboards));
        }

        if (written.Count == 0) return builder;

        return WebDataStudioInlineFiles.AddJson(builder, "WDS_DASHBOARD_FILE", DashboardsFolder,
            "dashboards.json",
            written.Select(dashboard => new
            {
                name = dashboard.Name,
                refreshSeconds = dashboard.RefreshSeconds,
                tiles = (dashboard.Tiles ?? []).Select(tile => new
                {
                    title = tile.Title,
                    // The studio resolves a connection by name as well as by id.
                    connectionId = tile.Connection,
                    sql = tile.Sql,
                    view = tile.View,
                    width = tile.Width,
                }),
            }));
    }

    /// <summary>
    /// Reads dashboards from a JSON file in your repository — the same array
    /// <see cref="WithDashboards"/> writes.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithDashboardsFromFile(
        this IResourceBuilder<WebDataStudioResource> builder, string path)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var target = $"{DashboardsTarget}/{Path.GetFileName(path)}";
        builder.WithBindMount(path, target, isReadOnly: true);

        return WebDataStudioInlineFiles.Mounted(builder, "WDS_DASHBOARD_FILE", target);
    }


    // --- backups the studio takes on its own -------------------------------------------------------

    /// <summary>
    /// Takes a dump on a schedule, so a stack you leave running has something to go back to.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="directory">Where the files go inside the container. Mount a volume there, or
    /// the dumps live exactly as long as the container does.</param>
    /// <param name="backups">The jobs.</param>
    /// <remarks>
    /// <para>
    /// The dumping is the engine's own tool — <c>pg_dump</c>, <c>mysqldump</c>,
    /// <c>mongodump</c> — which has to be in the studio's image; the studio says so rather than
    /// writing an empty file when it is not.
    /// </para>
    /// <para>
    /// Two ways of saying when: <c>EveryMinutes</c>, or <c>DailyAtUtc</c>. There is no cron
    /// parser, on purpose: nobody asked the studio to be a scheduler.
    /// </para>
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithBackupSchedule(
        this IResourceBuilder<WebDataStudioResource> builder, string directory,
        params StudioBackup[] backups)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        var written = (backups ?? []).Where(one => one is not null).ToList();

        foreach (var backup in written)
        {
            if (string.IsNullOrWhiteSpace(backup.Name) || string.IsNullOrWhiteSpace(backup.Connection))
                throw new ArgumentException("a backup job needs a name and a connection", nameof(backups));

            if (backup.EveryMinutes is null && string.IsNullOrWhiteSpace(backup.DailyAtUtc))
                throw new ArgumentException(
                    $"'{backup.Name}' never runs: say EveryMinutes or DailyAtUtc", nameof(backups));

            if (backup.EveryMinutes is { } minutes and < 1)
                throw new ArgumentOutOfRangeException(nameof(backups),
                    $"'{backup.Name}' would run every {minutes} minutes");

            if (backup.Keep < 0)
                throw new ArgumentOutOfRangeException(nameof(backups),
                    $"'{backup.Name}' cannot keep {backup.Keep} files");
        }

        if (written.Count == 0) return builder;

        builder.WithEnvironment("WDS_BACKUP_DIR", directory);

        return WebDataStudioInlineFiles.AddJson(builder, "WDS_BACKUP_SCHEDULE_FILE",
            BackupScheduleFolder, "backups.json",
            written.Select(backup => new
            {
                name = backup.Name,
                connection = backup.Connection,
                everyMinutes = backup.EveryMinutes,
                dailyAtUtc = backup.DailyAtUtc,
                format = backup.Format,
                schemaOnly = backup.SchemaOnly,
                keep = backup.Keep,
            }));
    }

    /// <summary>
    /// The same schedule, kept as JSON in your repository. The file has the shape
    /// <see cref="WithBackupSchedule(IResourceBuilder{WebDataStudioResource}, string, StudioBackup[])"/>
    /// writes.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithBackupScheduleFromFile(
        this IResourceBuilder<WebDataStudioResource> builder, string path, string directory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        var target = $"{BackupScheduleTarget}/{Path.GetFileName(path)}";
        builder.WithBindMount(path, target, isReadOnly: true);
        builder.WithEnvironment("WDS_BACKUP_DIR", directory);

        return WebDataStudioInlineFiles.Mounted(builder, "WDS_BACKUP_SCHEDULE_FILE", target);
    }

    // --- snippets ---------------------------------------------------------------------------------

    /// <summary>
    /// Ships editor snippets with the stack — the tenant filter everybody types, in everybody's
    /// completion list.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="snippets">The snippets. <c>${1:name}</c> is a tab stop.</param>
    /// <remarks>
    /// A person's own snippet with the same prefix wins for that person, the way one of their own
    /// wins over a built-in.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithSnippets(
        this IResourceBuilder<WebDataStudioResource> builder, params StudioSnippet[] snippets)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var written = (snippets ?? []).Where(one => one is not null).ToList();

        foreach (var snippet in written)
            if (string.IsNullOrWhiteSpace(snippet.Prefix) || string.IsNullOrWhiteSpace(snippet.Body))
                throw new ArgumentException("a snippet needs a prefix and a body", nameof(snippets));

        if (written.Count == 0) return builder;

        return WebDataStudioInlineFiles.AddJson(builder, "WDS_SNIPPETS_FILE", SnippetsFolder,
            "snippets.json",
            written.Select(snippet => new
            {
                prefix = snippet.Prefix,
                label = string.IsNullOrWhiteSpace(snippet.Label) ? snippet.Prefix : snippet.Label,
                body = snippet.Body,
                description = snippet.Description,
            }));
    }

    /// <summary>
    /// Reads editor snippets from a JSON file in your repository.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithSnippetsFromFile(
        this IResourceBuilder<WebDataStudioResource> builder, string path)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var target = $"{SnippetsTarget}/{Path.GetFileName(path)}";
        builder.WithBindMount(path, target, isReadOnly: true);

        return WebDataStudioInlineFiles.Mounted(builder, "WDS_SNIPPETS_FILE", target);
    }

    // --- preferences ------------------------------------------------------------------------------

    /// <summary>
    /// The preferences a studio starts with, before anybody has changed one: the time zone
    /// timestamps are shown in, how many rows a page holds, and the rest.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="timeZone"><c>local</c>, <c>utc</c>, or an IANA name like <c>Europe/Berlin</c>.</param>
    /// <param name="pageSize">Rows per page in the data tab.</param>
    /// <param name="inspectBeforeRun">Whether a statement is read before it runs.</param>
    /// <param name="notifyAfterSeconds">When a finished query says so. 0 switches it off.</param>
    /// <param name="historySnapshots">Whether a history entry keeps its result.</param>
    /// <param name="snapshotRows">How many rows a snapshot keeps.</param>
    /// <remarks>
    /// A starting point, not a lock: the first person to change one of these keeps their change, and
    /// what this does not name keeps the studio's own default. Setting the time zone to <c>utc</c>
    /// is the usual reason — a screenshot of a shared studio then cannot be misread.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithDefaultPreferences(
        this IResourceBuilder<WebDataStudioResource> builder,
        string? timeZone = null, int? pageSize = null, bool? inspectBeforeRun = null,
        int? notifyAfterSeconds = null, bool? historySnapshots = null, int? snapshotRows = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (pageSize is { } rows and < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), rows, "a page holds at least one row");

        if (notifyAfterSeconds is { } seconds and < 0)
            throw new ArgumentOutOfRangeException(nameof(notifyAfterSeconds), seconds,
                "0 switches the notification off; there is nothing below that");

        return WebDataStudioInlineFiles.AddJsonObject(builder, "WDS_PREFERENCES_FILE",
            PreferencesFolder, "preferences.json",
            new
            {
                timeZone,
                pageSize,
                inspectBeforeRun,
                notifyAfterSeconds,
                historySnapshots,
                snapshotRows,
            });
    }
}
