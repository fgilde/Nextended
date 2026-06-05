using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nextended.Aspire.Hosting.N8n.Client;

/// <summary>
/// Extension methods for adding an n8n REST API client to service projects.
/// </summary>
public static class N8nClientExtensions
{
    /// <summary>
    /// Registers a configured <see cref="N8nApiClient"/> (and a health check) in the service collection.
    /// The base URL is resolved from the connection string of a referenced n8n resource
    /// (<c>ConnectionStrings:{connectionName}</c>) or from a configuration section.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="connectionName">
    /// The connection name used for configuration lookup
    /// (<c>ConnectionStrings:{connectionName}</c> and the <c>{connectionName}</c> section).
    /// </param>
    /// <param name="configureSettings">Optional callback to configure or override settings (e.g. the API key).</param>
    /// <returns>The host application builder for chaining.</returns>
    /// <example>
    /// <code>
    /// // In Program.cs of your service project:
    /// builder.AddN8nClient("n8n", s => s.ApiKey = builder.Configuration["N8n:ApiKey"]);
    ///
    /// // Then inject it:
    /// public sealed class MyService(N8nApiClient n8n) { /* n8n.Http ... */ }
    /// </code>
    /// </example>
    public static IHostApplicationBuilder AddN8nClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<N8nClientSettings>? configureSettings = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        var settings = new N8nClientSettings();

        settings.ParseConnectionString(builder.Configuration.GetConnectionString(connectionName));

        var configSection = builder.Configuration.GetSection(connectionName);
        if (configSection.Exists())
            configSection.Bind(settings);

        configureSettings?.Invoke(settings);

        if (string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            throw new InvalidOperationException(
                $"n8n base URL is required. Configure it via ConnectionStrings:{connectionName} " +
                $"(e.g. by referencing the n8n resource in the AppHost) or via {connectionName}:BaseUrl in configuration.");
        }

        if (!Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException($"n8n base URL must be a valid HTTP(S) URL. Got: {settings.BaseUrl}");
        }

        builder.Services.AddSingleton(_ => new N8nApiClient(settings));

        builder.Services.AddHealthChecks()
            .AddCheck<N8nHealthCheck>(
                name: $"n8n_{connectionName}",
                tags: ["n8n", "ready"]);

        return builder;
    }
}
