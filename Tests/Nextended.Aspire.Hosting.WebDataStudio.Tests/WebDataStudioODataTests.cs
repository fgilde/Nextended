using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Nextended.Aspire.Hosting.WebDataStudio.Tests;

/// <summary>
/// An OData service as a connection: the URL of the service root, and the headers a service wants.
/// </summary>
public class WebDataStudioODataTests
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

    [Fact]
    public async Task WithODataService_WritesTheUrlAndTheEngine()
    {
        var studio = Add().WithODataService("NORTHWIND",
            "https://services.odata.org/V4/Northwind/Northwind.svc/", group: "Services");

        var env = await EnvOf(studio.Resource);

        Assert.Equal("https://services.odata.org/V4/Northwind/Northwind.svc/", env["WDS_CONN_NORTHWIND"]);
        Assert.Equal("odata", env["WDS_CONN_NORTHWIND_ENGINE"]);
        Assert.Equal("Services", env["WDS_CONN_NORTHWIND_GROUP"]);
        // The driver only ever reads, so the connection says so rather than letting the UI offer
        // an edit the service would refuse.
        Assert.Equal("true", env["WDS_CONN_NORTHWIND_READONLY"]);
    }

    /// <summary>The studio reads each further line of the connection string as one header.</summary>
    [Fact]
    public async Task WithODataService_PutsEachHeaderOnItsOwnLine()
    {
        var studio = Add().WithODataService("SAP", "https://sap.example/odata/", headers: new()
        {
            ["X-Api-Key"] = "abc123",
            ["Accept-Language"] = "de-DE",
        });

        var lines = (await EnvOf(studio.Resource))["WDS_CONN_SAP"].Split('\n');

        Assert.Equal("https://sap.example/odata/", lines[0]);
        Assert.Contains("X-Api-Key: abc123", lines);
        Assert.Contains("Accept-Language: de-DE", lines);
    }

    [Fact]
    public async Task WithODataService_TakesTheConnectionNameAsTheLabel()
    {
        var studio = Add().WithODataService("TRIPPIN", "https://services.odata.org/TripPin/");

        Assert.Contains("TRIPPIN", studio.Resource.ConnectionNames);
        Assert.True((await EnvOf(studio.Resource)).ContainsKey("WDS_CONN_TRIPPIN"));
    }

    /// <summary>
    /// A URL that is not one is the mistake worth catching here: <c>WithConnection</c> would take it
    /// and the studio would fail on the first click instead.
    /// </summary>
    [Theory]
    [InlineData("services.odata.org/V4/Northwind/")]
    [InlineData("ftp://services.odata.org/")]
    [InlineData("/V4/Northwind/")]
    public void WithODataService_RefusesSomethingThatIsNotAnHttpUrl(string url)
    {
        var error = Assert.Throws<ArgumentException>(() => Add().WithODataService("X", url));

        Assert.Contains("http", error.Message);
    }

    /// <summary>
    /// A newline in a header value would write a second header into the connection string, so it is
    /// refused rather than passed on.
    /// </summary>
    [Fact]
    public void WithODataService_RefusesAHeaderThatWouldSmuggleAnother()
    {
        Assert.Throws<ArgumentException>(() => Add().WithODataService("X", "https://x.example/",
            headers: new() { ["X-Api-Key"] = "abc\nAuthorization: Basic Zm9v" }));

        Assert.Throws<ArgumentException>(() => Add().WithODataService("X", "https://x.example/",
            headers: new() { ["X-Api\r-Key"] = "abc" }));
    }

    [Fact]
    public void WithODataService_RefusesAHeaderWithoutAName()
    {
        Assert.Throws<ArgumentException>(() => Add().WithODataService("X", "https://x.example/",
            headers: new() { ["  "] = "abc" }));
    }

    /// <summary>
    /// A token belongs in a parameter rather than in the app host's source, so the value may arrive
    /// as a reference the stack resolves when it runs.
    /// </summary>
    [Fact]
    public async Task WithODataService_TakesASecretHeaderAsAReference()
    {
        var builder = DistributedApplication.CreateBuilder();
        var key = builder.AddParameter("sap-key", "s3cret", secret: true);

        var studio = builder.AddWebDataStudio()
            .WithODataService("SAP", "https://sap.example/odata/", "X-Api-Key", key);

        var context = new EnvironmentCallbackContext(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));

        foreach (var annotation in studio.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
            await annotation.Callback(context);

        Assert.True(context.EnvironmentVariables.TryGetValue("WDS_CONN_SAP", out var value));

        var resolved = value switch
        {
            string text => text,
            IValueProvider provider => await provider.GetValueAsync(default) ?? "",
            _ => value?.ToString() ?? "",
        };

        Assert.Equal("https://sap.example/odata/\nX-Api-Key: s3cret", resolved);
        Assert.Equal("odata", (await EnvOf(studio.Resource))["WDS_CONN_SAP_ENGINE"]);
    }

    /// <summary>The engine is in the enum, so anything taking one can name an OData service.</summary>
    [Fact]
    public void TheEngineIdIsWhatTheStudioSpells()
    {
        Assert.Equal("odata", WebDataStudioEngine.OData.ToEngineId());
    }
}
