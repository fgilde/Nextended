using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// One saved query, written in the app host rather than kept as a file.
/// </summary>
/// <param name="Name">What the panel calls it.</param>
/// <param name="Sql">The statement.</param>
/// <param name="Folder">The folder in the saved-queries panel. Optional.</param>
/// <param name="Connection">The connection it belongs to, by the name the studio shows. Optional.</param>
public sealed record SavedStudioQuery(string Name, string Sql, string? Folder = null,
    string? Connection = null);

/// <summary>
/// One export format, written as text with placeholders rather than as code.
/// </summary>
/// <param name="Id">The id the studio lists it under. Saving a copy under another id is how somebody changes it.</param>
/// <param name="Label">What the export dialog calls it.</param>
/// <param name="Extension">The file extension, without the dot.</param>
/// <param name="ContentType">The content type the download carries.</param>
/// <param name="Row">The text written per row. <c>{{values}}</c>, <c>{{col.NAME}}</c>, <c>{{index}}</c>.</param>
/// <param name="Header">Written once before the rows. <c>{{table}}</c>, <c>{{columns}}</c>.</param>
/// <param name="Footer">Written once after them.</param>
/// <param name="Separator">What joins <c>{{columns}}</c> and <c>{{values}}</c>.</param>
public sealed record StudioExportTemplate(
    string Id, string Label, string Extension, string ContentType, string Row,
    string? Header = null, string? Footer = null, string Separator = ", ");

/// <summary>
/// One rule about the rows rather than about the schema.
/// </summary>
/// <param name="Connection">The connection, by the name the studio shows.</param>
/// <param name="Table">The table the rule is about.</param>
/// <param name="Kind">
/// <c>NotNull</c>, <c>Unique</c>, <c>Range</c>, <c>Referential</c>, <c>Freshness</c> or
/// <c>Expression</c>.
/// </param>
/// <param name="Column">The column. Not needed by <c>Expression</c>, which names its own.</param>
/// <param name="Schema">The schema, where the engine has them.</param>
/// <param name="Argument"><c>0..100</c>, <c>customers.id</c>, <c>24h</c>, or the condition a bad row satisfies.</param>
/// <param name="Message">What to say when it fails.</param>
public sealed record StudioQualityRule(
    string Connection, string Table, string Kind, string? Column = null, string? Schema = null,
    string? Argument = null, string? Message = null, bool Enabled = true);

/// <summary>
/// The things a repository can hold, written in the app host instead — and both at once where that
/// is what you want.
/// </summary>
/// <remarks>
/// Each of these settings takes a list of paths, so <c>WithSavedQueriesFromDirectory</c> and
/// <c>WithSavedQueries</c> add up rather than replacing each other. The inline files are created
/// inside the container, so a published stack carries them the same way a local one does — there is
/// no folder on anybody's machine to keep in step.
/// </remarks>
/// <summary>
/// Tables to copy from one connection into another when the stack comes up, for a development
/// database that should not start out empty.
/// </summary>
/// <param name="From">Where the rows are, by the name the studio shows.</param>
/// <param name="To">Where they should be.</param>
/// <param name="Tables">Which tables. Each is created in the target and filled.</param>
/// <param name="MaxRows">At most this many rows per table. 10 000 by default: this is a seed, not
/// a replica.</param>
/// <param name="Schema">The schema to create them in, where the target engine has schemas.</param>
public sealed record StudioSeedCopy(string From, string To, IReadOnlyList<string> Tables,
    int? MaxRows = null, string? Schema = null);

public static class WebDataStudioInlineExtensions
{
    private const string SeedFromFolder = "/data/seed-from-inline";
    private const string QueriesFolder = "/data/queries-inline";
    private const string TemplatesFolder = "/data/export-templates-inline";
    private const string QualityFolder = "/data/quality-inline";
    private const string SeedFolder = "/data/seed-inline";

