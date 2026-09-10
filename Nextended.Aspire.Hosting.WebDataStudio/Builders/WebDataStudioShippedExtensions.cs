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
/// What a widget draws. The names are the studio's own, so a new one there is a new one here
/// without anything in between having to be taught about it.
/// </summary>
public enum StudioWidgetType
{
    /// <summary>One headline number.</summary>
    Stat,
    /// <summary>A number against a range. Needs <c>Min</c> and <c>Max</c>.</summary>
    Gauge,
    /// <summary>Magnitude per category.</summary>
    Bar,
    /// <summary>Parts per category.</summary>
    StackedBar,
    /// <summary>Part of a whole, few parts.</summary>
    Pie,
    /// <summary>Part of a whole, many parts.</summary>
    Treemap,
    /// <summary>Change over time.</summary>
    Line,
    /// <summary>Change over time, filled.</summary>
    Area,
    /// <summary>Parts over time.</summary>
    StackedArea,
    /// <summary>A shape, small — for a strip of them.</summary>
    Sparkline,
    /// <summary>Rows as rows.</summary>
    Table,
    /// <summary>A label and a value per row, for a top ten.</summary>
    List,
    /// <summary>Density across two dimensions.</summary>
    Heatmap,
    /// <summary>Where the rows are, from latitude and longitude or a geometry column.</summary>
    GeoMap,
    /// <summary>Flow from one thing to another.</summary>
    Sankey,
    /// <summary>Markdown, with the dashboard's variables filled in.</summary>
    Text,
    /// <summary>A band that collapses, to put a name on a group of widgets.</summary>
    Row,
}

/// <summary>
/// One threshold and the state it means: <c>good</c>, <c>warning</c>, <c>serious</c> or
/// <c>critical</c>. The four are reserved — they are what a threshold means, not colours to pick.
/// </summary>
public sealed record StudioThreshold(double Value, string Level);

/// <summary>
/// One of a widget's sources, for a widget that spans connections. The studio stages each source
/// query in an in-memory DuckDB table named by its alias and runs the widget's own statement there.
/// </summary>
public sealed record StudioWidgetSource(string Connection, string Sql, string Alias);

/// <summary>
/// A dashboard variable: values from a statement, or a written list. It reaches a widget's SQL as
/// <c>$name</c>, <c>${name}</c> or <c>${name:csv}</c> for a list.
/// </summary>
public sealed record StudioVariable(string Name, IReadOnlyList<string>? Values = null,
    string? Connection = null, string? Sql = null, bool Multi = false, bool IncludeAll = false,
    string? Default = null, string? Label = null);

/// <summary>
/// One widget on a dashboard.
/// </summary>
/// <param name="Title">What it is called.</param>
/// <param name="Type">What it draws.</param>
/// <param name="Connection">The connection to run on, by the name the studio shows.</param>
/// <param name="Sql">Its statement. <c>$__timeFilter(column)</c>, <c>$__from</c>, <c>$__to</c>,
/// <c>$__interval</c> and the dashboard's variables are filled in by the studio.</param>
/// <param name="Width">How many of the twenty-four columns it takes.</param>
/// <param name="Height">How many rows tall it is.</param>
/// <param name="X">Which column it starts at. Left out, the widgets flow left to right.</param>
/// <param name="Y">Which row it starts at. Left out, the widgets flow left to right.</param>
/// <param name="Category">The column that names the things. Left out, the first column that is not
/// a number.</param>
/// <param name="Series">One series per value of this column, for <c>SELECT day, region, count(*)</c>.</param>
/// <param name="Value">The column that measures. Left out, every numeric column.</param>
/// <param name="Sources">Several connections instead of one, joined by <paramref name="Sql"/>.</param>
public sealed record StudioWidget(string Title,
    StudioWidgetType Type = StudioWidgetType.Table,
    string? Connection = null,
    string? Sql = null,
    int Width = 12,
    int Height = 6,
    int? X = null,
    int? Y = null,
    string? Unit = null,
    int? Decimals = null,
    double? Min = null,
    double? Max = null,
    string? Category = null,
    string? Series = null,
    string? Value = null,
    string? Latitude = null,
    string? Longitude = null,
    string? From = null,
    string? To = null,
    string? Weight = null,
    string? Markdown = null,
    string? Description = null,
    bool Horizontal = false,
    IReadOnlyList<StudioThreshold>? Thresholds = null,
    IReadOnlyList<StudioWidgetSource>? Sources = null);

