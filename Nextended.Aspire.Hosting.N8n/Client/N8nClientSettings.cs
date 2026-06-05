namespace Nextended.Aspire.Hosting.N8n.Client;

/// <summary>
/// Settings for configuring an n8n REST API client connection.
/// </summary>
public sealed class N8nClientSettings
{
    /// <summary>The base URL of the n8n instance (e.g. http://localhost:5678).</summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// The n8n public API key (sent as the <c>X-N8N-API-KEY</c> header).
    /// Create one in n8n under Settings → n8n API.
    /// </summary>
    public string? ApiKey { get; set; }

    internal void ParseConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        // A bare URL (as produced by the n8n resource connection string) is treated as BaseUrl.
        if (!connectionString.Contains('='))
        {
            BaseUrl = connectionString.Trim();
            return;
        }

        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        if (builder.TryGetValue("Url", out var url))
            BaseUrl = url.ToString();
        else if (builder.TryGetValue("BaseUrl", out var baseUrl))
            BaseUrl = baseUrl.ToString();

        if (builder.TryGetValue("ApiKey", out var apiKey))
            ApiKey = apiKey.ToString();
        else if (builder.TryGetValue("Key", out var key))
            ApiKey = key.ToString();
    }
}

file class DbConnectionStringBuilder : System.Data.Common.DbConnectionStringBuilder
{
}
