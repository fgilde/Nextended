using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.DbTools;

/// <summary>
/// Fills a Redis server with the contents of another one.
/// </summary>
/// <remarks>
/// <para>
/// By letting Redis do it: the target is pointed at the source as a replica, and cut loose again
/// once the sync has finished. Every key of every type arrives with its TTL, in the binary form
/// Redis itself uses.
/// </para>
/// <para>
/// A clone replaces the whole server — Redis has no database to scope this to, and a replica is not
/// a merge. The target is left as a master whether the sync finished or not: one left as somebody's
/// replica would be read-only for ever, which is worse than a clone that did not complete.
/// </para>
/// </remarks>
public static class RedisCloneExtensions
{
    private static CloneRecipe Recipe => new(
        RedisCloneRecipe.DefaultPort,
        RedisCloneRecipe.DefaultImage,
        RedisCloneRecipe.DefaultTag,
        RedisCloneRecipe.Script) { Label = "Redis", SupportsSchemaOnly = false };

    /// <summary>
    /// Clones another Redis server in this stack into this one.
    /// </summary>
    /// <param name="target">The server to fill.</param>
    /// <param name="source">The server to copy. Read only; nothing is written to it.</param>
    /// <param name="options">What to copy and what may be replaced. Null means everything, and only
    /// into a server with no keys in it.</param>
    public static IResourceBuilder<RedisResource> WithCloneFrom(
        this IResourceBuilder<RedisResource> target,
        IResourceBuilder<RedisResource> source,
        DbCloneOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        return CloneBuilder.Attach(target, Endpoint(target.Resource), Endpoint(source.Resource),
            source.Resource, Recipe, (options ?? new()) with { WaitForSource = true });
    }

    /// <summary>
    /// Clones from a connection string — a staging server, last night's restore, anything this stack
    /// does not model.
    /// </summary>
    /// <param name="target">The server to fill.</param>
    /// <param name="sourceConnectionString">Where to read from. Both the ADO.NET form and the URI
    /// form are understood.</param>
    /// <param name="options">What to copy and what may be replaced.</param>
    /// <remarks>
    /// A connection string with a password in it does not belong in a repository. The overloads that
    /// take a parameter or a connection-string resource are there for that.
    /// </remarks>
    public static IResourceBuilder<RedisResource> WithCloneFrom(
        this IResourceBuilder<RedisResource> target,
        string sourceConnectionString,
        DbCloneOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceConnectionString);

        return CloneBuilder.Attach(target, Endpoint(target.Resource),
            DbEndpoint.Parse(sourceConnectionString, RedisCloneRecipe.DefaultPort),
            null, Recipe, options);
    }

    /// <summary>
    /// The same, with the connection string in a parameter — which is where one carrying a password
    /// belongs.
    /// </summary>
    public static IResourceBuilder<RedisResource> WithCloneFrom(
        this IResourceBuilder<RedisResource> target,
        IResourceBuilder<ParameterResource> sourceConnectionString,
        DbCloneOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(sourceConnectionString);

        // A parameter is one value and it is not known until the stack runs, so the script is handed
        // the whole string and takes it apart itself.
        return CloneBuilder.Attach(target, Endpoint(target.Resource),
            DbEndpoint.Whole(ReferenceExpression.Create($"{sourceConnectionString.Resource}")),
            null, Recipe, options);
    }

    /// <summary>
    /// The same, from a connection string this stack was given rather than built —
    /// <c>builder.AddConnectionString("staging")</c>, whose value comes from configuration, or any
    /// other resource that has one.
    /// </summary>
    public static IResourceBuilder<RedisResource> WithCloneFrom(
        this IResourceBuilder<RedisResource> target,
        IResourceBuilder<IResourceWithConnectionString> source,
        DbCloneOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        return CloneBuilder.Attach(target, Endpoint(target.Resource),
            DbEndpoint.Whole(source.Resource.ConnectionStringExpression),
            null, Recipe, options);
    }

    /// A Redis server has no database to name, and a password only where one was asked for.
    internal static DbEndpoint Endpoint(RedisResource redis) =>
        DbEndpoint.Parts(
            ReferenceExpression.Create($"{redis.Host}"),
            ReferenceExpression.Create($"{redis.Port}"),
            DbEndpoint.Literal(null),
            redis.PasswordParameter is null
                ? DbEndpoint.Literal(null)
                : ReferenceExpression.Create($"{redis.PasswordParameter}"),
            DbEndpoint.Literal(null));
}
