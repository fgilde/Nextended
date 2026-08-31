using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.DbTools;

/// <summary>
/// Fills a MongoDB database with the contents of another one.
/// </summary>
/// <remarks>
/// <para>
/// <c>mongodump --archive | mongorestore --archive</c>: one stream, no temporary file, and the
/// archive carries the collections, their documents and their indexes.
/// </para>
/// <para>
/// A database here is a namespace rather than a schema, so "empty" means "has no collections", and
/// <see cref="DbCloneOptions.SchemaOnly"/> has nothing to mean — a collection *is* its documents.
/// Asking for it is refused rather than quietly answered with an empty database.
/// </para>
/// </remarks>
public static class MongoDBCloneExtensions
{
    private static CloneRecipe Recipe => new(
        MongoCloneRecipe.DefaultPort,
        MongoCloneRecipe.DefaultImage,
        MongoCloneRecipe.DefaultTag,
        MongoCloneRecipe.Script) { Label = "MongoDB", SupportsSchemaOnly = false };

    /// <summary>
    /// Clones another MongoDB database in this stack into this one.
    /// </summary>
    /// <param name="target">The database to fill.</param>
    /// <param name="source">The database to copy. Read only; nothing is written to it.</param>
    /// <param name="options">What to copy and what may be replaced. Null means everything, and only
    /// into a database with no collections in it.</param>
    public static IResourceBuilder<MongoDBDatabaseResource> WithCloneFrom(
        this IResourceBuilder<MongoDBDatabaseResource> target,
        IResourceBuilder<MongoDBDatabaseResource> source,
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
    /// <param name="target">The database to fill.</param>
    /// <param name="sourceConnectionString">Where to read from. Both the ADO.NET form and the URI
    /// form are understood.</param>
    /// <param name="options">What to copy and what may be replaced.</param>
    /// <remarks>
    /// A connection string with a password in it does not belong in a repository. The overloads that
    /// take a parameter or a connection-string resource are there for that.
    /// </remarks>
    public static IResourceBuilder<MongoDBDatabaseResource> WithCloneFrom(
        this IResourceBuilder<MongoDBDatabaseResource> target,
        string sourceConnectionString,
        DbCloneOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceConnectionString);

        return CloneBuilder.Attach(target, Endpoint(target.Resource),
            DbEndpoint.Parse(sourceConnectionString, MongoCloneRecipe.DefaultPort),
            null, Recipe, options);
    }

    /// <summary>
    /// The same, with the connection string in a parameter — which is where one carrying a password
    /// belongs.
    /// </summary>
    public static IResourceBuilder<MongoDBDatabaseResource> WithCloneFrom(
        this IResourceBuilder<MongoDBDatabaseResource> target,
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
    public static IResourceBuilder<MongoDBDatabaseResource> WithCloneFrom(
        this IResourceBuilder<MongoDBDatabaseResource> target,
        IResourceBuilder<IResourceWithConnectionString> source,
        DbCloneOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        return CloneBuilder.Attach(target, Endpoint(target.Resource),
            DbEndpoint.Whole(source.Resource.ConnectionStringExpression),
            null, Recipe, options);
    }

    /// Every part of a resource in this stack. Credentials are optional here: a development server
    /// often has none, and then the URI is built without them.
    internal static DbEndpoint Endpoint(MongoDBDatabaseResource database)
    {
        var server = database.Parent;

        return DbEndpoint.Parts(
            ReferenceExpression.Create($"{server.Host}"),
            ReferenceExpression.Create($"{server.Port}"),
            server.UserNameParameter is null
                ? DbEndpoint.Literal(null)
                : ReferenceExpression.Create($"{server.UserNameParameter}"),
            server.PasswordParameter is null
                ? DbEndpoint.Literal(null)
                : ReferenceExpression.Create($"{server.PasswordParameter}"),
            DbEndpoint.Literal(database.DatabaseName));
    }
}
