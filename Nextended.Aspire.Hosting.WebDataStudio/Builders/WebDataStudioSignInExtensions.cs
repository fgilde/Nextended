using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// Signing in to the studio with the identity provider the organisation already has, and keeping a
/// record of what was done through it.
/// <para>
/// <c>WithLogin</c> and <c>WithUser</c> put accounts in the container's environment: fine for one
/// team, wrong for a company that already decides who works there somewhere else. These calls point
/// the studio at that decision instead — Entra, Keycloak, Auth0, Okta, anything speaking OpenID
/// Connect — and the studio never sees a password.
/// </para>
/// </summary>
public static class WebDataStudioSignInExtensions
{
    /// <summary>
    /// Signs people in through an identity provider. The studio's own login form disappears unless
    /// accounts were configured as well.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="authority">
    /// The provider's issuer, e.g. <c>https://login.microsoftonline.com/&lt;tenant&gt;/v2.0</c> or
    /// <c>https://keycloak/realms/company</c>.
    /// </param>
    /// <param name="clientId">The application registered with the provider.</param>
    /// <param name="clientSecret">
    /// Its secret, as an Aspire parameter so it stays out of the manifest and out of source control.
    /// </param>
    /// <param name="label">What the button on the login screen says.</param>
    /// <param name="scopes">
    /// What to ask the provider for. <c>openid profile email</c> unless something else is named —
    /// add the one that carries groups where the roles come from groups.
    /// </param>
    /// <remarks>
    /// The redirect URI to register with the provider is <c>https://&lt;the studio&gt;/signin-oidc</c>.
    /// Roles stay the studio's own: see <see cref="WithSignInRoles"/>, because a provider knows its
    /// groups and not what an admin may do here.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithSingleSignOn(
        this IResourceBuilder<WebDataStudioResource> builder,
        string authority,
        string clientId,
        IResourceBuilder<ParameterResource>? clientSecret = null,
        string? label = null,
        params string[] scopes)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(authority);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        if (!Uri.TryCreate(authority, UriKind.Absolute, out var issuer)
            || (issuer.Scheme != Uri.UriSchemeHttps && issuer.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException(
                "an authority is the provider's issuer URL, e.g. " +
                "https://login.microsoftonline.com/<tenant>/v2.0 — " + authority, nameof(authority));

        builder.Resource.SignInAuthority = authority;

        builder = builder
            .WithEnvironment("WDS_OIDC_AUTHORITY", authority)
            .WithEnvironment("WDS_OIDC_CLIENT_ID", clientId);

        if (clientSecret is not null)
            builder = builder.WithEnvironment("WDS_OIDC_CLIENT_SECRET", clientSecret);

        if (!string.IsNullOrWhiteSpace(label))
            builder = builder.WithEnvironment("WDS_OIDC_LABEL", label);

        var asked = (scopes ?? [])
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope.Trim())
            .ToArray();

        if (asked.Length > 0)
            builder = builder.WithEnvironment("WDS_OIDC_SCOPES", string.Join(",", asked));

        // A provider on plain http is a Keycloak on a laptop, which is exactly what an app host runs.
        // Saying so here beats a metadata request that fails for a reason nobody can see.
        if (issuer.Scheme == Uri.UriSchemeHttp)
            builder = builder.WithEnvironment("WDS_OIDC_REQUIRE_HTTPS", "false");

        return builder;
    }

    /// <summary>
    /// Which of the provider's groups, roles or addresses get which studio role, and what everybody
    /// else gets.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="admins">Gets the admin role — everything, including the administration surface.</param>
    /// <param name="editors">Gets <c>editor</c>: read and write.</param>
    /// <param name="viewers">Gets <c>viewer</c>: every connection read-only.</param>
    /// <param name="defaultRole">
    /// What somebody who matched none of them gets. <c>viewer</c> unless said otherwise, which is the
    /// safe end of the three.
    /// </param>
    /// <remarks>
    /// Matching reads the provider's <c>roles</c>, <c>role</c>, <c>groups</c> and <c>wids</c> claims
    /// and the person's own name, address and UPN — so an address works in a tenant with no groups.
    /// It is not case-sensitive, and admin beats editor beats viewer for somebody in two of them.
    /// </remarks>
    public static IResourceBuilder<WebDataStudioResource> WithSignInRoles(
        this IResourceBuilder<WebDataStudioResource> builder,
        string[]? admins = null,
        string[]? editors = null,
        string[]? viewers = null,
        string? defaultRole = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (defaultRole is { Length: > 0 } && !StudioRoles.All.Contains(defaultRole))
            throw new ArgumentException(
                $"a role is {string.Join(", ", StudioRoles.All)} — {defaultRole}", nameof(defaultRole));

        builder = Names(builder, "WDS_OIDC_ADMINS", admins);
        builder = Names(builder, "WDS_OIDC_EDITORS", editors);
        builder = Names(builder, "WDS_OIDC_VIEWERS", viewers);

        return defaultRole is { Length: > 0 }
            ? builder.WithEnvironment("WDS_OIDC_DEFAULT_ROLE", defaultRole)
            : builder;
    }

    /// <summary>
    /// How long the studio keeps its record of who did what, in days. The trail is on by default and
    /// keeps 90 days; this is for a deployment that has to say a number out loud.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithAuditTrail(
        this IResourceBuilder<WebDataStudioResource> builder, int days = 90)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfLessThan(days, 1);

        builder.Resource.AuditDays = days;

        return builder
            .WithEnvironment("WDS_AUDIT", "true")
            .WithEnvironment("WDS_AUDIT_DAYS", days.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Turns the record off, for a deployment that keeps its own — a gateway that already logs every
    /// request with the person behind it.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithoutAuditTrail(
        this IResourceBuilder<WebDataStudioResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Resource.AuditDays = 0;
        return builder.WithEnvironment("WDS_AUDIT", "false");
    }

    private static IResourceBuilder<WebDataStudioResource> Names(
        IResourceBuilder<WebDataStudioResource> builder, string variable, string[]? names)
    {
        var named = (names ?? [])
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .ToArray();

        return named.Length == 0 ? builder : builder.WithEnvironment(variable, string.Join(",", named));
    }
}