    /// <summary>
    /// Ships saved queries with the stack: the five everybody needs, in the app host rather than in
    /// a chat message.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="queries">The queries. A name and a statement are required; folder and connection are not.</param>
    /// <remarks>
    /// The same rules a <c>.sql</c> file gets: importing is idempotent, so a restart replaces rather
    /// than duplicates, and a query is editable in the studio afterwards — these are a starting
    /// point, not a lock.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithSavedQueries(
        this IResourceBuilder<WebDataStudioResource> builder, params SavedStudioQuery[] queries)
    {
        ArgumentNullException.ThrowIfNull(builder);

        foreach (var query in queries ?? [])
        {
            if (query is null || string.IsNullOrWhiteSpace(query.Name)
                || string.IsNullOrWhiteSpace(query.Sql))
                throw new ArgumentException("a saved query needs a name and a statement", nameof(queries));

            // The header comments are how a .sql file says the same thing, so an inline query and a
            // file are the same thing to the studio.
            var header = new List<string>();
            if (!string.IsNullOrWhiteSpace(query.Connection))
                header.Add($"-- wds:connection {query.Connection.Trim()}");
            if (!string.IsNullOrWhiteSpace(query.Folder))
                header.Add($"-- wds:folder {query.Folder.Trim()}");

            var content = header.Count > 0
                ? string.Join("\n", header) + "\n\n" + query.Sql.Trim() + "\n"
                : query.Sql.Trim() + "\n";

            WebDataStudioInlineFiles.Add(builder, "WDS_SAVED_QUERIES_DIR", QueriesFolder,
                WebDataStudioInlineFiles.FileName(query.Name, ".sql"), content);
        }

        return builder;
    }

    /// <summary>
    /// Ships export formats with the stack, written as text with placeholders.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="templates">The templates.</param>
    /// <remarks>
    /// A template that comes with the deployment belongs to it: the studio exports with it and
    /// cannot change it. Somebody who wants it different saves a copy under another id.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithExportTemplates(
        this IResourceBuilder<WebDataStudioResource> builder, params StudioExportTemplate[] templates)
    {
        ArgumentNullException.ThrowIfNull(builder);

        foreach (var template in templates ?? [])
        {
            if (template is null || string.IsNullOrWhiteSpace(template.Id)
                || string.IsNullOrWhiteSpace(template.Row))
                throw new ArgumentException(
                    "an export template needs an id and a row template", nameof(templates));

            // One file per template, the way the folder version reads them.
            WebDataStudioInlineFiles.AddJsonObject(builder, "WDS_EXPORT_TEMPLATES_DIR", TemplatesFolder,
                WebDataStudioInlineFiles.FileName(template.Id, ".json"),
                new
                {
                    id = template.Id,
                    label = string.IsNullOrWhiteSpace(template.Label) ? template.Id : template.Label,
                    extension = string.IsNullOrWhiteSpace(template.Extension) ? "txt" : template.Extension,
                    contentType = string.IsNullOrWhiteSpace(template.ContentType)
                        ? "text/plain"
                        : template.ContentType,
                    header = template.Header,
                    row = template.Row,
                    footer = template.Footer,
                    separator = template.Separator,
                });
        }

        return builder;
    }

    /// <summary>
    /// Ships data quality rules with the stack — rules about the rows, in the app host rather than
    /// as JSON somebody has to keep in step by hand.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="rules">The rules.</param>
    /// <remarks>
    /// A rule that comes with the deployment runs and reports with the health findings, and the
    /// studio cannot change or delete it. A rule for a connection this studio does not have is
    /// skipped with a line in the log rather than taking the others with it.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithQualityRules(
        this IResourceBuilder<WebDataStudioResource> builder, params StudioQualityRule[] rules)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var written = (rules ?? []).Where(rule => rule is not null).ToList();

        foreach (var rule in written)
        {
            if (string.IsNullOrWhiteSpace(rule.Connection) || string.IsNullOrWhiteSpace(rule.Table))
                throw new ArgumentException(
                    "a quality rule needs a connection and a table", nameof(rules));

            if (string.IsNullOrWhiteSpace(rule.Kind))
                throw new ArgumentException($"the rule on {rule.Table} says no kind", nameof(rules));
        }

        if (written.Count == 0) return builder;

        // One file for all of them: the format is a list, and a list is what the studio reads.
        return WebDataStudioInlineFiles.AddJson(builder, "WDS_QUALITY_FILE", QualityFolder,
            "rules.json",
            written.Select(rule => new
            {
                connection = rule.Connection,
                schema = rule.Schema,
                table = rule.Table,
                column = rule.Column,
                kind = rule.Kind,
                argument = rule.Argument,
                message = rule.Message,
                enabled = rule.Enabled,
            }));
    }

    /// <summary>
    /// Fills one connection from another when the stack comes up — the other kind of seed.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="copies">What to copy where.</param>
    /// <remarks>
    /// <para>
    /// <see cref="WithSeedScript"/> is the answer when you can write the data down. This is the
    /// answer when you cannot: the tables already exist somewhere — a staging server, a container
    /// this stack brought up with a sample database in it — and a fresh stack should come up with
    /// them rather than with empty tables.
    /// </para>
    /// <para>
    /// The same guards a seed script has, plus one: <b>a table that already exists is left
    /// alone</b>. Nothing is copied into a read-only connection, nothing into one coloured red —
    /// the studio's convention for production — and a restart never overwrites what somebody has
    /// been working on.
    /// </para>
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithSeedFrom(
        this IResourceBuilder<WebDataStudioResource> builder, params StudioSeedCopy[] copies)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var written = (copies ?? []).Where(one => one is not null).ToList();

        foreach (var copy in written)
        {
            if (string.IsNullOrWhiteSpace(copy.From) || string.IsNullOrWhiteSpace(copy.To))
                throw new ArgumentException("a seed copy needs a source and a target connection",
                    nameof(copies));

            if (copy.From.Equals(copy.To, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(
                    $"'{copy.From}' cannot be seeded from itself", nameof(copies));

            if (copy.Tables is null || copy.Tables.Count == 0)
                throw new ArgumentException(
                    $"say which tables to copy from '{copy.From}'", nameof(copies));

            if (copy.MaxRows is { } rows and < 1)
                throw new ArgumentOutOfRangeException(nameof(copies),
                    $"copying {rows} rows from '{copy.From}' would copy nothing");
        }

        if (written.Count == 0) return builder;

        return WebDataStudioInlineFiles.AddJson(builder, "WDS_SEED_FROM_FILE", SeedFromFolder,
            "seed-from.json",
            written.Select(copy => new
            {
                from = copy.From,
                to = copy.To,
                tables = copy.Tables,
                maxRows = copy.MaxRows,
                schema = copy.Schema,
            }));
    }

    /// <summary>
    /// Seeds one connection from SQL written here, so a fresh stack comes up with data in it without
    /// a file to keep alongside.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="connectionName">The connection, as the studio names it.</param>
    /// <param name="sql">The script.</param>
    /// <remarks>
    /// For development stacks, with the same three rules the file version has: a script runs once
    /// per content — editing it makes it run again, restarting does not — and never on a read-only
    /// connection or one marked as production.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithSeedScript(
        this IResourceBuilder<WebDataStudioResource> builder, string connectionName, string sql)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        // The studio looks for {CONNECTION}.sql, so the file is named after the connection.
        return WebDataStudioInlineFiles.Add(builder, "WDS_SEED_SQL", SeedFolder,
            $"{connectionName.Trim()}.sql", sql.Trim() + "\n");
    }
}
