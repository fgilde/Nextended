using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Config;
using Nextended.Aspire.Hosting.Supabase.Resources;

namespace Nextended.Aspire.Hosting.Supabase.Builders;

public static class EnvironmentReferenceExtensions
{

    public static IResourceBuilder<T> WithSupabaseVite<T>(this IResourceBuilder<T> builder, IResourceBuilder<SupabaseStackResource> supabase)
        where T : IResourceWithEnvironment
    {
        return builder
            .WithSupabaseVite(supabase.Resource)
            .WithReference(supabase);
    }

    public static IResourceBuilder<T> WithSupabaseVite<T>(this IResourceBuilder<T> builder, ISupabaseReferenceInfo supabase)
        where T : IResourceWithEnvironment
    {
         builder
            .WithEnvironment("VITE_SUPABASE_URL", supabase.GetApiUrl())
            .WithEnvironment("VITE_SUPABASE_PUBLISHABLE_KEY", supabase.ServiceKey);

         if (!string.IsNullOrEmpty(supabase.ProjectRefId))
         {
             builder.WithEnvironment("VITE_SUPABASE_PROJECT_ID", supabase.ProjectRefId);
         }

         return builder;
    }
}