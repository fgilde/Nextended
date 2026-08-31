using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.DbTools;

/// <summary>
/// Fills a MySQL or MariaDB database with the contents of another one.
/// </summary>
/// <remarks>
/// <para>
/// The whole database, through <c>mysqldump | mysql</c>: schema, rows, indexes, foreign keys,
/// triggers, views and the stored routines that mysqldump leaves out unless asked. The dump is read
/// in one transaction, so the source is neither locked nor caught half way through a change.
/// </para>
/// <para>
/// The target database is created if it is not there, which it often is not when the target is a
/// server this stack just started.
/// </para>
/// </remarks>
public static class MySqlCloneExtensions
{
    private static CloneRecipe Recipe => new(
        MySqlCloneRecipe.DefaultPort,
        MySqlCloneRecipe.DefaultImage,
        MySqlCloneRecipe.DefaultTag,
        MySqlCloneRecipe.Script) { Label = "MySQL" };

    /// <summary>
    /// Clones another MySQL or MariaDB database in this stack into this one.
    /// </summary>
    /// <param name="target">The database to fill.</param>
    /// <param name="source">The database to copy. Read only; nothing is written to it.</param>
    /// <param name="options">What to copy and what may be replaced. Null means everything, and only
    /// into a database with nothing in it.</param>
    public static IResourceBuilder<MySqlDatabaseResource> WithCloneFrom(
        this IResourceBuilder<MySqlDatabaseResource> target,
        IResourceBuilder<MySqlDatabaseResource> source,
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
    public static IResourceBuilder<MySqlDatabaseResource> WithCloneFrom(
        this IResourceBuilder<MySqlDatabaseResource> target,
        string sourceConnectionString,
        DbCloneOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceConnectionString);

        return CloneBuilder.Attach(target, Endpoint(target.Resource),
            DbEndpoint.Parse(sourceConnectionString, MySqlCloneRecipe.DefaultPort),
            null, Recipe, options);
    }

    /// <summary>
    /// The same, with the connection string in a parameter — which is where one carrying a password
    /// belongs.
    /// </summary>
    public static IResourceBuilder<MySqlDatabaseResource> WithCloneFrom(
        this IResourceBuilder<MySqlDatabaseResource> target,
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
    public static IResourceBuilder<MySqlDatabaseResource> WithCloneFrom(
        this IResourceBuilder<MySqlDatabaseResource> target,
        IResourceBuilder<IResourceWithConnectionString> source,
        DbCloneOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        return CloneBuilder.Attach(target, Endpoint(target.Resource),
            DbEndpoint.Whole(source.Resource.ConnectionStringExpression),
            null, Recipe, options);
    }

    /// Every part of a resource in this stack, separately: the host is a container name and the
    /// password a parameter, and neither is a string until the stack runs.
    internal static DbEndpoint Endpoint(MySqlDatabaseResource database)
    {
        var server = database.Parent;

        return DbEndpoint.Parts(
            ReferenceExpression.Create($"{server.Host}"),
            ReferenceExpression.Create($"{server.Port}"),
            // Aspire's MySQL runs as root; there is no user parameter to read.
            DbEndpoint.Literal("root"),
            ReferenceExpression.Create($"{server.PasswordParameter}"),
            DbEndpoint.Literal(database.DatabaseName));
    }
}
