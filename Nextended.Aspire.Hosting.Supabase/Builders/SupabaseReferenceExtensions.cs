using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Resources;

namespace Nextended.Aspire.Hosting.Supabase.Builders;

/// <summary>
/// Extension methods for referencing Supabase from client projects.
/// </summary>
public static class SupabaseReferenceExtensions
{
    /// <summary>
    /// Adds a Supabase connection reference to a project.
    /// This configures the project to connect to Supabase using the client SDK.
    /// </summary>
    /// <param name="builder">The project resource builder.</param>
    /// <param name="supabase">The Supabase stack resource.</param>
    /// <param name="connectionName">Optional connection name (defaults to "supabase").</param>
    /// <returns>The project resource builder for chaining.</returns>
    /// <example>
    /// <code>
    /// // In AppHost Program.cs:
    /// var supabase = builder.AddSupabase("supabase");
    /// 
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///     .WithSupabaseReference(supabase);
    /// 
    /// // Then in the API project Program.cs:
    /// builder.AddSupabaseClient("supabase");
    /// </code>
    /// </example>
    public static IResourceBuilder<TDestination> WithSupabaseReference<TDestination>(
        this IResourceBuilder<TDestination> builder,
        IResourceBuilder<SupabaseStackResource> supabase,
        string connectionName = "supabase")
        where TDestination : IResourceWithEnvironment
    {
        var stack = supabase.Resource;

        if (stack.Kong == null)
            throw new InvalidOperationException("Kong not configured. Ensure AddSupabase() has been called.");

        // Get the Kong endpoint (API Gateway)
        var kongEndpoint = stack.Kong.GetEndpoint("http");

        // Build connection string as environment variable expression
        // Format: Url=<endpoint>;Key=<anon_key>
        builder.WithEnvironment($"ConnectionStrings__{connectionName}__Url", kongEndpoint);
        builder.WithEnvironment($"ConnectionStrings__{connectionName}__Key", stack.AnonKey);

        // Also add reference for service discovery
        builder.WithReference(supabase);

        return builder;
    }

    /// <summary>
    /// Adds a Supabase connection reference with the service role key (for server-side operations).
    /// WARNING: Only use this for trusted backend services, not client applications!
    /// </summary>
    /// <param name="builder">The project resource builder.</param>
    /// <param name="supabase">The Supabase stack resource.</param>
    /// <param name="connectionName">Optional connection name (defaults to "supabase").</param>
    /// <returns>The project resource builder for chaining.</returns>
    public static IResourceBuilder<TDestination> WithSupabaseServiceRoleReference<TDestination>(
        this IResourceBuilder<TDestination> builder,
        IResourceBuilder<SupabaseStackResource> supabase,
        string connectionName = "supabase")
        where TDestination : IResourceWithEnvironment
    {
        var stack = supabase.Resource;

        if (stack.Kong == null)
            throw new InvalidOperationException("Kong not configured. Ensure AddSupabase() has been called.");

        // Get the Kong endpoint (API Gateway)
        var kongEndpoint = stack.Kong.GetEndpoint("http");

        // Build connection string with SERVICE ROLE key
        builder.WithEnvironment($"ConnectionStrings__{connectionName}__Url", kongEndpoint);
        builder.WithEnvironment($"ConnectionStrings__{connectionName}__Key", stack.ServiceRoleKey);

        // Also add reference for service discovery
        builder.WithReference(supabase);

        return builder;
    }
}
