using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.DbTools;

/// <summary>
/// Fills a PostgreSQL database with the contents of another one.
/// </summary>
/// <remarks>
/// <para>
/// The whole database: schema, rows, indexes, constraints, views, functions, sequences and where
/// they are counted to. It runs <c>pg_dump | psql</c> in a container of its own, so it happens
/// wherever the stack runs — on a laptop with <c>dotnet run</c>, and in whatever a published stack
/// is deployed onto.
/// </para>
/// <para>
/// One version rule: <c>pg_dump</c> refuses a server newer than itself. The clone runs in
/// <c>postgres:17-alpine</c> unless <see cref="DbCloneOptions.Image"/> says otherwise, so a source
/// newer than that needs saying.
/// </para>
/// </remarks>
public static class PostgresCloneExtensions
{
    private static CloneRecipe Recipe => new(
        PostgresCloneRecipe.DefaultPort,
        PostgresCloneRecipe.DefaultImage,
        PostgresCloneRecipe.DefaultTag,
        PostgresCloneRecipe.Script) { Label = "PostgreSQL" };

    /// <summary>
    /// Clones another PostgreSQL database in this stack into this one.
    /// </summary>
    /// <param name="target">The database to fill.</param>
    /// <param name="source">The database to copy. Read only; nothing is written to it.</param>
    /// <param name="options">What to copy and what may be replaced. Null means everything, and only
    /// into a database with nothing in it.</param>
    public static IResourceBuilder<PostgresDatabaseResource> WithCloneFrom(
        this IResourceBuilder<PostgresDatabaseResource> target,
        IResourceBuilder<PostgresDatabaseResource> source,
        DbCloneOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        return CloneBuilder.Attach(target, Endpoint(target.Resource), Endpoint(source.Resource),
            source.Resource, Recipe, (options ?? new()) with { WaitForSource = true });
    }

    /// <summary>
    /// Clones a database from a connection string — a staging server, last night's restore, anything
    /// this stack does not model.
    /// </summary>
    /// <param name="target">The database to fill.</param>
    /// <param name="sourceConnectionString">Where to read from. Both the ADO.NET form
    /// (<c>Host=…;Database=…;Username=…;Password=…</c>) and the URI form
    /// (<c>postgres://user:pw@host:5432/db</c>) are understood.</param>
    /// <param name="options">What to copy and what may be replaced.</param>
    /// <remarks>
    /// A connection string with a password in it does not belong in a repository. The overloads that
    /// take a parameter or a connection-string resource are there for that.
    /// </remarks>
    public static IResourceBuilder<PostgresDatabaseResource> WithCloneFrom(
        this IResourceBuilder<PostgresDatabaseResource> target,
        string sourceConnectionString,
        DbCloneOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceConnectionString);

        return CloneBuilder.Attach(target, Endpoint(target.Resource),
            DbEndpoint.Parse(sourceConnectionString, PostgresCloneRecipe.DefaultPort),
            null, Recipe, options);
    }

    /// <summary>
    /// The same, with the connection string in a parameter — which is where one carrying a password
    /// belongs.
    /// </summary>
    public static IResourceBuilder<PostgresDatabaseResource> WithCloneFrom(
        this IResourceBuilder<PostgresDatabaseResource> target,
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
    public static IResourceBuilder<PostgresDatabaseResource> WithCloneFrom(
        this IResourceBuilder<PostgresDatabaseResource> target,
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
    internal static DbEndpoint Endpoint(PostgresDatabaseResource database)
    {
        var server = database.Parent;

        return DbEndpoint.Parts(
            ReferenceExpression.Create($"{server.Host}"),
            ReferenceExpression.Create($"{server.Port}"),
            ReferenceExpression.Create($"{server.UserNameReference}"),
            ReferenceExpression.Create($"{server.PasswordParameter}"),
            DbEndpoint.Literal(database.DatabaseName));
    }
}
