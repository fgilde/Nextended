using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Nextended.Aspire.Hosting.WebDataStudio.Tests;

/// <summary>
/// Signing in through the identity provider the organisation already has, and the record of what was
/// done through the studio.
/// </summary>
public class WebDataStudioSignInTests
{
    private static IResourceBuilder<WebDataStudioResource> Add() =>
        DistributedApplication.CreateBuilder().AddWebDataStudio();

    private static async Task<Dictionary<string, string>> EnvOf(IResource resource)
    {
        var context = new EnvironmentCallbackContext(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));

        foreach (var annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
            await annotation.Callback(context);

        var resolved = new Dictionary<string, string>();

        foreach (var (key, value) in context.EnvironmentVariables)
            resolved[key] = value switch
            {
                string text => text,
                IValueProvider provider => await provider.GetValueAsync(default) ?? "",
                _ => value?.ToString() ?? "",
            };

        return resolved;
    }

    [Fact]
    public async Task WithSingleSignOn_WritesTheAuthorityAndTheClient()
    {
        var studio = Add().WithSingleSignOn(
            "https://login.microsoftonline.com/tenant/v2.0", "studio-app");

        var env = await EnvOf(studio.Resource);

        Assert.Equal("https://login.microsoftonline.com/tenant/v2.0", env["WDS_OIDC_AUTHORITY"]);
        Assert.Equal("studio-app", env["WDS_OIDC_CLIENT_ID"]);
        // No secret was given: a public client is a valid answer, not a missing variable.
        Assert.DoesNotContain("WDS_OIDC_CLIENT_SECRET", env.Keys);
        Assert.Equal("https://login.microsoftonline.com/tenant/v2.0",
            studio.Resource.SignInAuthority);
    }

    [Fact]
    public async Task WithSingleSignOn_TakesTheSecretAsAParameter()
    {
        var builder = DistributedApplication.CreateBuilder();
        var secret = builder.AddParameter("oidc-secret", "s3cret", secret: true);

        var studio = builder.AddWebDataStudio()
            .WithSingleSignOn("https://keycloak.example/realms/company", "studio",
                secret, "Sign in with Keycloak", "openid", "profile", "groups");

        var env = await EnvOf(studio.Resource);

        Assert.Equal("s3cret", env["WDS_OIDC_CLIENT_SECRET"]);
        Assert.Equal("Sign in with Keycloak", env["WDS_OIDC_LABEL"]);
        Assert.Equal("openid,profile,groups", env["WDS_OIDC_SCOPES"]);
    }

    [Fact]
    public async Task WithSingleSignOn_AllowsPlainHttpMetadataForAProviderInTheAppHost()
    {
        // A Keycloak in the same app host is on http, which is exactly what an app host runs.
        var env = await EnvOf(Add()
            .WithSingleSignOn("http://localhost:8081/realms/company", "studio").Resource);

        Assert.Equal("false", env["WDS_OIDC_REQUIRE_HTTPS"]);
    }

    [Fact]
    public async Task WithSingleSignOn_LeavesHttpsAloneBecauseItIsTheDefault()
    {
        var env = await EnvOf(Add()
            .WithSingleSignOn("https://login.microsoftonline.com/tenant/v2.0", "studio").Resource);

        Assert.DoesNotContain("WDS_OIDC_REQUIRE_HTTPS", env.Keys);
    }

    [Theory]
    [InlineData("login.microsoftonline.com")]
    [InlineData("tenant")]
    [InlineData("ftp://provider.example")]
    public void WithSingleSignOn_RefusesSomethingThatIsNotAnIssuerUrl(string authority) =>
        Assert.Throws<ArgumentException>(() => Add().WithSingleSignOn(authority, "studio"));

    [Fact]
    public async Task WithSignInRoles_MapsGroupsToTheStudiosOwnRoles()
    {
        var env = await EnvOf(Add()
            .WithSingleSignOn("https://login.microsoftonline.com/tenant/v2.0", "studio")
            .WithSignInRoles(
                admins: ["dba-group", "ada@example.com"],
                editors: ["developers"],
                viewers: ["everyone"],
                defaultRole: StudioRoles.Viewer)
            .Resource);

        Assert.Equal("dba-group,ada@example.com", env["WDS_OIDC_ADMINS"]);
        Assert.Equal("developers", env["WDS_OIDC_EDITORS"]);
        Assert.Equal("everyone", env["WDS_OIDC_VIEWERS"]);
        Assert.Equal("viewer", env["WDS_OIDC_DEFAULT_ROLE"]);
    }

    [Fact]
    public async Task WithSignInRoles_WritesOnlyWhatWasSaid()
    {
        var env = await EnvOf(Add().WithSignInRoles(admins: ["dba-group"]).Resource);

        Assert.Equal("dba-group", env["WDS_OIDC_ADMINS"]);
        Assert.DoesNotContain("WDS_OIDC_EDITORS", env.Keys);
        Assert.DoesNotContain("WDS_OIDC_DEFAULT_ROLE", env.Keys);
    }

    [Fact]
    public void WithSignInRoles_RefusesARoleTheStudioDoesNotHave() =>
        Assert.Throws<ArgumentException>(() => Add().WithSignInRoles(defaultRole: "superuser"));

    [Fact]
    public async Task WithAuditTrail_SaysHowLongTheRecordIsKept()
    {
        var studio = Add().WithAuditTrail(365);
        var env = await EnvOf(studio.Resource);

        Assert.Equal("true", env["WDS_AUDIT"]);
        Assert.Equal("365", env["WDS_AUDIT_DAYS"]);
        Assert.Equal(365, studio.Resource.AuditDays);
    }

    [Fact]
    public void WithAuditTrail_RefusesANumberOfDaysThatIsNotOne() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Add().WithAuditTrail(0));

    [Fact]
    public async Task WithoutAuditTrail_TurnsItOffForADeploymentThatKeepsItsOwn()
    {
        var studio = Add().WithoutAuditTrail();
        var env = await EnvOf(studio.Resource);

        Assert.Equal("false", env["WDS_AUDIT"]);
        Assert.Equal(0, studio.Resource.AuditDays);
    }

    [Fact]
    public async Task ByDefaultTheStudioIsAskedForNeither()
    {
        // The trail is on in the studio itself; an app host that said nothing should not have to
        // repeat that, and no provider means the login form stays as it was.
        var env = await EnvOf(Add().Resource);

        Assert.DoesNotContain("WDS_AUDIT", env.Keys);
        Assert.DoesNotContain("WDS_OIDC_AUTHORITY", env.Keys);
        Assert.Null(Add().Resource.SignInAuthority);
    }
}
