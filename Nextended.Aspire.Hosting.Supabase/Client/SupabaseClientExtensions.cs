using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nextended.Aspire.Hosting.Supabase.Client;

/// <summary>
/// Extension methods for adding Supabase client to service projects.
/// </summary>
public static class SupabaseClientExtensions
{
    /// <summary>
    /// Adds a Supabase client to the service collection.
    /// Automatically configures the client using connection strings or configuration.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="connectionName">
    /// The connection name to use for configuration lookup.
    /// Looks for "ConnectionStrings:{connectionName}" in configuration.
    /// </param>
    /// <param name="configureSettings">Optional callback to configure settings.</param>
    /// <returns>The host application builder for chaining.</returns>
    /// <example>
    /// <code>
    /// // In Program.cs of your service project:
    /// builder.AddSupabaseClient("supabase");
    /// 
    /// // Then inject in your services:
    /// public class MyService
    /// {
    ///     private readonly global::Supabase.Client _supabase;
    ///     
    ///     public MyService(global::Supabase.Client supabase)
    ///     {
    ///         _supabase = supabase;
    ///     }
    /// }
    /// </code>
    /// </example>
    public static IHostApplicationBuilder AddSupabaseClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<SupabaseClientSettings>? configureSettings = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        var settings = new SupabaseClientSettings();

        // 1. Try to load from connection string
        var connectionString = builder.Configuration.GetConnectionString(connectionName);
        settings.ParseConnectionString(connectionString);

        // 2. Try to load from configuration section (e.g., Supabase:Url, Supabase:Key)
        var configSection = builder.Configuration.GetSection(connectionName);
        if (configSection.Exists())
        {
            configSection.Bind(settings);
        }

        // 3. Allow explicit configuration override
        configureSettings?.Invoke(settings);

        // 4. Validate required settings
        ValidateSettings(settings, connectionName);

        // 5. Register Supabase client as singleton
        builder.Services.AddSingleton(sp =>
        {
            var options = new global::Supabase.SupabaseOptions
            {
                AutoConnectRealtime = settings.AutoConnectRealtime,
                AutoRefreshToken = settings.AutoRefreshToken
            };

            var client = new global::Supabase.Client(settings.Url!, settings.Key!, options);
            
            // Initialize the client asynchronously
            // Note: This blocks during startup, which is acceptable for Aspire service initialization
            client.InitializeAsync().GetAwaiter().GetResult();
            
            return client;
        });

        // 6. Add health check for Supabase connection
        builder.Services.AddHealthChecks()
            .AddCheck<SupabaseHealthCheck>(
                name: $"supabase_{connectionName}",
                tags: ["supabase", "ready"]);

        return builder;
    }

    private static void ValidateSettings(SupabaseClientSettings settings, string connectionName)
    {
        if (string.IsNullOrWhiteSpace(settings.Url))
        {
            throw new InvalidOperationException(
                $"Supabase URL is required. Configure it via ConnectionStrings:{connectionName} " +
                $"or {connectionName}:Url in appsettings.json. " +
                $"Example: \"ConnectionStrings:{connectionName}\": \"Url=http://localhost:8000;Key=eyJ...\"");
        }

        if (string.IsNullOrWhiteSpace(settings.Key))
        {
            throw new InvalidOperationException(
                $"Supabase API Key is required. Configure it via ConnectionStrings:{connectionName} " +
                $"or {connectionName}:Key in appsettings.json. " +
                $"Example: \"ConnectionStrings:{connectionName}\": \"Url=http://localhost:8000;Key=eyJ...\"");
        }

        // Basic validation of URL format
        if (!Uri.TryCreate(settings.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            throw new InvalidOperationException(
                $"Supabase URL must be a valid HTTP or HTTPS URL. Got: {settings.Url}");
        }
    }
}
