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
