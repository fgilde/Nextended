using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.SigNoz;

/// <summary>
/// The OpenID Connect provider of <c>WithOidcLogin</c>. With <see cref="IssuerUrl"/> alone the proxy reads the provider's discovery
/// document; <see cref="LoginUrl"/>, <see cref="RedeemUrl"/> and <see cref="JwksUrl"/> together replace it — for a provider the proxy
/// reaches at another address than the browser does. <see cref="Keycloak"/> sets everything up from a realm.
/// </summary>
public sealed class SigNozOidcOptions
{
    /// <summary>The issuer as it appears in the tokens (<c>iss</c>), e.g. <c>https://sso.example.com/realms/company</c>.</summary>
    public required string IssuerUrl { get; init; }

    public required string ClientId { get; init; }
    public required ReferenceExpression ClientSecret { get; init; }

    /// <summary>The provider's name on the proxy's pages.</summary>
    public string Name { get; init; } = "OpenID Connect";

    /// <summary>oauth2-proxy's provider: <c>oidc</c>, or <c>keycloak-oidc</c> (needed for <see cref="AllowedRoles"/>), <c>entra-id</c> …</summary>
    public string Provider { get; init; } = "oidc";

    public string Scopes { get; init; } = "openid email profile";

    /// <summary>The authorization endpoint as the browser reaches it.</summary>
    public string? LoginUrl { get; init; }

    /// <summary>The token endpoint as the proxy reaches it.</summary>
    public string? RedeemUrl { get; init; }

    /// <summary>The signing keys as the proxy reaches them.</summary>
    public string? JwksUrl { get; init; }

    /// <summary>Keycloak roles one of which a user needs (<c>keycloak-oidc</c>): realm roles by name, client roles as <c>client:role</c>.</summary>
    public IReadOnlyList<string> AllowedRoles { get; init; } = [];

    /// <summary>Groups one of which a user needs, read from <see cref="GroupsClaim"/>.</summary>
    public IReadOnlyList<string> AllowedGroups { get; init; } = [];

    public string GroupsClaim { get; init; } = "groups";

    /// <summary>Claims that may carry the client id as audience; <c>null</c> keeps oauth2-proxy's <c>aud</c>. Keycloak's access tokens name the client in <c>azp</c> only.</summary>
    public IReadOnlyList<string>? AudienceClaims { get; init; }

    /// <summary>Email domains allowed in; <c>*</c> (default) leaves the decision to the provider and the roles or groups.</summary>
    public IReadOnlyList<string> AllowedEmailDomains { get; init; } = ["*"];

    /// <summary>
    /// A Keycloak realm, e.g. <c>https://sso.example.com/realms/company</c>. Only users with one of <paramref name="allowedRoles"/> get in —
    /// by default the client role <c>admin</c> of <paramref name="clientId"/> (everyone who gets in is SigNoz's admin).
    /// <paramref name="backchannelRealmUrl"/> is how the proxy reaches the realm when that differs from the browser's address — a Keycloak
    /// container next to it is <c>http://keycloak:8080/…</c> for the proxy and <c>http://localhost:…</c> for the browser; Keycloak then needs
    /// <c>KC_HOSTNAME</c> (the browser's address) and <c>KC_HOSTNAME_BACKCHANNEL_DYNAMIC=true</c>.
    /// </summary>
    public static SigNozOidcOptions Keycloak(string realmUrl, string clientId, ReferenceExpression clientSecret,
        string? backchannelRealmUrl = null, IReadOnlyList<string>? allowedRoles = null, string name = "Keycloak")
    {
        var browser = RealmUrl(realmUrl, nameof(realmUrl));
        var backchannel = backchannelRealmUrl is null ? null : RealmUrl(backchannelRealmUrl, nameof(backchannelRealmUrl));
        return new SigNozOidcOptions
        {
            Name = name,
            Provider = "keycloak-oidc",
            IssuerUrl = browser,
            ClientId = clientId,
            ClientSecret = clientSecret,
            LoginUrl = backchannel is null ? null : $"{browser}/protocol/openid-connect/auth",
            RedeemUrl = backchannel is null ? null : $"{backchannel}/protocol/openid-connect/token",
            JwksUrl = backchannel is null ? null : $"{backchannel}/protocol/openid-connect/certs",
            AllowedRoles = allowedRoles ?? [$"{clientId}:admin"],
            AudienceClaims = ["aud", "azp"],
        };
    }

    private static string RealmUrl(string url, string parameter) =>
        Uri.TryCreate(url, UriKind.Absolute, out var realm)
        && (realm.Scheme == Uri.UriSchemeHttps || realm.Scheme == Uri.UriSchemeHttp)
        && realm.AbsolutePath.Contains("/realms/", StringComparison.Ordinal)
            ? url.TrimEnd('/')
            : throw new ArgumentException($"a realm URL looks like https://sso.example.com/realms/company — {url}", parameter);
}
