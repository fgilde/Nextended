using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.DbTools;

/// <summary>
/// Fills a SQL Server database with the contents of another one.
/// </summary>
/// <remarks>
/// <para>
/// As a BACPAC, through <c>sqlpackage</c>: schema and data in one file, server to server over the
/// network, so a source outside this stack works exactly like one in it.
/// </para>
/// <para>
/// SQL Server is the one engine whose tools are not in its own image. <c>sqlcmd</c> and <c>bcp</c>
/// are, and neither carries a schema; <c>sqlpackage</c>, which does, is a .NET tool that is in no
/// image at all — so the clone runs in the .NET 8 SDK image and installs it, which means the first
/// clone needs the internet. An air-gapped stack points <see cref="DbCloneOptions.Image"/> at an
/// image that already has sqlpackage in it.
/// </para>
/// <para>
/// Importing into a database that already holds objects is refused by sqlpackage itself, which is
/// the "only when empty" rule enforced without anybody asking for it.
/// <see cref="DbCloneOptions.Overwrite"/> adds a second container that drops the database first,
/// because dropping it needs a client the clone's own image does not have.
/// </para>
/// </remarks>
public static class SqlServerCloneExtensions
{
    /// Which of the two ways in: sqlpackage, or the metadata a reader may read.
    private static CloneRecipe RecipeFor(DbCloneOptions? options) =>
        options?.FromMetadata == true
            ? new CloneRecipe(
                SqlServerMetadataCloneRecipe.DefaultPort,
                SqlServerMetadataCloneRecipe.DefaultImage,
                SqlServerMetadataCloneRecipe.DefaultTag,
                SqlServerMetadataCloneRecipe.Script)
            {
                Label = "SQL Server",
                SupportsFromMetadata = true,
                // The copier creates, drops and fills the database itself, so nothing has to be
                // cleared out of its way by a second container.
            }
            : Sqlpackage;

    private static CloneRecipe Sqlpackage => new(
        SqlServerCloneRecipe.DefaultPort,
        SqlServerCloneRecipe.DefaultImage,
        SqlServerCloneRecipe.DefaultTag,
        SqlServerCloneRecipe.Script)
    {
        Label = "SQL Server",
        // A BACPAC is schema and rows in one file and sqlpackage restores it whole, so there is no
        // data-only clone to be had here.
        SupportsDataOnly = false,
        Prepare = (SqlServerCloneRecipe.PrepareImage, SqlServerCloneRecipe.PrepareTag,
            SqlServerCloneRecipe.PrepareScript),
    };

    /// <summary>
    /// Clones another SQL Server database in this stack into this one.
    /// </summary>
    /// <param name="target">The database to fill.</param>
    /// <param name="source">The database to copy. Read only; nothing is written to it.</param>
    /// <param name="options">What to copy and what may be replaced. Null means everything, and only
    /// into a database with no objects in it.</param>
    public static IResourceBuilder<SqlServerDatabaseResource> WithCloneFrom(
        this IResourceBuilder<SqlServerDatabaseResource> target,
        IResourceBuilder<SqlServerDatabaseResource> source,
        DbCloneOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        return CloneBuilder.Attach(target, Endpoint(target.Resource), Endpoint(source.Resource),
            source.Resource, RecipeFor(options), (options ?? new()) with { WaitForSource = true });
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
    public static IResourceBuilder<SqlServerDatabaseResource> WithCloneFrom(
        this IResourceBuilder<SqlServerDatabaseResource> target,
        string sourceConnectionString,
        DbCloneOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceConnectionString);

        return CloneBuilder.Attach(target, Endpoint(target.Resource),
            DbEndpoint.Parse(sourceConnectionString, SqlServerCloneRecipe.DefaultPort),
            null, RecipeFor(options), options);
    }

    /// <summary>
    /// The same, with the connection string in a parameter — which is where one carrying a password
    /// belongs.
    /// </summary>
    public static IResourceBuilder<SqlServerDatabaseResource> WithCloneFrom(
        this IResourceBuilder<SqlServerDatabaseResource> target,
        IResourceBuilder<ParameterResource> sourceConnectionString,
        DbCloneOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(sourceConnectionString);

        // A parameter is one value and it is not known until the stack runs, so the script is handed
        // the whole string and takes it apart itself.
        return CloneBuilder.Attach(target, Endpoint(target.Resource),
            DbEndpoint.Whole(ReferenceExpression.Create($"{sourceConnectionString.Resource}")),
            null, RecipeFor(options), options);
    }

    /// <summary>
    /// The same, from a connection string this stack was given rather than built —
    /// <c>builder.AddConnectionString("staging")</c>, whose value comes from configuration, or any
    /// other resource that has one.
    /// </summary>
    public static IResourceBuilder<SqlServerDatabaseResource> WithCloneFrom(
        this IResourceBuilder<SqlServerDatabaseResource> target,
        IResourceBuilder<IResourceWithConnectionString> source,
        DbCloneOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        return CloneBuilder.Attach(target, Endpoint(target.Resource),
            DbEndpoint.Whole(source.Resource.ConnectionStringExpression),
            null, RecipeFor(options), options);
    }

    /// Every part of a resource in this stack. SQL Server's administrator is always `sa`; there is
    /// no user parameter to read.
    internal static DbEndpoint Endpoint(SqlServerDatabaseResource database)
    {
        var server = database.Parent;

        return DbEndpoint.Parts(
            ReferenceExpression.Create($"{server.Host}"),
            ReferenceExpression.Create($"{server.Port}"),
            DbEndpoint.Literal("sa"),
            ReferenceExpression.Create($"{server.PasswordParameter}"),
            DbEndpoint.Literal(database.DatabaseName));
    }
}
