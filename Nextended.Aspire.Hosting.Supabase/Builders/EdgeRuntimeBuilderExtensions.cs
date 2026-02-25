using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Resources;

namespace Nextended.Aspire.Hosting.Supabase.Builders;

/// <summary>
/// Provides extension methods for configuring the Supabase Edge Runtime.
/// </summary>
public static class EdgeRuntimeBuilderExtensions
{
    #region Direct Stack Methods (Aspire-Standard Pattern)

    /// <summary>
    /// Sets the internal Edge Runtime port.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="port">The port number.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> WithEdgeRuntimePort(
        this IResourceBuilder<SupabaseStackResource> builder,
        int port)
    {
        var stack = builder.Resource;
        if (stack.EdgeRuntime is null)
            throw new InvalidOperationException("EdgeRuntime not configured. Ensure WithEdgeFunctions() has been called.");

        stack.EdgeRuntime.Resource.Port = port;
        stack.EdgeRuntime.WithEnvironment("EDGE_RUNTIME_PORT", port.ToString());
        return builder;
    }

    /// <summary>
    /// Sets a custom environment variable for the Edge Runtime.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="name">The environment variable name.</param>
    /// <param name="value">The environment variable value.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> WithEdgeRuntimeEnvironment(
        this IResourceBuilder<SupabaseStackResource> builder,
        string name,
        string value)
    {
        var stack = builder.Resource;
        if (stack.EdgeRuntime is null)
            throw new InvalidOperationException("EdgeRuntime not configured. Ensure WithEdgeFunctions() has been called.");

        stack.EdgeRuntime.WithEnvironment(name, value);
        return builder;
    }

    #endregion

    #region Legacy ConfigureEdgeRuntime (Obsolete)

    /// <summary>
    /// Configures the Edge Runtime settings.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="configure">Configuration action for the Edge Runtime resource builder.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> ConfigureEdgeRuntime(
        this IResourceBuilder<SupabaseStackResource> builder,
        Action<IResourceBuilder<SupabaseEdgeRuntimeResource>> configure)
    {
        var stack = builder.Resource;
        if (stack.EdgeRuntime is null)
            throw new InvalidOperationException("EdgeRuntime not configured. Ensure WithEdgeFunctions() has been called.");

        configure(stack.EdgeRuntime);
        return builder;
    }

    #endregion

    #region Sub-Resource Methods (for use with ConfigureEdgeRuntime)

    /// <summary>
    /// Sets the internal Edge Runtime port.
    /// </summary>
    public static IResourceBuilder<SupabaseEdgeRuntimeResource> WithPort(
        this IResourceBuilder<SupabaseEdgeRuntimeResource> builder,
        int port)
    {
        builder.Resource.Port = port;
        builder.WithEnvironment("EDGE_RUNTIME_PORT", port.ToString());
        return builder;
    }

    /// <summary>
    /// Sets a custom environment variable for the Edge Runtime.
    /// </summary>
    public static IResourceBuilder<SupabaseEdgeRuntimeResource> WithCustomEnvironment(
        this IResourceBuilder<SupabaseEdgeRuntimeResource> builder,
        string name,
        string value)
    {
        builder.WithEnvironment(name, value);
        return builder;
    }

    #endregion
}
