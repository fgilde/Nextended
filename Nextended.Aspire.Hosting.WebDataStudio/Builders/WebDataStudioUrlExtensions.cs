using System.Globalization;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>Where a connection the studio opened from its own URL is kept.</summary>
public enum UrlConnections
{
    /// <summary>
    /// For the browser that opened the link and nowhere else: nobody else sees it, nothing about it
    /// is written down, and it is gone when the studio restarts. The right answer when the link
    /// carried a password.
    /// </summary>
    Session,

    /// <summary>
    /// In the studio's connection store like any other connection: kept across restarts, visible to
    /// everybody who can open the studio. Right for a studio one person runs, wrong for a shared one.
    /// </summary>
    Store,
}

/// <summary>
/// A database that is a file rather than a server, and links that open one.
/// </summary>
/// <remarks>
/// Both halves are the same idea from two sides: the studio can open a SQLite file, a DuckDB file or
/// a folder of Parquet and CSV files, and a link can name one so that sending somebody a URL is
/// enough to show them a database.
/// </remarks>
public static class WebDataStudioUrlExtensions
{
    private const string RootsSetting = "WDS_FILE_ROOTS";
    private const string RootsBase = "/data/files";

    /// <summary>
    /// Mounts a folder of database files and lets the studio read files from it.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="path">A folder on your machine holding the files.</param>
    /// <param name="name">
    /// The folder's name inside the container, so a second call is a second root rather than a
    /// collision. <c>database-files</c> by default.
    /// </param>
    /// <remarks>
    /// What the studio opens is taken from the extension: <c>.db</c>, <c>.sqlite</c>,
    /// <c>.sqlite3</c>, <c>.db3</c> and <c>.s3db</c> as SQLite — the file has to start with
    /// <c>SQLite format 3</c>, because a <c>.db</c> is whatever somebody renamed — <c>.duckdb</c>
    /// and <c>.ddb</c> as DuckDB, and a <c>.parquet</c>, <c>.csv</c>, <c>.tsv</c>, <c>.ndjson</c>,
    /// <c>.jsonl</c>, <c>.json</c> or <c>.xlsx</c> as a storage connection over the folder it lies
    /// in, read-only whatever else is set.
    /// <para>
    /// These folders are what the <b>Browse the server</b> picker offers and the only paths a
    /// <c>?u=</c> file entry may name. Mounted read-only: the studio reads them, it does not own
    /// them. A file somebody uploads through the form lands in the studio's own data directory
    /// instead, which is why that one needs no root here.
    /// </para>
    /// </remarks>
    /// <exception cref="DirectoryNotFoundException">
    /// The folder is not there. A root that does not exist is not a root the studio would offer, and
    /// finding that out while the stack is being described beats an empty picker later.
    /// </exception>
    public static IResourceBuilder<WebDataStudioResource> WithDatabaseFiles(
        this IResourceBuilder<WebDataStudioResource> builder, string path, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var full = Path.GetFullPath(path);

        if (!Directory.Exists(full))
            throw new DirectoryNotFoundException(
                $"'{full}' is not a folder, so the studio would have no files to read there");

        var target = $"{RootsBase}/{(string.IsNullOrWhiteSpace(name) ? "database-files" : name.Trim())}";

        builder.WithBindMount(full, target, isReadOnly: true);

        // Comma-separated, and what an earlier call named keeps: two folders are two roots.
        var current = builder.Resource.PathSettings.TryGetValue(RootsSetting, out var existing)
            ? existing
            : [];

        if (!current.Contains(target, StringComparer.Ordinal))
        {
            current = [.. current, target];
            builder.Resource.PathSettings[RootsSetting] = current;
        }

        return builder.WithEnvironment(RootsSetting, string.Join(',', current));
    }

    /// <summary>
    /// Lets the studio's own URL open connections: <c>?u=/data/files/database-files/shop.sqlite3</c>.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="files">
    /// A path inside the folders the studio may read — those of <see cref="WithDatabaseFiles"/> plus
    /// its own data directory.
    /// </param>
    /// <param name="downloads">
    /// An <c>http(s)</c> URL the studio fetches once and then opens as a file. Needs
    /// <paramref name="hosts"/>.
    /// </param>
    /// <param name="connectionStrings">
    /// A whole connection string, credentials and all. Off unless it is asked for here.
    /// </param>
    /// <param name="hosts">
    /// The hosts a download may come from. <c>*.example.com</c> matches one level of subdomain.
    /// </param>
    /// <param name="keep">Where the connection is kept. Per browser by default.</param>
    /// <param name="writable">Whether a connection opened from a link may write. It may not by default.</param>
    /// <param name="maxMegabytes">The largest file a download may fetch. 512 by default.</param>
    /// <remarks>
    /// Off in the studio unless this is called, and each kind is its own parameter because each is
    /// its own risk. A file the container can already read is nothing new. A download is a fetcher
    /// inside your network, which is why <paramref name="hosts"/> is required rather than optional.
    /// And a connection string in a URL is a password in browser history, in proxy logs and in
    /// screenshots, so it is never on by accident.
    /// <para>
    /// A link may name several databases at once and label them:
    /// <c>?u=sales:/data/files/database-files/sales.duckdb,https://data.example/shop.sqlite3</c>.
    /// Each entry answers for itself, so the two that are allowed open and the third comes back with
    /// the setting that would have allowed it.
    /// </para>
    /// <example>
    /// <code>
    /// var studio = builder.AddWebDataStudio("studio")
    ///     .WithDatabaseFiles("./sample-databases")
    ///     .WithOpenFromUrl(downloads: true, hosts: ["data.example"]);
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Nothing is allowed, which is the same as not calling this — or a download was asked for
    /// without the hosts it may fetch from.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">The size cap is not a positive number of megabytes.</exception>
    public static IResourceBuilder<WebDataStudioResource> WithOpenFromUrl(
        this IResourceBuilder<WebDataStudioResource> builder,
        bool files = true, bool downloads = false, bool connectionStrings = false,
        IEnumerable<string>? hosts = null, UrlConnections keep = UrlConnections.Session,
        bool writable = false, int maxMegabytes = 512)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxMegabytes, 1);

        var kinds = new List<string>();
        if (files) kinds.Add("file");
        if (downloads) kinds.Add("download");
        if (connectionStrings) kinds.Add("connection-string");

        if (kinds.Count == 0)
            throw new ArgumentException(
                "WithOpenFromUrl with nothing allowed is the same as not calling it; allow files, "
                + "downloads or connection strings", nameof(files));

        var allowed = (hosts ?? []).Where(host => !string.IsNullOrWhiteSpace(host))
            .Select(host => host.Trim())
            .ToArray();

        if (downloads && allowed.Length == 0)
            throw new ArgumentException(
                "a download needs the hosts it may fetch from: without them the studio would fetch "
                + "whatever a link says, including addresses only this network can reach",
                nameof(hosts));

        return builder
            .WithEnvironment("WDS_OPEN_FROM_URL", string.Join(',', kinds))
            .WithEnvironment("WDS_OPEN_FROM_URL_HOSTS", string.Join(',', allowed))
            .WithEnvironment("WDS_OPEN_FROM_URL_KEEP",
                keep == UrlConnections.Store ? "store" : "session")
            .WithEnvironment("WDS_OPEN_FROM_URL_WRITABLE", writable ? "true" : "false")
            .WithEnvironment("WDS_OPEN_FROM_URL_MAX_MB",
                maxMegabytes.ToString(CultureInfo.InvariantCulture));
    }
}
