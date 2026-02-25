using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Helpers;
using Nextended.Aspire.Hosting.Supabase.Resources;
using static Nextended.Aspire.Hosting.Supabase.Helpers.SupabaseLogger;

namespace Nextended.Aspire.Hosting.Supabase.Builders;

/// <summary>
/// Provides extension methods for configuring the Supabase Database (PostgreSQL).
/// </summary>
public static class DatabaseBuilderExtensions
{
    private const int PostgresPort = 5432;

    #region Direct Stack Methods (Aspire-Standard Pattern)

    /// <summary>
    /// Sets the PostgreSQL password and updates all dependent containers.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="password">The database password.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> WithDatabasePassword(
        this IResourceBuilder<SupabaseStackResource> builder,
        string password)
    {
        var stack = builder.Resource;
        if (stack.Database is null)
            throw new InvalidOperationException("Database not configured. Ensure AddSupabase() has been called.");

        var resource = stack.Database.Resource;
        resource.Password = password;

        var containerPrefix = stack.Name;

        // Get database endpoint for dynamic service discovery (works in ACA and locally)
        var dbEndpoint = stack.Database.GetEndpoint("tcp");

        // Update environment variables on all containers that use the password
        stack.Database.WithEnvironment("POSTGRES_PASSWORD", password);

        // Auth container - update DB URL using dynamic endpoint resolution
        var authDbUrl = ReferenceExpression.Create(
            $"postgres://supabase_auth_admin:{password}@{dbEndpoint.Property(EndpointProperty.Host)}:{dbEndpoint.Property(EndpointProperty.Port)}/postgres?search_path=auth");
        stack.Auth?.WithEnvironment("GOTRUE_DB_DATABASE_URL", authDbUrl);

        // Rest container - update DB URI using dynamic endpoint resolution
        var restDbUri = ReferenceExpression.Create(
            $"postgres://authenticator:{password}@{dbEndpoint.Property(EndpointProperty.Host)}:{dbEndpoint.Property(EndpointProperty.Port)}/postgres");
        stack.Rest?.WithEnvironment("PGRST_DB_URI", restDbUri);

        // Storage container - update DB URL using dynamic endpoint resolution
        var storageDatabaseUrl = ReferenceExpression.Create(
            $"postgres://supabase_storage_admin:{password}@{dbEndpoint.Property(EndpointProperty.Host)}:{dbEndpoint.Property(EndpointProperty.Port)}/postgres");
        stack.Storage?.WithEnvironment("DATABASE_URL", storageDatabaseUrl);

        // Meta container - update password
        stack.Meta?.WithEnvironment("PG_META_DB_PASSWORD", password);

        // Studio container (which is the stack itself) - update password
        stack.StackBuilder?.WithEnvironment("POSTGRES_PASSWORD", password);

        // Init container (for publish mode) - update password via environment variable
        stack.InitContainer?.WithEnvironment("DB_PASSWORD", password);

        // Re-generate SQL files with the new password
        if (!string.IsNullOrEmpty(stack.InitSqlPath))
        {
            // Update 00_init.sql with new password
            SupabaseSqlGenerator.WriteInitSql(stack.InitSqlPath, password);

            // Update post_init.sh with new password (only for local development)
            var scriptsDir = Path.Combine(Path.GetDirectoryName(stack.InitSqlPath)!, "scripts");
            var postInitShPath = Path.Combine(scriptsDir, "post_init.sh");
            if (Directory.Exists(scriptsDir))
            {
                // Note: This script uses hardcoded hostname for local development only
                // In ACA, the init container uses environment variables for dynamic resolution
                SupabaseSqlGenerator.WritePostInitScript(postInitShPath, $"{containerPrefix}-db", password);
            }

            LogInformation("Database password updated in all containers and SQL files");
        }

        return builder;
    }

    /// <summary>
    /// Sets the external PostgreSQL port.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="port">The external port number.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> WithDatabasePort(
        this IResourceBuilder<SupabaseStackResource> builder,
        int port)
    {
        var stack = builder.Resource;
        if (stack.Database is null)
            throw new InvalidOperationException("Database not configured. Ensure AddSupabase() has been called.");

        stack.Database.Resource.ExternalPort = port;
        return builder;
    }

    #endregion

    #region Legacy ConfigureDatabase (Obsolete)

    /// <summary>
    /// Configures the PostgreSQL database settings.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="configure">Configuration action for the database resource builder.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> ConfigureDatabase(
        this IResourceBuilder<SupabaseStackResource> builder,
        Action<IResourceBuilder<SupabaseDatabaseResource>> configure)
    {
        var stack = builder.Resource;
        if (stack.Database is null)
            throw new InvalidOperationException("Database not configured. Ensure AddSupabase() has been called.");

        configure(stack.Database);
        return builder;
    }

    #endregion

    #region Sub-Resource Methods (for use with ConfigureDatabase)

    /// <summary>
    /// Sets the PostgreSQL password and updates all dependent containers.
    /// </summary>
    public static IResourceBuilder<SupabaseDatabaseResource> WithPassword(
        this IResourceBuilder<SupabaseDatabaseResource> builder,
        string password)
    {
        var resource = builder.Resource;
        resource.Password = password;

        var stack = resource.Stack;
        if (stack is null)
            throw new InvalidOperationException("Stack not configured on database resource.");

        var containerPrefix = stack.Name;

        // Get database endpoint for dynamic service discovery (works in ACA and locally)
        var dbEndpoint = stack.Database!.GetEndpoint("tcp");

        // Update environment variables on all containers that use the password
        builder.WithEnvironment("POSTGRES_PASSWORD", password);

        // Auth container - update DB URL using dynamic endpoint resolution
        var authDbUrl = ReferenceExpression.Create(
            $"postgres://supabase_auth_admin:{password}@{dbEndpoint.Property(EndpointProperty.Host)}:{dbEndpoint.Property(EndpointProperty.Port)}/postgres?search_path=auth");
        stack.Auth?.WithEnvironment("GOTRUE_DB_DATABASE_URL", authDbUrl);

        // Rest container - update DB URI using dynamic endpoint resolution
        var restDbUri = ReferenceExpression.Create(
            $"postgres://authenticator:{password}@{dbEndpoint.Property(EndpointProperty.Host)}:{dbEndpoint.Property(EndpointProperty.Port)}/postgres");
        stack.Rest?.WithEnvironment("PGRST_DB_URI", restDbUri);

        // Storage container - update DB URL using dynamic endpoint resolution
        var storageDatabaseUrl = ReferenceExpression.Create(
            $"postgres://supabase_storage_admin:{password}@{dbEndpoint.Property(EndpointProperty.Host)}:{dbEndpoint.Property(EndpointProperty.Port)}/postgres");
        stack.Storage?.WithEnvironment("DATABASE_URL", storageDatabaseUrl);

        // Meta container - update password
        stack.Meta?.WithEnvironment("PG_META_DB_PASSWORD", password);

        // Studio container (which is the stack itself) - update password
        stack.StackBuilder?.WithEnvironment("POSTGRES_PASSWORD", password);

        // Init container (for publish mode) - update password via environment variable
        stack.InitContainer?.WithEnvironment("DB_PASSWORD", password);

        // Re-generate SQL files with the new password
        if (!string.IsNullOrEmpty(stack.InitSqlPath))
        {
            // Update 00_init.sql with new password
            SupabaseSqlGenerator.WriteInitSql(stack.InitSqlPath, password);

            // Update post_init.sh with new password (only for local development)
            var scriptsDir = Path.Combine(Path.GetDirectoryName(stack.InitSqlPath)!, "scripts");
            var postInitShPath = Path.Combine(scriptsDir, "post_init.sh");
            if (Directory.Exists(scriptsDir))
            {
                // Note: This script uses hardcoded hostname for local development only
                // In ACA, the init container uses environment variables for dynamic resolution
                SupabaseSqlGenerator.WritePostInitScript(postInitShPath, $"{containerPrefix}-db", password);
            }

            LogInformation("Database password updated in all containers and SQL files");
        }

        return builder;
    }

    /// <summary>
    /// Sets the external PostgreSQL port.
    /// </summary>
    public static IResourceBuilder<SupabaseDatabaseResource> WithPort(
        this IResourceBuilder<SupabaseDatabaseResource> builder,
        int port)
    {
        builder.Resource.ExternalPort = port;
        return builder;
    }

    #endregion
}
