using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Grafana;
using Nextended.Aspire.Hosting.Supabase.Builders;
using Nextended.Aspire.Hosting.Supabase.Resources;

namespace Nextended.Aspire.Hosting.Observability;

/// <summary>Grafana signing in with the accounts of a Supabase stack.</summary>
public static class GrafanaSupabaseLogin
{
    /// <summary>
    /// "Sign in with <paramref name="name"/>" — Supabase Auth of <paramref name="supabase"/> as
    /// Grafana's OAuth provider, registered there as public client <paramref name="clientName"/>
    /// (PKCE, no secret) and locked down like the other sign-ins: no login form, no local admin,
    /// and only whom <paramref name="roleAttributePath"/> gives a role gets in — e.g.
    /// <c>GrafanaOAuthOptions.Roles("permissions", grafanaAdmins: ["grafana.manage"])</c> over a claim
    /// the app adds with a custom access token hook (<see cref="OAuthBuilderExtensions.WithCustomAccessTokenHook"/>).
    /// The app serves the consent page (<see cref="OAuthBuilderExtensions.WithOAuthServer"/>).
    /// <paramref name="publicUrl"/> is Grafana's address when it is not its own endpoint (a custom domain).
    /// Use it in <c>ObservabilityStackOptions.ConfigureGrafana</c> or on a Grafana of your own.
    /// </summary>
    public static IResourceBuilder<GrafanaResource> WithSupabaseLogin(this IResourceBuilder<GrafanaResource> grafana,
        IResourceBuilder<SupabaseStackResource> supabase, string roleAttributePath, string? publicUrl = null,
        string name = "Supabase", string clientName = "Grafana")
    {
        ArgumentNullException.ThrowIfNull(supabase);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleAttributePath);
        var stack = supabase.Resource;
        var auth = (stack.Auth ?? throw new InvalidOperationException("Auth not configured. Ensure AddSupabase() has been called."))
            .GetEndpoint("http");
        var endpoint = grafana.GetEndpoint(GrafanaResource.HttpEndpointName);

        var client = supabase.AddOAuthClient(clientName, ReferenceExpression.Create($"{endpoint}{CallbackPath}"));

        grafana
            .WithGenericOAuth(name, client.ClientId.ToString(), clientSecret: null,
                // No openid scope: with the usual HS256 secret Supabase Auth signs no ID token.
                scopes: "email profile", roleAttributePath)
            .WithBrowserApiUrl("GF_AUTH_GENERIC_OAUTH_AUTH_URL", stack, "/auth/v1/oauth/authorize")
            // Back channel straight to Auth, inside the app's network.
            .WithEnvironment("GF_AUTH_GENERIC_OAUTH_TOKEN_URL", ReferenceExpression.Create($"{auth}/oauth/token"))
            .WithEnvironment("GF_AUTH_GENERIC_OAUTH_API_URL", ReferenceExpression.Create($"{auth}/oauth/userinfo"))
            // So the role, email and name come from the access token's claims.
            .WithEnvironment("GF_AUTH_GENERIC_OAUTH_ID_TOKEN_ATTRIBUTE_NAME", "access_token")
            .WithEnvironment("GF_AUTH_GENERIC_OAUTH_EMAIL_ATTRIBUTE_PATH", "email")
            .WithEnvironment("GF_AUTH_GENERIC_OAUTH_LOGIN_ATTRIBUTE_PATH", "email")
            .WithEnvironment("GF_AUTH_GENERIC_OAUTH_NAME_ATTRIBUTE_PATH", "user_metadata.display_name || user_metadata.full_name || email");

        if (publicUrl is not null)
        {
            if (!Uri.TryCreate(publicUrl, UriKind.Absolute, out _)) throw new ArgumentException($"an absolute URL — {publicUrl}", nameof(publicUrl));
            publicUrl = publicUrl.TrimEnd('/');
            client.RedirectUris.Add(ReferenceExpression.Create($"{publicUrl}{CallbackPath}"));
            // After the sign-in's own root URL, so the redirect Grafana sends is the custom domain.
            grafana.WithEnvironment("GF_SERVER_ROOT_URL", publicUrl);
        }

        return grafana;
    }

    /// <summary>Where Grafana's generic OAuth takes the provider's answer.</summary>
    public const string CallbackPath = "/login/generic_oauth";
}
