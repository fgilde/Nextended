using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// Things a repository can hold and a review can catch: the queries everybody on the team needs, and
/// the data that makes a fresh database worth opening. Both are folders on your machine, mounted
/// into the studio and read at start.
/// </summary>
public static class WebDataStudioFilesExtensions
{
    private const string QueriesTarget = "/data/queries";
    private const string SeedTarget = "/data/seed";
    private const string TemplatesTarget = "/data/export-templates";
    private const string QualityTarget = "/data/quality";

    /// <summary>
    /// Mounts a folder of export templates — an export format written as text with placeholders,
    /// rather than as code the studio would have to run.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="path">A folder of <c>.json</c> templates on your machine.</param>
    /// <remarks>
    /// A template has an id, a label, a file extension, a content type and up to three pieces of
    /// text: a header, a row and a footer. The placeholders are <c>{{table}}</c>,
    /// <c>{{columns}}</c>, <c>{{values}}</c>, <c>{{index}}</c>, <c>{{comma}}</c> and
    /// <c>{{col.NAME}}</c>, each of which takes a filter for the escaping that format needs:
    /// <c>{{values|sql}}</c>, <c>json</c>, <c>csv</c>, <c>html</c>, <c>upper</c>, <c>lower</c>.
    /// <para>
    /// A template mounted this way belongs to the deployment: the studio lists it and exports with
    /// it, and somebody who wants it different saves a copy under another id.
    /// </para>
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithExportTemplates(
        this IResourceBuilder<WebDataStudioResource> builder, string path)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Read-only: the studio reads these, it does not own them.
        return builder
            .WithBindMount(path, TemplatesTarget, isReadOnly: true)
            .WithEnvironment("WDS_EXPORT_TEMPLATES_DIR", TemplatesTarget);
    }

    /// <summary>
    /// Mounts the data quality rules the deployment owns — rules about the rows rather than about the
    /// schema, kept in the repository with everything else that has to survive a rollout.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="path">A <c>.json</c> file, or a folder of them, on your machine.</param>
    /// <remarks>
    /// Each entry names a connection the way a person does — by name — plus a table, a column and a
    /// kind: <c>NotNull</c>, <c>Unique</c>, <c>Range</c> (<c>"argument": "0..100"</c>),
    /// <c>Referential</c> (<c>"customers.id"</c>), <c>Freshness</c> (<c>"24h"</c>) or
    /// <c>Expression</c> (the condition a bad row satisfies).
    /// <para>
    /// A rule mounted this way belongs to the deployment: the studio runs it and reports it with the
    /// health findings, and cannot change or delete it. A rule for a connection this studio does not
    /// have is skipped with a line in the log rather than breaking the others.
    /// </para>
    /// <example>
    /// <code>
    /// [
    ///   {
    ///     "connection": "SHOP",
    ///     "schema": "public",
    ///     "table": "invoices",
    ///     "column": "customer_id",
    ///     "kind": "NotNull",
    ///     "message": "every invoice needs a customer"
    ///   }
    /// ]
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithQualityRules(
        this IResourceBuilder<WebDataStudioResource> builder, string path)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // A file rather than a folder keeps its own name inside the container, so the studio reads
        // exactly what was mounted either way. Asked of the file system rather than of the name: a
        // folder is allowed to have a dot in it, and "rules.d" is not a file.
        var isFile = File.Exists(path)
                     || (!Directory.Exists(path) && Path.GetExtension(path).Length > 0);

        var target = isFile ? $"{QualityTarget}/{Path.GetFileName(path)}" : QualityTarget;

        // Read-only: the studio runs these, it does not own them.
        return builder
            .WithBindMount(path, target, isReadOnly: true)
            .WithEnvironment("WDS_QUALITY_FILE", target);
    }

    /// <summary>
    /// Limits which schemas a connection reads at all — the tree, the completion cache, the object
    /// search and the schema snapshot each walk what they are given, and on a server with five
    /// thousand tables that is the difference between a tree that opens and one that does not.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="connectionName">The connection, as it is named in the studio.</param>
    /// <param name="schemas">The schemas to read. Nothing given reads all of them.</param>
    /// <remarks>
    /// Set here, the scope is the deployment's and the studio cannot widen it. Left unset, somebody
    /// can choose a scope for their own studio in the connection's properties.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithSchemas(
        this IResourceBuilder<WebDataStudioResource> builder, string connectionName,
        params string[] schemas)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        if (schemas.Length == 0) return builder;

        var name = WebDataStudioNaming.ToVariableSuffix(connectionName);

        return builder.WithEnvironment($"WDS_CONN_{name}_SCHEMAS", string.Join(',', schemas));
    }

    /// <summary>
    /// Imports every <c>.sql</c> file in a folder as a saved query, so a stack ships the five
    /// queries everybody needs instead of pasting them into a chat.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="path">A folder on your machine. Subfolders become the folders in the panel.</param>
    /// <remarks>
    /// A file may name its connection and folder in comments — <c>-- wds:connection SHOP</c>,
    /// <c>-- wds:folder Ops</c> — and it is still a file the database accepts. Importing is
    /// idempotent: a restart replaces rather than duplicates.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithSavedQueriesFromDirectory(
        this IResourceBuilder<WebDataStudioResource> builder, string path)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        builder.Resource.SavedQueriesPath = path;

        // Read-only: the studio imports these, it does not own them.
        return builder
            .WithBindMount(path, QueriesTarget, isReadOnly: true)
            .WithEnvironment("WDS_SAVED_QUERIES_DIR", QueriesTarget);
    }

    /// <summary>
    /// Runs a seed script once per connection, so a fresh stack comes up with data in it. Either one
    /// file for every connection, or a folder holding <c>{CONNECTION}.sql</c> per connection name.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="path">A <c>.sql</c> file or a folder of them, on your machine.</param>
    /// <remarks>
    /// For development stacks. A script runs once per content — editing it makes it run again,
    /// restarting does not — and never on a read-only connection or one marked as production
    /// (colour red).
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithSeedScript(
        this IResourceBuilder<WebDataStudioResource> builder, string path)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        builder.Resource.SeedScriptPath = path;

        // A file is mounted as a file, a folder as a folder: the studio takes either.
        var isFile = Path.GetExtension(path).Equals(".sql", StringComparison.OrdinalIgnoreCase);
        var target = isFile ? $"{SeedTarget}/seed.sql" : SeedTarget;

        return builder
            .WithBindMount(path, target, isReadOnly: true)
            .WithEnvironment("WDS_SEED_SQL", target);
    }
}
