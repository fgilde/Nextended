using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Helpers;
using Nextended.Aspire.Hosting.Supabase.Resources;

namespace Nextended.Aspire.Hosting.Supabase.Builders;

/// <summary>
/// Provides extension methods for configuring the Supabase Kong API Gateway.
/// </summary>
public static class KongBuilderExtensions
{
    #region Direct Stack Methods (Aspire-Standard Pattern)

    /// <summary>
    /// Sets the external Kong port.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="port">The external port number.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> WithKongPort(
        this IResourceBuilder<SupabaseStackResource> builder,
        int port)
    {
        var stack = builder.Resource;
        if (stack.Kong is null)
            throw new InvalidOperationException("Kong not configured. Ensure AddSupabase() has been called.");

        stack.Kong.Resource.ExternalPort = port;
        return builder;
    }

    /// <summary>
    /// Sets the Kong plugins to enable.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="plugins">The plugins to enable.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> WithKongPlugins(
        this IResourceBuilder<SupabaseStackResource> builder,
        params string[] plugins)
    {
        var stack = builder.Resource;
        if (stack.Kong is null)
            throw new InvalidOperationException("Kong not configured. Ensure AddSupabase() has been called.");

        stack.Kong.Resource.Plugins = plugins;
        stack.Kong.WithEnvironment("KONG_PLUGINS", string.Join(",", plugins));
        return builder;
    }

    /// <summary>
    /// Enables Kong's built-in <c>opentelemetry</c> plugin as a global plugin in
    /// the declarative Kong YAML. Every API request through the gateway will emit
    /// an OTLP span to the supplied endpoint — no per-route config, no
    /// instrumentation in downstream services required.
    ///
    /// This rewrites the Kong YAML on disk (the file Kong bind-mounts as its
    /// config) and adds <c>opentelemetry</c> to the <c>KONG_PLUGINS</c> env var,
    /// so it works both for the local-dev mode and the publish-mode template.
    /// Call this AFTER <c>AddSupabase(...)</c> but BEFORE the app is built.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="tracesEndpoint">Full OTLP/HTTP traces URL, e.g.
    /// <c>http://monitoring-otel-collector:4318/v1/traces</c>.</param>
    /// <param name="samplingRate">0.0–1.0. Default 1.0 (every request — fine for dev,
    /// dial down for prod).</param>
    /// <param name="serviceName">Value of the <c>service.name</c> resource attribute in
    /// emitted spans. When <c>null</c> (default), derived from the Kong resource name
    /// (e.g. <c>adam-supabase-kong</c>) so spans match the actual container in dashboards.</param>
    /// <param name="headerType">Trace-propagation header format. Default <c>w3c</c>.</param>
    public static IResourceBuilder<SupabaseStackResource> WithKongOpenTelemetry(
        this IResourceBuilder<SupabaseStackResource> builder,
        string tracesEndpoint = "http://monitoring-otel-collector:4318/v1/traces",
        double samplingRate = 1.0,
        string? serviceName = null,
        string headerType = "w3c")
    {
        var stack = builder.Resource;
        if (stack.Kong is null)
            throw new InvalidOperationException("Kong not configured. Ensure AddSupabase() has been called.");
        if (string.IsNullOrEmpty(stack.InfraRootDir))
            throw new InvalidOperationException("Supabase InfraRootDir not set — AddSupabase didn't complete.");

        // Default the OTel service.name to the actual Kong container's resource
        // name so spans match what users see in the Aspire dashboard. Saves the
        // caller from having to know what string Kong got registered as.
        var resolvedServiceName = serviceName ?? stack.Kong.Resource.Name;

        var config = new SupabaseSqlGenerator.KongTracingConfig(
            TracesEndpoint: tracesEndpoint,
            SamplingRate: samplingRate,
            ServiceName: resolvedServiceName,
            HeaderType: headerType);

        // Remember the config — needed for publish-mode template regeneration on
        // a future `azd up` run, and so other code can read it if needed.
        stack.KongTracing = config;

        // Re-write the local-dev kong.yml in place. AddSupabase already wrote it
        // (without the OTel block); we replace its content here. The Kong
        // container reads the bind-mounted file when it starts, so as long as
        // we're called before .Build()/.Run(), Kong picks up the new YAML.
        //
        // We don't have access to the service URLs the original write used, but
        // we don't need them — we re-generate from the same container-name
        // convention SupabaseBuilderExtensions uses internally.
        var configDir = Path.Combine(stack.InfraRootDir, "config");
        var kongYmlPath = Path.Combine(configDir, "kong.yml");
        if (!File.Exists(kongYmlPath))
            throw new InvalidOperationException($"Kong config not found at {kongYmlPath}");

        // Reuse the same container-prefix that AddSupabase used (the stack name).
        var prefix = stack.Name;
        SupabaseSqlGenerator.WriteKongConfig(
            kongYmlPath,
            stack.AnonKey,
            stack.ServiceRoleKey,
            prefix,
            goTruePort: 9999,       // values mirrored from SupabaseBuilderExtensions
            postRestPort: 3000,     // .Ports constants. If those change, update here.
            storagePort: 5000,
            metaPort: 8080,
            edgeRuntimePort: 9000,
            realtimePort: 4000,
            tracing: config);

        // Also refresh the publish-mode template if it was already cached on
        // the stack — keeps `azd up` honest.
        if (stack.KongConfigBase64 is not null)
        {
            var publishTemplate = SupabaseSqlGenerator.GetKongConfigTemplateForPublish(
                stack.AnonKey, stack.ServiceRoleKey, tracing: config);
            stack.KongConfigBase64 = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(publishTemplate));
        }

        // Make sure the opentelemetry plugin is in Kong's allowed-plugins list.
        // Without this, Kong refuses to load any plugin not in KONG_PLUGINS.
        var plugins = stack.Kong.Resource.Plugins;
        if (!plugins.Contains("opentelemetry"))
        {
            var next = plugins.Concat(["opentelemetry"]).ToArray();
            stack.Kong.Resource.Plugins = next;
            stack.Kong.WithEnvironment("KONG_PLUGINS", string.Join(",", next));
        }

        // Crank Kong's log level so the OTel plugin's internal activity surfaces
        // in the container logs (default `notice` hides plugin-level info).
        stack.Kong.WithEnvironment("KONG_LOG_LEVEL", "info");

        stack.Kong.WithEnvironment("KONG_TRACING_INSTRUMENTATIONS", "request");
        stack.Kong.WithEnvironment("KONG_TRACING_SAMPLING_RATE",
            samplingRate.ToString("0.0##", System.Globalization.CultureInfo.InvariantCulture));

        return builder;
    }

    #endregion

    #region Legacy ConfigureKong (Obsolete)

    /// <summary>
    /// Configures the Kong API Gateway settings.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="configure">Configuration action for the Kong resource builder.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> ConfigureKong(
        this IResourceBuilder<SupabaseStackResource> builder,
        Action<IResourceBuilder<SupabaseKongResource>> configure)
    {
        var stack = builder.Resource;
        if (stack.Kong is null)
            throw new InvalidOperationException("Kong not configured. Ensure AddSupabase() has been called.");

        configure(stack.Kong);
        return builder;
    }

    #endregion

    #region Sub-Resource Methods (for use with ConfigureKong)

    /// <summary>
    /// Sets the external Kong port.
    /// </summary>
    public static IResourceBuilder<SupabaseKongResource> WithPort(
        this IResourceBuilder<SupabaseKongResource> builder,
        int port)
    {
        builder.Resource.ExternalPort = port;
        return builder;
    }

    /// <summary>
    /// Sets the Kong plugins to enable.
    /// </summary>
    public static IResourceBuilder<SupabaseKongResource> WithPlugins(
        this IResourceBuilder<SupabaseKongResource> builder,
        params string[] plugins)
    {
        builder.Resource.Plugins = plugins;
        builder.WithEnvironment("KONG_PLUGINS", string.Join(",", plugins));
        return builder;
    }

    #endregion
}
