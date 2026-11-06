using System.Security.Cryptography;
using System.Text;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.Supabase.Resources;

/// <summary>
/// A client that signs in with the accounts of a Supabase stack (Supabase Auth as OAuth 2.1
/// provider) — Grafana, Studio or anything else speaking OAuth with PKCE. It is a public client:
/// no secret, PKCE and the exact redirect URIs protect the flow. The id is derived from the stack
/// and the name, so it stays the same across starts, deploys and rebuilds and can be handed to the
/// client as plain configuration.
/// </summary>
public sealed class SupabaseOAuthClient
{
    internal SupabaseOAuthClient(string stackName, string name)
    {
        Name = name;
        ClientId = IdFor(stackName, name);
    }

    /// <summary>What the consent page shows and Supabase Auth stores as client name.</summary>
    public string Name { get; }

    /// <summary>The OAuth <c>client_id</c>.</summary>
    public Guid ClientId { get; }

    /// <summary>
    /// The URIs Supabase Auth may send the browser back to — compared exactly, so every address the
    /// client is reached under needs its own (endpoint and custom domain). Deployed an expression
    /// resolves to the public address; locally to the one on the host (localhost).
    /// </summary>
    public List<ReferenceExpression> RedirectUris { get; } = [];

    /// <summary>The client id <see cref="SupabaseOAuthClient"/> gets for <paramref name="name"/> in stack <paramref name="stackName"/>.</summary>
    public static Guid IdFor(string stackName, string name)
    {
        // Name-based UUID (version 5 layout over SHA-256): stable, and two stacks never share one.
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"nextended-oauth-client:{stackName}:{name}"));
        var bytes = hash.AsSpan(0, 16).ToArray();
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes, bigEndian: true);
    }

    /// <summary>psql variable carrying the comma-joined redirect URIs of the client at <paramref name="index"/>.</summary>
    internal static string RedirectVariable(int index) => $"oauth_client_redirects_{index}";

    /// <summary>All redirect URIs as one comma-joined expression — Supabase Auth's storage format.</summary>
    internal ReferenceExpression JoinedRedirectUris()
    {
        var builder = new ReferenceExpressionBuilder();
        for (var i = 0; i < RedirectUris.Count; i++)
        {
            if (i > 0) builder.AppendLiteral(",");
            builder.Append($"{RedirectUris[i]}");
        }
        return builder.Build();
    }

    /// <summary>
    /// Upserts every client into <c>auth.oauth_clients</c> — idempotent, runs on each start of the
    /// init container. Auth versions without the OAuth server have no such table; then it only says so.
    /// </summary>
    internal static string RegistrationSql(IReadOnlyList<SupabaseOAuthClient> clients)
    {
        if (clients.Count == 0) return string.Empty;
        var sql = new StringBuilder();
        sql.AppendLine("-- OAuth clients (Supabase Auth as OAuth 2.1 provider)");
        sql.AppendLine("SELECT to_regclass('auth.oauth_clients') IS NOT NULL AS nextended_oauth_clients \\gset");
        sql.AppendLine("\\if :nextended_oauth_clients");
        for (var i = 0; i < clients.Count; i++)
        {
            var client = clients[i];
            var name = client.Name.Replace("'", "''");
            sql.AppendLine(
                "INSERT INTO auth.oauth_clients (id, client_secret_hash, registration_type, client_type, token_endpoint_auth_method, redirect_uris, grant_types, client_name)");
            sql.AppendLine(
                $"VALUES ('{client.ClientId}', '', 'manual', 'public', 'none', :'{RedirectVariable(i)}', 'authorization_code,refresh_token', '{name}')");
            sql.AppendLine(
                "ON CONFLICT (id) DO UPDATE SET redirect_uris = EXCLUDED.redirect_uris, client_name = EXCLUDED.client_name, client_type = 'public', " +
                "token_endpoint_auth_method = 'none', client_secret_hash = '', grant_types = EXCLUDED.grant_types, deleted_at = NULL, updated_at = now();");
        }
        sql.AppendLine("\\else");
        sql.AppendLine("\\echo '[Post-Init] auth.oauth_clients fehlt: diese Supabase-Auth-Version hat keinen OAuth-Server'");
        sql.AppendLine("\\endif");
        return sql.ToString();
    }
}