/// <summary>
/// A dashboard as a canvas: twenty-four columns, one time range, and the variables its statements
/// read. The shape the studio keeps its own dashboards in.
/// </summary>
/// <param name="Name">What the dashboard is called.</param>
/// <param name="Widgets">The widgets on it.</param>
/// <param name="RefreshSeconds">How often it runs itself. 0 means only when asked; below 10 is
/// rounded up by the studio.</param>
/// <param name="From">The start of its time range, relative (<c>now-24h</c>) or ISO.</param>
/// <param name="To">The end of it.</param>
public sealed record StudioCanvas(string Name, IReadOnlyList<StudioWidget> Widgets,
    int RefreshSeconds = 0, string From = "now-24h", string To = "now",
    IReadOnlyList<StudioVariable>? Variables = null, IReadOnlyList<string>? Tags = null);

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
    /// Ships a dashboard as a canvas: twenty-four columns, fifteen widget types, one time range and
    /// the variables its statements read.
    /// </summary>
    /// <remarks>
    /// A dashboard that comes with the deployment belongs to it: the studio shows it and cannot
    /// change or delete it. Somebody who wants it different saves a copy under another name.
    /// <para>
    /// Widgets without <c>X</c> and <c>Y</c> flow left to right and wrap, which is what a page
    /// written as a list of widgets is meant to look like.
    /// </para>
    /// </remarks>
    /// <param name="builder">The studio.</param>
    /// <param name="dashboards">The dashboards.</param>
    public static IResourceBuilder<WebDataStudioResource> WithDashboards(
        this IResourceBuilder<WebDataStudioResource> builder, params StudioCanvas[] dashboards)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var written = (dashboards ?? []).Where(one => one is not null).ToList();

        foreach (var dashboard in written) Check(dashboard);

        if (written.Count == 0) return builder;

        return WebDataStudioInlineFiles.AddJson(builder, "WDS_DASHBOARD_FILE", DashboardsFolder,
            "canvas.json", written.Select(Document));
    }

    /// <summary>
    /// Reads dashboards from Grafana JSON — a file, or a folder of them.
    /// </summary>
    /// <remarks>
    /// Nothing here says which format the files are in, because nothing has to: the studio decides
    /// per file, and reads its own shape, the older tile shape and Grafana's the same way. What
    /// cannot come along from a Grafana dashboard — a Prometheus query, a panel type the studio does
    /// not draw, transformations, alert rules — is a line in the studio's log rather than a silently
    /// empty widget.
    /// <para>
    /// A team with thirteen exported dashboards has them in exactly this shape, which is the point:
    /// point this at the folder and they are in the studio.
    /// </para>
    /// </remarks>
    /// <param name="builder">The studio.</param>
    /// <param name="path">A Grafana JSON file, or a folder of them, next to the app host.</param>
    public static IResourceBuilder<WebDataStudioResource> WithGrafanaDashboards(
        this IResourceBuilder<WebDataStudioResource> builder, string path)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var full = Path.GetFullPath(path);
        var target = Directory.Exists(full)
            ? $"{DashboardsTarget}/grafana"
            : $"{DashboardsTarget}/grafana/{Path.GetFileName(full)}";

        builder.WithBindMount(full, target, isReadOnly: true);

        return WebDataStudioInlineFiles.Mounted(builder, "WDS_DASHBOARD_FILE", target);
    }

    /// What a dashboard has to say for itself before a container is built around it. The studio
    /// would cope — a widget it cannot draw says so in its own frame — but an app host that can
    /// catch it should.
    private static void Check(StudioCanvas dashboard)
    {
        if (string.IsNullOrWhiteSpace(dashboard.Name))
            throw new ArgumentException("a dashboard needs a name", nameof(dashboard));

        foreach (var widget in dashboard.Widgets ?? [])
        {
            if (widget.Type is StudioWidgetType.Row or StudioWidgetType.Text) continue;

            var federated = widget.Sources is { Count: > 0 };

            if (string.IsNullOrWhiteSpace(widget.Sql))
                throw new ArgumentException(
                    $"the widget '{widget.Title}' on '{dashboard.Name}' says no statement",
                    nameof(dashboard));

            if (!federated && string.IsNullOrWhiteSpace(widget.Connection))
                throw new ArgumentException(
                    $"the widget '{widget.Title}' on '{dashboard.Name}' says no connection",
                    nameof(dashboard));

            // The studio refuses to draw a gauge without a range rather than inventing a scale, so
            // this would be a container that comes up with a widget nobody can read.
            if (widget.Type == StudioWidgetType.Gauge && (widget.Min is null || widget.Max is null))
                throw new ArgumentException(
                    $"the gauge '{widget.Title}' on '{dashboard.Name}' needs Min and Max: a gauge "
                    + "without a range would need an invented scale",
                    nameof(dashboard));

            foreach (var threshold in widget.Thresholds ?? [])
                if (!Levels.Contains(threshold.Level))
                    throw new ArgumentException(
                        $"'{threshold.Level}' is not a state a threshold can mean; the four are "
                        + string.Join(", ", Levels),
                        nameof(dashboard));

            foreach (var source in widget.Sources ?? [])
                if (string.IsNullOrWhiteSpace(source.Alias) || string.IsNullOrWhiteSpace(source.Sql)
                    || string.IsNullOrWhiteSpace(source.Connection))
                    throw new ArgumentException(
                        $"a source of '{widget.Title}' on '{dashboard.Name}' is missing its "
                        + "connection, its statement or its alias",
                        nameof(dashboard));
        }
    }

    private static readonly string[] Levels = ["good", "warning", "serious", "critical"];

    /// The dashboard in the studio's own shape. Written here rather than translated there, so a
    /// deployment's page is the same document a person would have built.
    private static object Document(StudioCanvas dashboard)
    {
        var widgets = new List<object>();
        var (x, y, tallest) = (0, 0, 0);

        foreach (var widget in dashboard.Widgets ?? [])
        {
            var w = Math.Clamp(widget.Width, 1, 24);
            var h = Math.Max(widget.Type == StudioWidgetType.Row ? 1 : 2, widget.Height);
            var flowing = widget.X is null || widget.Y is null;

            // Left to right, wrapping at the edge of the grid — unless the widget says where it
            // belongs, in which case it belongs there.
            if (flowing)
            {
                if (x + w > 24) (x, y, tallest) = (0, y + tallest, 0);
                tallest = Math.Max(tallest, h);
            }

            var at = new { x = widget.X ?? x, y = widget.Y ?? y, w, h };
            if (flowing) x += w;

            widgets.Add(new
            {
                id = "",
                type = widget.Type.ToString(),
                title = widget.Title,
                description = widget.Description,
                position = at,
                source = widget.Sources is { Count: > 0 } sources
                    ? new
                    {
                        kind = "Federated",
                        connectionId = (string?)null,
                        sql = widget.Sql,
                        sources = sources.Select(one => new
                        {
                            // The studio resolves a connection by name as well as by id.
                            connectionId = one.Connection,
                            sql = one.Sql,
                            alias = one.Alias,
                        }).ToList<object>(),
                    }
                    : new
                    {
                        kind = "Sql",
                        connectionId = widget.Connection,
                        sql = widget.Sql,
                        sources = new List<object>(),
                    },
                mapping = new
                {
                    category = widget.Category,
                    series = widget.Series,
                    values = widget.Value is null ? Array.Empty<string>() : [widget.Value],
                    latitude = widget.Latitude,
                    longitude = widget.Longitude,
                    from = widget.From,
                    to = widget.To,
                    weight = widget.Weight,
                },
                options = new
                {
                    unit = widget.Unit,
                    decimals = widget.Decimals,
                    min = widget.Min,
                    max = widget.Max,
                    thresholds = (widget.Thresholds ?? []).Select(one => new
                    {
                        value = one.Value,
                        level = one.Level,
                    }),
                    legend = true,
                    stacked = widget.Type is StudioWidgetType.StackedBar or StudioWidgetType.StackedArea,
                    orientation = widget.Horizontal ? "horizontal" : null,
                    markdown = widget.Markdown,
                },
            });
        }

        return new
        {
            name = dashboard.Name,
            refreshSeconds = dashboard.RefreshSeconds,
            timeRange = new { from = dashboard.From, to = dashboard.To },
            variables = (dashboard.Variables ?? []).Select(one => new
            {
                name = one.Name,
                kind = one.Sql is { Length: > 0 } ? "Query" : "Custom",
                label = one.Label,
                connectionId = one.Connection,
                sql = one.Sql,
                values = one.Values ?? [],
                multi = one.Multi,
                includeAll = one.IncludeAll,
                @default = one.Default,
            }),
            tags = dashboard.Tags ?? [],
            widgets,
        };
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
