using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.N8n.Resources;

namespace Nextended.Aspire.Hosting.N8n.Builders;

/// <summary>
/// Provides accessor extension methods for an <see cref="N8nResource"/>.
/// </summary>
public static class N8nGetterExtensions
{
    /// <summary>Gets the PostgreSQL database resource n8n connects to (null when using SQLite).</summary>
    public static IResourceBuilder<PostgresDatabaseResource>? GetDatabase(this IResourceBuilder<N8nResource> builder)
        => builder.Resource.Database;

    /// <summary>Gets the Redis container resource used for queue mode (null when queue mode is disabled).</summary>
    public static IResourceBuilder<ContainerResource>? GetRedis(this IResourceBuilder<N8nResource> builder)
        => builder.Resource.Redis;

    /// <summary>Gets the configured n8n worker resources (queue mode).</summary>
    public static IReadOnlyList<IResourceBuilder<N8nWorkerResource>> GetWorkers(this IResourceBuilder<N8nResource> builder)
        => builder.Resource.Workers;

    /// <summary>Gets the reference to the primary HTTP endpoint of the n8n editor/REST API.</summary>
    public static EndpointReference GetHttpEndpoint(this IResourceBuilder<N8nResource> builder)
        => builder.Resource.HttpEndpoint;

    /// <summary>Gets the n8n encryption key.</summary>
    public static string GetEncryptionKey(this IResourceBuilder<N8nResource> builder)
        => builder.Resource.EncryptionKey;
}
