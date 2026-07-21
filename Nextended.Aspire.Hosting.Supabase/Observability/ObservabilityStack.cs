using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Resources;

namespace Nextended.Aspire.Hosting.Observability;

/// <summary>
/// Supabase-aware entry point for the observability stack. The stack itself
/// (Grafana, Prometheus, Loki, Promtail, cAdvisor, Tempo, OTel-Collector,
/// postgres_exporter) lives in the <c>Nextended.Aspire.Hosting.Grafana</c>
/// package — see <see cref="ObservabilityStackExtensions"/> for the generic
/// options-based overload and
/// <see cref="Nextended.Aspire.Hosting.Grafana.GrafanaBuilderExtensions"/> for
/// the fluent piecemeal API.
/// </summary>
public static class ObservabilityStack
{
    /// <summary>
    /// Convenience overload — derives Postgres connection details from the
    /// supplied Supabase stack and lets the caller tweak everything else.
    /// </summary>
    public static IDistributedApplicationBuilder AddObservabilityStack(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<SupabaseStackResource> supabase,
        Action<ObservabilityStackOptions>? configure = null)
    {
        var dbPassword = supabase.Resource.Database?.Resource.Password
            ?? throw new InvalidOperationException("Supabase database password not configured");

        var defaultConfigRoot = Path.GetFullPath(
            Path.Combine(builder.AppHostDirectory, "..", "observability"));

        var options = new ObservabilityStackOptions
        {
            ConfigRootPath = defaultConfigRoot,
            PostgresExporter = new PostgresExporterOptions
            {
                // Use the actual Database container's resource name — keeps things
                // in lock-step if the Supabase library ever changes the suffix.
                Host = supabase.Resource.Database?.Resource.Name ?? $"{supabase.Resource.Name}-db",
                Password = dbPassword,
            },
        };
        configure?.Invoke(options);

        return builder.AddObservabilityStack(options);
    }
}
