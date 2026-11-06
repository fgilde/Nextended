using System.Text.RegularExpressions;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Resources;

namespace Nextended.Aspire.Hosting.Supabase.Builders;

/// <summary>
/// Sign-in beyond email and password, in both directions:
/// <list type="bullet">
/// <item>Supabase Auth as OAuth 2.1 provider (<see cref="WithOAuthServer"/>, <see cref="AddOAuthClient"/>):
/// other tools — Grafana, Studio, anything with OAuth and PKCE — sign in with the app's accounts.</item>
/// <item>External providers for the app itself (<see cref="WithExternalProvider(IResourceBuilder{SupabaseStackResource}, string, string, ReferenceExpression, string?, string?)"/>,
/// <see cref="WithKeycloak(IResourceBuilder{SupabaseStackResource}, string, string, ReferenceExpression)"/>):
/// "Sign in with Keycloak / Azure / Google …" on the app's login page.</item>
/// </list>
/// </summary>
public static partial class OAuthBuilderExtensions
{
    /// <summary>Where Supabase Auth sends the browser for consent: a page of the app at the site URL.</summary>
    public const string DefaultConsentPath = "/oauth/consent";

    /// <summary>
    /// Turns Supabase Auth into an OAuth 2.1 provider (authorization code with PKCE, refresh
    /// tokens, userinfo). The app has to serve <paramref name="consentPath"/>: it reads
    /// <c>authorization_id</c> from the query, shows the request with
    /// <c>supabase.auth.oauth.getAuthorizationDetails(id)</c> and answers with
    /// <c>approveAuthorization(id)</c> or <c>denyAuthorization(id)</c>; once a user approved a
    /// client, later sign-ins skip the page. Dynamic client registration stays off — clients come
    /// from <see cref="AddOAuthClient"/>. ID tokens need an asymmetric JWT signing key; with the
    /// usual HS256 secret request no <c>openid</c> scope and read the claims from the access token.
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithOAuthServer(
        this IResourceBuilder<SupabaseStackResource> builder, string consentPath = DefaultConsentPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consentPath);
        if (!consentPath.StartsWith('/')) throw new ArgumentException($"a path of the app, starting with / — {consentPath}", nameof(consentPath));

