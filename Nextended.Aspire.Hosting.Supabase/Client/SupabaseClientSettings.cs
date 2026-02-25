using Microsoft.Extensions.Configuration;

namespace Nextended.Aspire.Hosting.Supabase.Client;

/// <summary>
/// Settings for configuring a Supabase client connection.
/// </summary>
public sealed class SupabaseClientSettings
{
    /// <summary>
    /// The Supabase URL (e.g., https://xxx.supabase.co or http://localhost:8000 for local).
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// The Supabase API key (anon/public key for client-side, service role key for server-side).
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Whether to automatically connect to Realtime on initialization.
    /// Default: true
    /// </summary>
    public bool AutoConnectRealtime { get; set; } = true;

    /// <summary>
    /// Whether to automatically refresh the token.
    /// Default: true
    /// </summary>
    public bool AutoRefreshToken { get; set; } = true;

    /// <summary>
    /// Whether to persist the session.
    /// Default: true
    /// </summary>
    public bool PersistSession { get; set; } = true;

    /// <summary>
    /// Custom headers to include in requests.
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    internal void ParseConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        // Connection string format: "Url=http://localhost:8000;Key=eyJhbGc..."
        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        if (builder.TryGetValue("Url", out var url))
        {
            Url = url.ToString();
        }

        if (builder.TryGetValue("Key", out var key))
        {
            Key = key.ToString();
        }
    }
}

file class DbConnectionStringBuilder : System.Data.Common.DbConnectionStringBuilder
{
}
