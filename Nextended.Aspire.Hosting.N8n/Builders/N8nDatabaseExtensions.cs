using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.N8n.Resources;
using static Nextended.Aspire.Hosting.N8n.Helpers.N8nLogger;

namespace Nextended.Aspire.Hosting.N8n.Builders;

/// <summary>
/// Provides extension methods for configuring the n8n database backend.
/// </summary>
public static class N8nDatabaseExtensions
{
    /// <summary>
    /// Configures n8n to use an existing Aspire PostgreSQL database resource instead of the
    /// auto-created backend. The auto-created PostgreSQL container (if any) is removed.
    /// Call this before <c>WithQueueMode()</c>.
    /// </summary>
    /// <param name="builder">The n8n resource builder.</param>
    /// <param name="database">The PostgreSQL database resource n8n should connect to.</param>
    public static IResourceBuilder<N8nResource> WithDatabase(
        this IResourceBuilder<N8nResource> builder,
        IResourceBuilder<PostgresDatabaseResource> database)
    {
        ArgumentNullException.ThrowIfNull(database);

        RemoveAutoDatabase(builder);

        var resource = builder.Resource;
        resource.Database = database;
        resource.UsesSqlite = false;
        resource.OwnsDatabase = false;

        builder.WaitFor(database);
        LogInformation($"n8n '{resource.Name}' uses external database '{database.Resource.Name}'.");
        return builder;
    }

    /// <summary>
    /// Configures n8n to use a database on an existing Aspire PostgreSQL server resource.
    /// A logical database is added to the server and used as the n8n backend.
    /// </summary>
    /// <param name="builder">The n8n resource builder.</param>
    /// <param name="postgresServer">The PostgreSQL server resource.</param>
    /// <param name="databaseName">The name of the database to create/use. Default: "n8n".</param>
    public static IResourceBuilder<N8nResource> WithDatabase(
        this IResourceBuilder<N8nResource> builder,
        IResourceBuilder<PostgresServerResource> postgresServer,
        string databaseName = N8nBuilderExtensions.Defaults.DatabaseName)
    {
        ArgumentNullException.ThrowIfNull(postgresServer);
        var database = postgresServer.AddDatabase($"{builder.Resource.Name}-db", databaseName);
        return builder.WithDatabase(database);
    }

    /// <summary>
    /// Configures n8n to use the bundled SQLite database instead of PostgreSQL.
    /// The auto-created PostgreSQL container (if any) is removed. Data is persisted in the
    /// n8n data directory (bind mount locally, volume in publish mode).
    /// </summary>
    /// <param name="builder">The n8n resource builder.</param>
    public static IResourceBuilder<N8nResource> WithSqlite(
        this IResourceBuilder<N8nResource> builder)
    {
        RemoveAutoDatabase(builder);
        builder.Resource.UsesSqlite = true;
        LogInformation($"n8n '{builder.Resource.Name}' uses the bundled SQLite database.");
        return builder;
    }

    /// <summary>
    /// Removes the PostgreSQL backend that <c>AddN8n</c> created automatically, including the
    /// matching <see cref="WaitAnnotation"/> on the n8n resource. No-op for an external database.
    /// </summary>
    private static void RemoveAutoDatabase(IResourceBuilder<N8nResource> builder)
    {
        var resource = builder.Resource;
        if (!resource.OwnsDatabase)
            return;

        var app = builder.ApplicationBuilder;

        if (resource.Database is { } autoDb)
        {
            var staleWaits = builder.Resource.Annotations
                .OfType<WaitAnnotation>()
                .Where(w => w.Resource == autoDb.Resource)
                .Cast<IResourceAnnotation>()
                .ToList();
            foreach (var annotation in staleWaits)
                builder.Resource.Annotations.Remove(annotation);

            app.Resources.Remove(autoDb.Resource);
        }

        if (resource.PostgresServer is { } autoServer)
            app.Resources.Remove(autoServer.Resource);

        resource.Database = null;
        resource.PostgresServer = null;
        resource.OwnsDatabase = false;
    }
}
