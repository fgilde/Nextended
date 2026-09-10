using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Nextended.Aspire.Hosting.WebDataStudio.Tests;

/// <summary>
/// What people may do in the studio: where a connection they make goes, which ways in are open, and
/// the whole viewer stack as one call.
/// </summary>
public class WebDataStudioAccessTests
{
    private static IResourceBuilder<WebDataStudioResource> Add() =>
        DistributedApplication.CreateBuilder().AddWebDataStudio();

    private static async Task<Dictionary<string, string>> EnvOf(IResource resource)
    {
        var context = new EnvironmentCallbackContext(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));

        foreach (var annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
            await annotation.Callback(context);

        return context.EnvironmentVariables
            .Where(pair => pair.Value is string)
            .ToDictionary(pair => pair.Key, pair => (string)pair.Value);
    }

    // --- the two questions -------------------------------------------------------------------------

    /// A public viewer closes the server browser, and a studio whose point is a folder of samples
    /// says the opposite out loud after it. Whichever comes last is what the container gets.
    [Fact]
    public async Task A_viewer_can_be_told_to_show_its_samples_after_all()
    {
        var studio = Add()
            .AsPublicViewer()
            .WithFileBrowse();

        Assert.Equal("true", (await EnvOf(studio.Resource))["WDS_ALLOW_FILE_BROWSE"]);
    }

    [Fact]
    public async Task A_public_viewer_closes_the_server_browser_by_itself()
    {
        var studio = Add().AsPublicViewer();

        Assert.Equal("false", (await EnvOf(studio.Resource))["WDS_ALLOW_FILE_BROWSE"]);
    }

    [Fact]
    public async Task WithConnectionScope_SaysWhereANewConnectionGoes()
    {
        Assert.Equal("session",
            (await EnvOf(Add().WithConnectionScope(ConnectionScope.Session).Resource))["WDS_CONNECTION_SCOPE"]);

        Assert.Equal("stored",
            (await EnvOf(Add().WithConnectionScope(ConnectionScope.Stored).Resource))["WDS_CONNECTION_SCOPE"]);
    }

    /// <summary>A studio that says nothing keeps every way in it has now.</summary>
    [Fact]
    public async Task WithNothingSaid_NoneOfTheSwitchesAreWritten()
    {
        var env = await EnvOf(Add().Resource);

        Assert.DoesNotContain("WDS_CONNECTION_SCOPE", env.Keys);
        Assert.DoesNotContain("WDS_ALLOW_ADD_CONNECTION", env.Keys);
        Assert.DoesNotContain("WDS_ALLOW_FILE_UPLOAD", env.Keys);
        Assert.DoesNotContain("WDS_ALLOW_FILE_BROWSE", env.Keys);
        Assert.DoesNotContain("WDS_CONNECT_HOSTS", env.Keys);
    }

    [Fact]
    public async Task TheThreeDoorsEachCloseOnTheirOwn()
    {
        var studio = Add().WithoutAddingConnections().WithoutFileUpload().WithoutFileBrowse();
        var env = await EnvOf(studio.Resource);

        Assert.Equal("false", env["WDS_ALLOW_ADD_CONNECTION"]);
        Assert.Equal("false", env["WDS_ALLOW_FILE_UPLOAD"]);
        Assert.Equal("false", env["WDS_ALLOW_FILE_BROWSE"]);
    }

    [Fact]
    public async Task WithConnectHosts_WritesTheListTheStudioExpects()
    {
        var studio = Add().WithConnectHosts("db.example", " *.internal.example ");

        Assert.Equal("db.example,*.internal.example",
            (await EnvOf(studio.Resource))["WDS_CONNECT_HOSTS"]);
    }

    /// <summary>An empty list would refuse every connection the studio has.</summary>
    [Fact]
    public void WithConnectHosts_RefusesNoHostsAtAll()
    {
        Assert.Throws<ArgumentException>(() => Add().WithConnectHosts());
        Assert.Throws<ArgumentException>(() => Add().WithConnectHosts("  "));
    }

