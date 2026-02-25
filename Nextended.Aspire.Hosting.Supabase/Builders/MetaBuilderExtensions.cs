using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Resources;

namespace Nextended.Aspire.Hosting.Supabase.Builders;

/// <summary>
/// Provides extension methods for configuring the Supabase Postgres-Meta service.
/// </summary>
public static class MetaBuilderExtensions
{
    #region Direct Stack Methods (Aspire-Standard Pattern)

    /// <summary>
    /// Sets the internal Postgres-Meta port.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="port">The port number.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> WithMetaPort(
        this IResourceBuilder<SupabaseStackResource> builder,
        int port)
    {
        var stack = builder.Resource;
        if (stack.Meta is null)
            throw new InvalidOperationException("Meta not configured. Ensure AddSupabase() has been called.");

        stack.Meta.Resource.Port = port;
        stack.Meta.WithEnvironment("PG_META_PORT", port.ToString());
        return builder;
    }

    #endregion

    #region Legacy ConfigureMeta (Obsolete)

    /// <summary>
    /// Configures the Postgres-Meta settings.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="configure">Configuration action for the Meta resource builder.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> ConfigureMeta(
        this IResourceBuilder<SupabaseStackResource> builder,
        Action<IResourceBuilder<SupabaseMetaResource>> configure)
    {
        var stack = builder.Resource;
        if (stack.Meta is null)
            throw new InvalidOperationException("Meta not configured. Ensure AddSupabase() has been called.");

        configure(stack.Meta);
        return builder;
    }

    #endregion

    #region Sub-Resource Methods (for use with ConfigureMeta)

    /// <summary>
    /// Sets the internal Postgres-Meta port.
    /// </summary>
    public static IResourceBuilder<SupabaseMetaResource> WithPort(
        this IResourceBuilder<SupabaseMetaResource> builder,
        int port)
    {
        builder.Resource.Port = port;
        builder.WithEnvironment("PG_META_PORT", port.ToString());
        return builder;
    }

    #endregion
}
