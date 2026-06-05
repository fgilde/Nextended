namespace Nextended.Aspire.Hosting.N8n.Client;

/// <summary>
/// A lightweight, ready-to-use client for the n8n REST API.
/// Wraps a pre-configured <see cref="HttpClient"/> with the base URL and API key applied.
/// </summary>
public sealed class N8nApiClient : IDisposable
{
    /// <summary>The n8n public API base path (relative to the instance base URL).</summary>
    public const string ApiBasePath = "/api/v1/";

    /// <summary>
    /// Creates a new <see cref="N8nApiClient"/> from the given settings.
    /// </summary>
    public N8nApiClient(N8nClientSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(settings.BaseUrl))
            throw new InvalidOperationException("n8n BaseUrl is required.");

        Http = new HttpClient { BaseAddress = new Uri(settings.BaseUrl, UriKind.Absolute) };
        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            Http.DefaultRequestHeaders.Add("X-N8N-API-KEY", settings.ApiKey);
    }

    /// <summary>Gets the configured <see cref="HttpClient"/> targeting the n8n instance.</summary>
    public HttpClient Http { get; }

    /// <summary>
    /// Returns the URI of the n8n public API (e.g. for calling <c>/api/v1/workflows</c>).
    /// </summary>
    public Uri ApiBaseUri => new(Http.BaseAddress!, ApiBasePath);

    /// <inheritdoc />
    public void Dispose() => Http.Dispose();
}