    [Fact]
    public async Task WithSessionLifetime_AndTheCeiling()
    {
        var env = await EnvOf(Add().WithSessionLifetime(minutes: 30, maxConnections: 3).Resource);

        Assert.Equal("30", env["WDS_SESSION_TTL_MINUTES"]);
        Assert.Equal("3", env["WDS_SESSION_MAX_CONNECTIONS"]);
    }

    /// <summary>Zero is a deployment saying "never", which the studio reads that way.</summary>
    [Fact]
    public async Task WithSessionLifetime_ZeroMeansNever()
    {
        Assert.Equal("0", (await EnvOf(Add().WithSessionLifetime(0).Resource))["WDS_SESSION_TTL_MINUTES"]);
    }

    [Fact]
    public void WithSessionLifetime_RefusesNonsense()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Add().WithSessionLifetime(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Add().WithSessionLifetime(maxConnections: 0));
    }

    [Fact]
    public async Task WithUploadLimit_WritesMegabytes()
    {
        Assert.Equal("25", (await EnvOf(Add().WithUploadLimit(25).Resource))["WDS_UPLOAD_MAX_MB"]);
        Assert.Throws<ArgumentOutOfRangeException>(() => Add().WithUploadLimit(0));
    }

    // --- the whole viewer stack in one call --------------------------------------------------------

    [Fact]
    public async Task AsPublicViewer_SaysAllOfIt()
    {
        var env = await EnvOf(Add().AsPublicViewer().Resource);

        Assert.Equal("session", env["WDS_CONNECTION_SCOPE"]);
        Assert.Equal("false", env["WDS_ALLOW_FILE_BROWSE"]);
        Assert.Equal("true", env["WDS_READONLY"]);
        Assert.Equal("file", env["WDS_OPEN_FROM_URL"]);
        Assert.Equal("120", env["WDS_SESSION_TTL_MINUTES"]);
        Assert.Equal("50", env["WDS_UPLOAD_MAX_MB"]);
        // Uploads stay open unless they are turned off, and the form stays open: a viewer with no
        // way to bring anything is a blank page.
        Assert.DoesNotContain("WDS_ALLOW_FILE_UPLOAD", env.Keys);
        Assert.DoesNotContain("WDS_ALLOW_ADD_CONNECTION", env.Keys);
    }

    [Fact]
    public async Task AsPublicViewer_TakesConnectionStringsOnlyWithHosts()
    {
        var env = await EnvOf(Add()
            .AsPublicViewer(connectionStrings: true, hosts: ["db.example"]).Resource);

        Assert.Equal("file,connection-string", env["WDS_OPEN_FROM_URL"]);
        Assert.Equal("db.example", env["WDS_CONNECT_HOSTS"]);
    }

    /// <summary>
    /// The switch that lets a stranger choose the address is the switch that needs the list.
    /// </summary>
    [Fact]
    public void AsPublicViewer_RefusesConnectionStringsWithoutHosts()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            Add().AsPublicViewer(connectionStrings: true));

        Assert.Contains("hosts", error.Message);
    }

    [Fact]
    public async Task AsPublicViewer_CanCloseTheUploadToo()
    {
        Assert.Equal("false",
            (await EnvOf(Add().AsPublicViewer(upload: false).Resource))["WDS_ALLOW_FILE_UPLOAD"]);
    }

    /// <summary>
    /// A viewer with accounts is a contradiction, and the app host is where to find that out.
    /// </summary>
    [Fact]
    public void AsPublicViewer_RefusesAStudioWithALogin()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            Add().WithLogin("admin", "hunter2").AsPublicViewer());

        Assert.Contains("login", error.Message);
    }

    [Fact]
    public void AsPublicViewer_RefusesAStudioThatBringsItsOwnConnections()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            Add().WithConnection("SHOP", "Data Source=/data/shop.db", WebDataStudioEngine.Sqlite)
                .AsPublicViewer());

        Assert.Contains("connections of its own", error.Message);
    }
}
