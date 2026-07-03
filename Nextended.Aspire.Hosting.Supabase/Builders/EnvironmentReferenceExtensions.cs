using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Config;
using Nextended.Aspire.Hosting.Supabase.Resources;

namespace Nextended.Aspire.Hosting.Supabase.Builders;

public static class EnvironmentReferenceExtensions
{
    /// <summary>
    /// Configures Vite environment variables for Supabase connection.
    /// Uses dynamic endpoint reference that works both locally and in Azure.
    /// Besides the VITE_-prefixed client variables, the non-prefixed server-side variables
    /// (SUPABASE_URL, SUPABASE_SERVICE_ROLE_KEY, ...) are injected too, so SSR frameworks like
    /// TanStack Start (Lovable server functions via createServerFn reading process.env) work
    /// against the local stack. Vite only exposes VITE_-prefixed variables to the client bundle,
    /// so the service role key never leaks into the browser.
    /// </summary>
    public static IResourceBuilder<T> WithSupabaseVite<T>(this IResourceBuilder<T> builder, IResourceBuilder<SupabaseStackResource> supabase)
        where T : IResourceWithEnvironment
    {
        var stack = supabase.Resource;

        if (stack.Kong == null)
            throw new InvalidOperationException("Kong not configured. Ensure AddSupabase() has been called.");

        // Use EndpointReference for dynamic URL resolution (works in Azure Container Apps)
        var kongEndpoint = stack.Kong.GetEndpoint("http");

        builder
            .WithEnvironment("VITE_SUPABASE_URL", kongEndpoint)
            .WithEnvironment("VITE_SUPABASE_PUBLISHABLE_KEY", stack.AnonKey)
            // Server-side (SSR / TanStack Start server functions): non-prefixed variants.
            .WithEnvironment("SUPABASE_URL", kongEndpoint)
            .WithEnvironment("SUPABASE_ANON_KEY", stack.AnonKey)
            .WithEnvironment("SUPABASE_PUBLISHABLE_KEY", stack.AnonKey)
            .WithEnvironment("SUPABASE_SERVICE_ROLE_KEY", stack.ServiceRoleKey)
            .WithReference(supabase);

        if (!string.IsNullOrEmpty(stack.ProjectRefId))
        {
            builder
                .WithEnvironment("VITE_SUPABASE_PROJECT_ID", stack.ProjectRefId)
                .WithEnvironment("SUPABASE_PROJECT_ID", stack.ProjectRefId);
        }

        return builder;
    }

    /// <summary>
    /// Configures Vite environment variables for external Supabase (e.g., Supabase Cloud).
    /// </summary>
    public static IResourceBuilder<T> WithSupabaseVite<T>(this IResourceBuilder<T> builder, ISupabaseReferenceInfo supabase)
        where T : IResourceWithEnvironment
    {
         builder
            .WithEnvironment("VITE_SUPABASE_URL", supabase.GetApiUrl())
            .WithEnvironment("VITE_SUPABASE_PUBLISHABLE_KEY", supabase.ServiceKey)
            .WithEnvironment("SUPABASE_URL", supabase.GetApiUrl())
            .WithEnvironment("SUPABASE_PUBLISHABLE_KEY", supabase.ServiceKey);

         if (!string.IsNullOrEmpty(supabase.ProjectRefId))
         {
             builder
                 .WithEnvironment("VITE_SUPABASE_PROJECT_ID", supabase.ProjectRefId)
                 .WithEnvironment("SUPABASE_PROJECT_ID", supabase.ProjectRefId);
         }

         return builder;
    }
}