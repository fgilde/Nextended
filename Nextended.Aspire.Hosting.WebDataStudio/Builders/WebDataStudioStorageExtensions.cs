using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// Object storage as a connection: a bucket, a container or a folder, browsable in the studio's tree
/// and queryable through DuckDB — a Parquet file in a bucket is a table that happens to live
/// somewhere else.
/// </summary>
public static class WebDataStudioStorageExtensions
{
    /// <summary>
    /// Attaches object storage by its URL.
    /// <para>
    /// <c>s3://bucket/prefix?region=eu-central-1</c> for AWS and anything speaking S3 — MinIO,
    /// Cloudflare R2, Wasabi, Ceph, with <c>?endpoint=</c> for those;
    /// <c>azblob://account/container</c> for Azure Blob Storage; <c>gs://bucket</c> for Google Cloud
    /// Storage; <c>file:///data/incoming</c> for a folder the container can reach.
    /// </para>
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="connectionName">Label in the studio, e.g. <c>LAKE</c>.</param>
    /// <param name="url">The storage URL.</param>
    /// <param name="readOnly">Opens it read-only, which refuses every upload and delete.</param>
    /// <param name="group">Groups the connection in the explorer.</param>
    /// <param name="color">Tints the connection, e.g. <c>#e03131</c> for production — which also refuses writes.</param>
    /// <remarks>
    /// With no credentials in the URL the studio uses the identity it runs as: a managed identity on
    /// Azure, an instance role on AWS, application default credentials on Google. That is the form
    /// to prefer — a deployment carrying an access key for its own storage account is a deployment
    /// with a secret it did not need. Where keys are unavoidable, pass them as an Aspire parameter
    /// rather than writing them into the app host.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithStorage(
        this IResourceBuilder<WebDataStudioResource> builder,
        string connectionName,
        string url,
        bool readOnly = false,
        string? group = null,
        string? color = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        if (!IsStorageUrl(url))
            throw new ArgumentException(
                "a storage URL starts with s3://, azblob://, gs:// or file:// — " + url, nameof(url));

        return builder.WithConnection(connectionName, url, WebDataStudioEngine.Storage,
            readOnly, group, color);
    }

    /// <summary>
    /// Attaches a blob container the app host models — Azurite while developing, the real storage
    /// account once deployed — as a storage connection.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="blobs">The blob resource: <c>AddAzureStorage(…).AddBlobs(…)</c> or a blob container.</param>
    /// <param name="container">
    /// Which container to open. Defaults to the resource's own name, which is what
    /// <c>AddBlobContainer("exports")</c> means.
    /// </param>
    /// <param name="connectionName">Label in the studio (default: the resource's name, upper-cased).</param>
    /// <param name="prefix">Opens the connection at a prefix inside the container rather than at its root.</param>
    /// <param name="readOnly">Opens it read-only, which refuses every upload and delete.</param>
    /// <param name="group">Groups the connection in the explorer.</param>
    /// <param name="color">Tints the connection, e.g. <c>#e03131</c> for production — which also refuses writes.</param>
    /// <remarks>
    /// The resource's connection string travels as it is: a connection string for the emulator, and
    /// the blob service URI once deployed — where the studio then uses its own managed identity. The
    /// account name is inside either form, so the URL does not repeat it.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithBlobStorage<T>(
        this IResourceBuilder<WebDataStudioResource> builder,
        IResourceBuilder<T> blobs,
        string? container = null,
        string? connectionName = null,
        string? prefix = null,
        bool readOnly = false,
        string? group = null,
        string? color = null)
        where T : IResourceWithConnectionString
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(blobs);

        var name = container ?? blobs.Resource.Name;
        var path = string.IsNullOrWhiteSpace(prefix) ? name : $"{name}/{prefix!.Trim('/')}";

        // The connection string is only known once the stack runs, so the URL is a reference
        // expression rather than a string: azblob:///container?connectionstring=<the resource>.
        var url = ReferenceExpression.Create(
            $"azblob:///{path}?connectionstring={blobs.Resource.ConnectionStringExpression}");

        return builder.WithConnection(connectionName ?? blobs.Resource.Name, url,
            WebDataStudioEngine.Storage, readOnly, group, color);
    }

    private static bool IsStorageUrl(string url) =>
        url.StartsWith("s3://", StringComparison.OrdinalIgnoreCase)
        || url.StartsWith("azblob://", StringComparison.OrdinalIgnoreCase)
        || url.StartsWith("azure://", StringComparison.OrdinalIgnoreCase)
        || url.StartsWith("gs://", StringComparison.OrdinalIgnoreCase)
        || url.StartsWith("gcs://", StringComparison.OrdinalIgnoreCase)
        || url.StartsWith("file://", StringComparison.OrdinalIgnoreCase);
}
