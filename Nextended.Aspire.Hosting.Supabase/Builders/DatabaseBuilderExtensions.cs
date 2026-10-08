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
    /// Sets the PostgreSQL password. Every container reads it when its environment is evaluated,
    /// so this may come after the services that use it.
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

        stack.Database.WithPassword(password);
        return builder;
    }

    /// <summary>Sets the PostgreSQL password from an Aspire parameter (see <see cref="WithPassword(IResourceBuilder{SupabaseDatabaseResource}, IResourceBuilder{ParameterResource})"/>).</summary>
    public static IResourceBuilder<SupabaseStackResource> WithDatabasePassword(
        this IResourceBuilder<SupabaseStackResource> builder,
        IResourceBuilder<ParameterResource> password)
    {
        var stack = builder.Resource;
        if (stack.Database is null)
            throw new InvalidOperationException("Database not configured. Ensure AddSupabase() has been called.");

        stack.Database.WithPassword(password);
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

    public static IResourceBuilder<SupabaseStackResource> ConfigureDatabaseIf(
        this IResourceBuilder<SupabaseStackResource> builder,
        bool condition,
        Action<IResourceBuilder<SupabaseDatabaseResource>> configure) =>
        condition ? ConfigureDatabase(builder, configure) : builder;

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
        if (stack.UsesExternalDatabase)
            throw new InvalidOperationException(
                "ConfigureDatabase(...) is not valid when an external Postgres resource was passed to " +
                "AddSupabase(..., externalDatabase: ...). The Supabase stack does not own the database in " +
                "that mode — configure the image, password, data volume and deployment directly on your " +
                "own Postgres resource (e.g. builder.AddPostgres(\"mypg\").WithImage(\"supabase/postgres\", …)).");
        if (stack.Database is null)
            throw new InvalidOperationException("Database not configured. Ensure AddSupabase() has been called.");

        configure(stack.Database);
        return builder;
    }

    #endregion

    #region Sub-Resource Methods (for use with ConfigureDatabase)

    /// <summary>
    /// Sets the PostgreSQL password from an Aspire parameter, so the value lives in
    /// configuration (user secrets, a gitignored secrets.json, or <c>Parameters__…</c> in the
    /// environment) instead of in source.
    /// </summary>
    /// <remarks>
    /// The parameter stays a reference: deployed, the database and every service get it as a
    /// secret, the post-init SQL receives it at runtime, and the value appears in neither the
    /// manifest nor the bicep. Locally it resolves from configuration, also for the generated
    /// SQL files.
    /// </remarks>
    public static IResourceBuilder<SupabaseDatabaseResource> WithPassword(
        this IResourceBuilder<SupabaseDatabaseResource> builder,
        IResourceBuilder<ParameterResource> password)
    {
        ArgumentNullException.ThrowIfNull(password);
        builder.Resource.PasswordSecret.Set(password.Resource);
        LogInformation("Database password set from parameter");
        return builder;
    }

    /// <summary>
    /// Sets the PostgreSQL password. Every container reads it when its environment is evaluated,
    /// so this may come after the services that use it.
    /// </summary>
    public static IResourceBuilder<SupabaseDatabaseResource> WithPassword(
        this IResourceBuilder<SupabaseDatabaseResource> builder,
        string password)
    {
        builder.Resource.Password = password;
        LogInformation("Database password updated");
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
