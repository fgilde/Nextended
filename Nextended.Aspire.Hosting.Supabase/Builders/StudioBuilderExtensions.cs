using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Resources;

namespace Nextended.Aspire.Hosting.Supabase.Builders;

/// <summary>
/// Provides extension methods for configuring the Supabase Studio Dashboard.
/// Note: The SupabaseStackResource IS the Studio container, so these methods configure the stack directly.
/// </summary>
public static class StudioBuilderExtensions
{
    #region Direct Stack Methods (Aspire-Standard Pattern)

    // Note: WithStudioPort, WithOrganizationName, WithProjectName are already direct methods
    // on the stack, so they follow the Aspire standard pattern.

    #endregion

    #region Legacy ConfigureStudio (Obsolete)

    /// <summary>
    /// Configures the Studio Dashboard settings.
    /// Since the SupabaseStackResource IS the Studio container, this configures the stack itself.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="configure">Configuration action for the Studio (stack) resource builder.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> ConfigureStudio(
        this IResourceBuilder<SupabaseStackResource> builder,
        Action<IResourceBuilder<SupabaseStackResource>> configure)
    {
        configure(builder);
        return builder;
    }

    #endregion

    #region Studio Configuration Methods

    /// <summary>
    /// Sets the external Studio port.
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithStudioPort(
        this IResourceBuilder<SupabaseStackResource> builder,
        int port)
    {
        builder.Resource.StudioPort = port;
        return builder;
    }

    /// <summary>
    /// Sets the organization name displayed in Studio.
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithOrganizationName(
        this IResourceBuilder<SupabaseStackResource> builder,
        string name)
    {
        builder.WithEnvironment("DEFAULT_ORGANIZATION_NAME", name);
        return builder;
    }

    /// <summary>
    /// Sets the project name displayed in Studio.
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithProjectName(
        this IResourceBuilder<SupabaseStackResource> builder,
        string name)
    {
        builder.WithEnvironment("DEFAULT_PROJECT_NAME", name);
        return builder;
    }


    public static IResourceBuilder<SupabaseStackResource> WithOpenAIIntegration(
        this IResourceBuilder<SupabaseStackResource> builder,
        string openAIApiKey)
    {
        builder.WithEnvironment("OPENAI_API_KEY ", openAIApiKey);
        return builder;
    }


    public static IResourceBuilder<SupabaseStackResource> WithOpenAIIntegration(
        this IResourceBuilder<SupabaseStackResource> builder,
        IResourceBuilder<ParameterResource> openAIApiKey)
    {
        builder.WithEnvironment("OPENAI_API_KEY", openAIApiKey);
        return builder;
    }

    /// <summary>
    /// Enables login protection for Supabase Studio Dashboard with username and password strings.
    /// When enabled, users must authenticate via HTTP Basic Auth before accessing Studio.
    /// Works both locally and in deployed environments.
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithLogin(
        this IResourceBuilder<SupabaseStackResource> builder,
        string username,
        string password)
    {
        builder.Resource.DashboardUsername = username;
        builder.Resource.DashboardPassword = password;
        builder.WithEnvironment("DASHBOARD_USERNAME", username);
        builder.WithEnvironment("DASHBOARD_PASSWORD", password);
        return ApplyAuthProxy(builder);
    }

    /// <summary>
    /// Enables login protection for Supabase Studio Dashboard with Aspire parameters.
    /// Useful for keeping credentials in user secrets or configuration.
    /// Works both locally and in deployed environments.
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithLogin(
        this IResourceBuilder<SupabaseStackResource> builder,
        IResourceBuilder<ParameterResource> username,
        IResourceBuilder<ParameterResource> password)
    {
        builder.Resource.DashboardUsername = "param";
        builder.Resource.DashboardPassword = "param";
        builder.WithEnvironment("DASHBOARD_USERNAME", username);
        builder.WithEnvironment("DASHBOARD_PASSWORD", password);
        return ApplyAuthProxy(builder);
    }

    /// <summary>
    /// Enables login protection for Supabase Studio Dashboard with the password from an Aspire
    /// parameter — deployed it is a secret, never plain text in the container's settings.
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithLogin(
        this IResourceBuilder<SupabaseStackResource> builder,
        string username,
        IResourceBuilder<ParameterResource> password)
    {
        builder.Resource.DashboardUsername = username;
        builder.Resource.DashboardPassword = "param";
        builder.WithEnvironment("DASHBOARD_USERNAME", username);
        builder.WithEnvironment("DASHBOARD_PASSWORD", password);
        return ApplyAuthProxy(builder);
    }

    /// <summary>
    /// Studio behind an OpenID Connect / OAuth 2.1 sign-in (Keycloak, Authentik, Entra …) instead of
    /// a password: the login proxy sends a page load to <see cref="StudioOAuthOptions.AuthUrl"/>
    /// (authorization code with PKCE), takes the token from <see cref="StudioOAuthOptions.TokenUrl"/>
    /// and lets in only whom <see cref="StudioOAuthOptions.Claim"/> grants
    /// <see cref="StudioOAuthOptions.Value"/> — then a signed session cookie. Register
    /// <c>{studio-url}/__oauth/callback</c> as redirect URI. For the stack's own accounts see
    /// <see cref="WithSupabaseLogin"/>.
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithOAuthLogin(
        this IResourceBuilder<SupabaseStackResource> builder, StudioOAuthOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ClientId);
        foreach (var (name, url) in new[] { ("AuthUrl", options.AuthUrl), ("TokenUrl", options.TokenUrl) })
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                throw new ArgumentException($"{name} is an absolute http(s) URL of the provider — {url}", nameof(options));

        builder
            .WithEnvironment("STUDIO_OAUTH_AUTH_URL", options.AuthUrl)
            .WithEnvironment("STUDIO_OAUTH_TOKEN_URL", options.TokenUrl);
        if (options.ClientSecret is { } secret) builder.WithEnvironment("STUDIO_OAUTH_CLIENT_SECRET", secret);
        if (options.RedirectUri is { } redirect) builder.WithEnvironment("STUDIO_OAUTH_REDIRECT_URI", redirect);
        return builder.WithOAuthProxy(options.ClientId, options.Scopes, options.Claim, options.Value);
    }

    /// <summary>
    /// Studio signs in with the stack's own accounts — Supabase Auth as OAuth provider
    /// (<see cref="OAuthBuilderExtensions.AddOAuthClient"/>, registered here as public client
    /// <paramref name="clientName"/>). Only a token whose <paramref name="claim"/> holds
    /// <paramref name="value"/> gets in, so the app puts it there with a custom access token hook
    /// (<see cref="OAuthBuilderExtensions.WithCustomAccessTokenHook"/>), e.g. a <c>permissions</c>
    /// array; the app also serves the consent page (<see cref="OAuthBuilderExtensions.WithOAuthServer"/>).
    /// <paramref name="publicUrl"/> is Studio's address when it is not its own endpoint (a custom domain).
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithSupabaseLogin(
        this IResourceBuilder<SupabaseStackResource> builder, string claim, string value,
        string? publicUrl = null, string clientName = "Supabase Studio")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(claim);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var stack = builder.Resource;
        var studio = builder.GetEndpoint("http");
        var auth = (stack.Auth ?? throw new InvalidOperationException("Auth not configured. Ensure AddSupabase() has been called."))
            .GetEndpoint("http");

        var client = builder.AddOAuthClient(clientName, ReferenceExpression.Create($"{studio}{StudioCallbackPath}"));
        if (publicUrl is not null)
        {
            if (!Uri.TryCreate(publicUrl, UriKind.Absolute, out _)) throw new ArgumentException($"an absolute URL — {publicUrl}", nameof(publicUrl));
            var redirect = publicUrl.TrimEnd('/') + StudioCallbackPath;
            client.RedirectUris.Add(ReferenceExpression.Create($"{redirect}"));
            builder.WithEnvironment("STUDIO_OAUTH_REDIRECT_URI", redirect);
        }
        else if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            builder.WithEnvironment("STUDIO_OAUTH_REDIRECT_URI", ReferenceExpression.Create($"{studio}{StudioCallbackPath}"));
        }
        else
        {
            // Locally the reference would be the container-network name; the browser comes back to the host.
            builder.WithEnvironment(context => context.EnvironmentVariables["STUDIO_OAUTH_REDIRECT_URI"] = studio.Url + StudioCallbackPath);
        }

        builder
            .WithBrowserApiUrl("STUDIO_OAUTH_AUTH_URL", stack, "/auth/v1/oauth/authorize")
            // Back channel straight to Auth, inside the app's network.
            .WithEnvironment("STUDIO_OAUTH_TOKEN_URL", ReferenceExpression.Create($"{auth}/oauth/token"));
        // No openid scope: with the usual HS256 secret Supabase Auth signs no ID token, the claims come from the access token.
        return builder.WithOAuthProxy(client.ClientId.ToString(), "email", claim, value);
    }

    /// <summary>Where the login proxy takes the provider's answer.</summary>
    public const string StudioCallbackPath = "/__oauth/callback";

    private static IResourceBuilder<SupabaseStackResource> WithOAuthProxy(this IResourceBuilder<SupabaseStackResource> builder,
        string clientId, string scopes, string claim, string value)
    {
        builder.Resource.DashboardUsername = null;
        builder.Resource.DashboardPassword = null;
        builder
            .WithEnvironment("STUDIO_AUTH_MODE", "oauth")
            .WithEnvironment("STUDIO_OAUTH_CLIENT_ID", clientId)
            .WithEnvironment("STUDIO_OAUTH_SCOPES", scopes)
            .WithEnvironment("STUDIO_OAUTH_CLAIM", claim)
            .WithEnvironment("STUDIO_OAUTH_VALUE", value)
            // No basic auth next to it, even if WithLogin was called before.
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables.Remove("DASHBOARD_USERNAME");
                context.EnvironmentVariables.Remove("DASHBOARD_PASSWORD");
            });
        return ApplyAuthProxy(builder);
    }

    /// <summary>
    /// Puts the login proxy (Resources/studio-auth-proxy.js) in front of Studio: it starts Studio on
    /// PORT+1 and forwards only requests with a login — HTTP Basic Auth from DASHBOARD_USERNAME /
    /// DASHBOARD_PASSWORD, or with STUDIO_AUTH_MODE=oauth the OAuth sign-in (STUDIO_OAUTH_*).
    /// </summary>
    private static IResourceBuilder<SupabaseStackResource> ApplyAuthProxy(
        IResourceBuilder<SupabaseStackResource> builder)
    {
        var proxyBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(AuthProxyScriptSource()));
        builder.WithEnvironment("AUTH_PROXY_SCRIPT_BASE64", proxyBase64);
        builder.WithEntrypoint("/bin/sh");
        // Called twice (WithLogin, then WithOAuthLogin) the arguments are set, not appended.
        builder.WithArgs(context =>
        {
            context.Args.Clear();
            context.Args.Add("-c");
            context.Args.Add("echo \"$AUTH_PROXY_SCRIPT_BASE64\" | base64 -d > /tmp/auth-proxy.js && exec node /tmp/auth-proxy.js");
        });

        return builder;
    }

    /// <summary>The login proxy's JavaScript source, as Studio's container runs it.</summary>
    public static string AuthProxyScriptSource() => AuthProxyScript.Value;

    private static readonly Lazy<string> AuthProxyScript = new(() =>
    {
        using var stream = typeof(StudioBuilderExtensions).Assembly.GetManifestResourceStream("studio-auth-proxy.js")
            ?? throw new InvalidOperationException("studio-auth-proxy.js is not embedded.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });

    #endregion
}

/// <summary>
/// An OpenID Connect / OAuth 2.1 provider for the Studio login (<c>WithOAuthLogin</c>). Grafana's
/// presets work the same way; <see cref="Keycloak"/> derives the endpoints from a realm.
/// </summary>
public sealed class StudioOAuthOptions
{
    public required string ClientId { get; init; }

    /// <summary>Confidential clients only; a public client relies on PKCE alone.</summary>
    public ReferenceExpression? ClientSecret { get; init; }

    /// <summary>The provider's authorization endpoint, as the browser reaches it.</summary>
    public required string AuthUrl { get; init; }

    /// <summary>The provider's token endpoint, as Studio's container reaches it.</summary>
    public required string TokenUrl { get; init; }

    public string Scopes { get; init; } = "openid email profile";

    /// <summary>
    /// Claim of the ID token (else the access token) that has to hold <see cref="Value"/>, as the
    /// value or one entry of an array; dots go deeper, e.g. <c>realm_access.roles</c>.
    /// </summary>
    public required string Claim { get; init; }

    public required string Value { get; init; }

    /// <summary>
    /// The redirect URI registered at the provider. Empty = <c>{scheme}://{host}/__oauth/callback</c>
    /// of the request (honouring X-Forwarded-Proto / -Host).
    /// </summary>
    public string? RedirectUri { get; init; }

    /// <summary>
    /// A Keycloak realm, e.g. <c>https://sso.example.com/realms/company</c>: only users with the
    /// client role <paramref name="role"/> (in a <c>roles</c> claim — a "User Client Role" mapper
    /// with that token claim name) get in. <paramref name="backchannelRealmUrl"/> is how Studio's
    /// container reaches the realm when that differs from the browser's address.
    /// </summary>
    public static StudioOAuthOptions Keycloak(string realmUrl, string clientId, ReferenceExpression clientSecret,
        string role = "studio-admin", string? backchannelRealmUrl = null)
    {
        static string Realm(string url, string parameter) =>
            Uri.TryCreate(url, UriKind.Absolute, out var realm)
            && (realm.Scheme == Uri.UriSchemeHttps || realm.Scheme == Uri.UriSchemeHttp)
            && realm.AbsolutePath.Contains("/realms/", StringComparison.Ordinal)
                ? url.TrimEnd('/') + "/protocol/openid-connect"
                : throw new ArgumentException($"a realm URL looks like https://sso.example.com/realms/company — {url}", parameter);

        var browser = Realm(realmUrl, nameof(realmUrl));
        var backchannel = backchannelRealmUrl is null ? browser : Realm(backchannelRealmUrl, nameof(backchannelRealmUrl));
        return new StudioOAuthOptions
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            AuthUrl = $"{browser}/auth",
            TokenUrl = $"{backchannel}/token",
            Claim = "roles",
            Value = role,
        };
    }
}