        AuthOf(builder)
            .WithEnvironment("GOTRUE_OAUTH_SERVER_ENABLED", "true")
            .WithEnvironment("GOTRUE_OAUTH_SERVER_AUTHORIZATION_PATH", consentPath);
        return builder;
    }

    /// <summary>
    /// A client that signs in with this stack's accounts, upserted into <c>auth.oauth_clients</c>
    /// on every start (also turns on <see cref="WithOAuthServer"/>). Calling it again with the same
    /// name returns the same client and adds the further redirect URIs.
    /// </summary>
    public static SupabaseOAuthClient AddOAuthClient(
        this IResourceBuilder<SupabaseStackResource> builder, string name, params ReferenceExpression[] redirectUris)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var stack = builder.Resource;
        var client = stack.OAuthClients.FirstOrDefault(c => c.Name == name);
        if (client is null)
        {
            if (stack.OAuthClients.Count == 0) builder.WithOAuthServer();
            client = new SupabaseOAuthClient(stack.Name, name);
            stack.OAuthClients.Add(client);
        }
        client.RedirectUris.AddRange(redirectUris);
        return client;
    }

    /// <summary>
    /// Runs <paramref name="function"/> (<c>schema.name</c>, a Postgres function
    /// <c>(event jsonb) returns jsonb</c>) before every access token is issued, to add or change
    /// claims — e.g. permissions an OAuth client checks. Grant <c>supabase_auth_admin</c> usage on
    /// the schema and execute on the function; an error in it fails the sign-in, so catch them.
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithCustomAccessTokenHook(
        this IResourceBuilder<SupabaseStackResource> builder, string function = "public.custom_access_token_hook")
    {
        var parts = function.Split('.');
        if (parts.Length != 2 || parts.Any(p => !Identifier().IsMatch(p)))
            throw new ArgumentException($"schema.function, e.g. public.custom_access_token_hook — {function}", nameof(function));

        AuthOf(builder)
            .WithEnvironment("GOTRUE_HOOK_CUSTOM_ACCESS_TOKEN_ENABLED", "true")
            .WithEnvironment("GOTRUE_HOOK_CUSTOM_ACCESS_TOKEN_URI", $"pg-functions://postgres/{parts[0]}/{parts[1]}");
        return builder;
    }

    /// <summary>
    /// An external sign-in provider for the app — any one Supabase Auth knows (<c>keycloak</c>,
    /// <c>azure</c>, <c>google</c>, <c>github</c>, <c>gitlab</c>, <c>linkedin_oidc</c> …); the app starts it
    /// with <c>supabase.auth.signInWithOAuth({ provider })</c>. <paramref name="url"/> is the provider's
    /// own address where it has one (Keycloak realm, Azure tenant, GitLab instance). Register
    /// <paramref name="redirectUri"/> at the provider; by default it is the API's
    /// <c>/auth/v1/callback</c> under the public address (<see cref="WithPublicUrl"/>, else Kong).
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithExternalProvider(
        this IResourceBuilder<SupabaseStackResource> builder, string provider, string clientId,
        ReferenceExpression secret, string? url = null, string? redirectUri = null)
    {
        ArgumentNullException.ThrowIfNull(secret);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        if (string.IsNullOrWhiteSpace(provider) || !Identifier().IsMatch(provider))
            throw new ArgumentException($"a provider name of Supabase Auth, e.g. keycloak or linkedin_oidc — {provider}", nameof(provider));
        if (url is not null && !IsHttpUrl(url))
            throw new ArgumentException($"the provider's absolute http(s) address — {url}", nameof(url));

        var prefix = $"GOTRUE_EXTERNAL_{provider.ToUpperInvariant()}";
        var auth = AuthOf(builder)
            .WithEnvironment($"{prefix}_ENABLED", "true")
            .WithEnvironment($"{prefix}_CLIENT_ID", clientId)
            .WithEnvironment($"{prefix}_SECRET", secret);
        if (url is not null) auth.WithEnvironment($"{prefix}_URL", url.TrimEnd('/'));
        if (redirectUri is not null) auth.WithEnvironment($"{prefix}_REDIRECT_URI", redirectUri);
        else auth.WithBrowserApiUrl($"{prefix}_REDIRECT_URI", builder.Resource, "/auth/v1/callback");
        return builder;
    }

    /// <inheritdoc cref="WithExternalProvider(IResourceBuilder{SupabaseStackResource}, string, string, ReferenceExpression, string?, string?)"/>
    public static IResourceBuilder<SupabaseStackResource> WithExternalProvider(
        this IResourceBuilder<SupabaseStackResource> builder, string provider, string clientId,
        IResourceBuilder<ParameterResource> secret, string? url = null, string? redirectUri = null)
        => builder.WithExternalProvider(provider, clientId, ReferenceExpression.Create($"{secret}"), url, redirectUri);

    /// <summary>
    /// "Sign in with Keycloak" for the app: a confidential OpenID Connect client of the realm
    /// <paramref name="realmUrl"/> (e.g. <c>https://sso.example.com/realms/company</c>) whose valid
    /// redirect URI is the API's <c>/auth/v1/callback</c>; the app calls
    /// <c>supabase.auth.signInWithOAuth({ provider: 'keycloak', options: { scopes: 'openid' } })</c>.
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithKeycloak(
        this IResourceBuilder<SupabaseStackResource> builder, string realmUrl, string clientId, ReferenceExpression secret)
    {
        if (!IsHttpUrl(realmUrl) || !realmUrl.Contains("/realms/", StringComparison.Ordinal))
            throw new ArgumentException($"a realm URL looks like https://sso.example.com/realms/company — {realmUrl}", nameof(realmUrl));
        return builder.WithExternalProvider("keycloak", clientId, secret, realmUrl);
    }

    /// <inheritdoc cref="WithKeycloak(IResourceBuilder{SupabaseStackResource}, string, string, ReferenceExpression)"/>
    public static IResourceBuilder<SupabaseStackResource> WithKeycloak(
        this IResourceBuilder<SupabaseStackResource> builder, string realmUrl, string clientId, IResourceBuilder<ParameterResource> secret)
        => builder.WithKeycloak(realmUrl, clientId, ReferenceExpression.Create($"{secret}"));

    /// <summary>
    /// The address browsers reach the API under when it is not Kong's own endpoint — a custom
    /// domain, e.g. <c>https://api.example.com</c>. Auth builds its links from it
    /// (<c>API_EXTERNAL_URL</c>), Studio its API URL, and the OAuth sign-ins their redirects.
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithPublicUrl(
        this IResourceBuilder<SupabaseStackResource> builder, string url)
    {
        if (!IsHttpUrl(url)) throw new ArgumentException($"an absolute http(s) URL — {url}", nameof(url));
        url = url.TrimEnd('/');
        builder.Resource.PublicUrl = url;
        AuthOf(builder).WithEnvironment("API_EXTERNAL_URL", url);
        return builder.WithEnvironment("SUPABASE_PUBLIC_URL", url);
    }

    /// <summary>
    /// Sets <paramref name="variable"/> to the API address a browser uses plus <paramref name="path"/>:
    /// <see cref="WithPublicUrl"/> if set, else Kong's endpoint — deployed its public URL, locally the
    /// one on the host. Read when the environment is built, so a later <see cref="WithPublicUrl"/> counts.
    /// </summary>
    internal static IResourceBuilder<T> WithBrowserApiUrl<T>(this IResourceBuilder<T> target, string variable,
        SupabaseStackResource stack, string path) where T : IResourceWithEnvironment
    {
        var kong = (stack.Kong ?? throw new InvalidOperationException("Kong not configured. Ensure AddSupabase() has been called."))
            .GetEndpoint("http");
        if (target.ApplicationBuilder.ExecutionContext.IsPublishMode)
            return target.WithEnvironment(context => context.EnvironmentVariables[variable] =
                stack.PublicUrl is { } url ? url + path : (object)ReferenceExpression.Create($"{kong}{path}"));
        return target.WithEnvironment(context => context.EnvironmentVariables[variable] = (stack.PublicUrl ?? kong.Url) + path);
    }

    /// <summary>
    /// The value a psql variable of the local post-init script gets: what the browser sees, so an
    /// endpoint inside resolves to its address on the host.
    /// </summary>
    internal static async Task<string> HostValueAsync(object value, CancellationToken cancellationToken) => value switch
    {
        string text => text,
        IResourceBuilder<ParameterResource> parameter => await parameter.Resource.GetValueAsync(cancellationToken) ?? string.Empty,
        IValueProvider provider => await provider.GetValueAsync(cancellationToken) ?? string.Empty,
        _ => value.ToString() ?? string.Empty,
    };

    private static IResourceBuilder<SupabaseAuthResource> AuthOf(IResourceBuilder<SupabaseStackResource> builder) =>
        builder.Resource.Auth ?? throw new InvalidOperationException("Auth not configured. Ensure AddSupabase() has been called.");

    private static bool IsHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    [GeneratedRegex("^[a-z_][a-z0-9_]*$")]
    private static partial Regex Identifier();
}
