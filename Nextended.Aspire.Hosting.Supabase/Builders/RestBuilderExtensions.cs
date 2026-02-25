using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Resources;

namespace Nextended.Aspire.Hosting.Supabase.Builders;

/// <summary>
/// Provides extension methods for configuring the Supabase REST API (PostgREST).
/// </summary>
public static class RestBuilderExtensions
{
    #region Direct Stack Methods (Aspire-Standard Pattern)

    /// <summary>
    /// Sets the database schemas exposed by PostgREST.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="schemas">The schemas to expose.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> WithRestSchemas(
        this IResourceBuilder<SupabaseStackResource> builder,
        params string[] schemas)
    {
        var stack = builder.Resource;
        if (stack.Rest is null)
            throw new InvalidOperationException("Rest not configured. Ensure AddSupabase() has been called.");

        stack.Rest.Resource.Schemas = schemas;
        stack.Rest.WithEnvironment("PGRST_DB_SCHEMAS", string.Join(",", schemas));
        return builder;
    }

    /// <summary>
    /// Sets the anonymous role name for unauthenticated requests.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="role">The anonymous role name.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> WithRestAnonRole(
        this IResourceBuilder<SupabaseStackResource> builder,
        string role)
    {
        var stack = builder.Resource;
        if (stack.Rest is null)
            throw new InvalidOperationException("Rest not configured. Ensure AddSupabase() has been called.");

        stack.Rest.Resource.AnonRole = role;
        stack.Rest.WithEnvironment("PGRST_DB_ANON_ROLE", role);
        return builder;
    }

    #endregion

    #region Legacy ConfigureRest (Obsolete)

    /// <summary>
    /// Configures the PostgREST settings.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="configure">Configuration action for the REST resource builder.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> ConfigureRest(
        this IResourceBuilder<SupabaseStackResource> builder,
        Action<IResourceBuilder<SupabaseRestResource>> configure)
    {
        var stack = builder.Resource;
        if (stack.Rest is null)
            throw new InvalidOperationException("Rest not configured. Ensure AddSupabase() has been called.");

        configure(stack.Rest);
        return builder;
    }

    #endregion

    #region Sub-Resource Methods (for use with ConfigureRest)

    /// <summary>
    /// Sets the database schemas exposed by PostgREST.
    /// </summary>
    public static IResourceBuilder<SupabaseRestResource> WithSchemas(
        this IResourceBuilder<SupabaseRestResource> builder,
        params string[] schemas)
    {
        builder.Resource.Schemas = schemas;
        builder.WithEnvironment("PGRST_DB_SCHEMAS", string.Join(",", schemas));
        return builder;
    }

    /// <summary>
    /// Sets the anonymous role name for unauthenticated requests.
    /// </summary>
    public static IResourceBuilder<SupabaseRestResource> WithAnonRole(
        this IResourceBuilder<SupabaseRestResource> builder,
        string role)
    {
        builder.Resource.AnonRole = role;
        builder.WithEnvironment("PGRST_DB_ANON_ROLE", role);
        return builder;
    }

    #endregion
}
