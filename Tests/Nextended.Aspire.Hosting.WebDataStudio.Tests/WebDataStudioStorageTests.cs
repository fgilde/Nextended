using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Nextended.Aspire.Hosting.WebDataStudio.Tests;

/// <summary>
/// Object storage as a connection: a URL the app host writes down, or a blob resource it already
/// models.
/// </summary>
public class WebDataStudioStorageTests
{
    /// <summary>Stands in for a blob resource: what matters is its connection string.</summary>
    private sealed class FakeBlobResource(string name, string connectionString)
        : Resource(name), IResourceWithConnectionString
    {
        public ReferenceExpression ConnectionStringExpression =>
            ReferenceExpression.Create($"{connectionString}");
    }

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

    /// <summary>
    /// The value of one variable, resolved. A URL built from a resource's connection string arrives
    /// as a reference expression rather than a string, and the shape of it is the thing to check.
    /// </summary>
    private static async Task<string> ValueOf(IResource resource, string key)
    {
        var context = new EnvironmentCallbackContext(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));

        foreach (var annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
            await annotation.Callback(context);

        Assert.True(context.EnvironmentVariables.TryGetValue(key, out var value), key);

        return value switch
        {
            string text => text,
            IValueProvider provider => await provider.GetValueAsync(default) ?? "",
            _ => value?.ToString() ?? "",
        };
    }

    [Fact]
    public async Task WithStorage_WritesTheUrlAndTheStorageEngine()
    {
        var studio = Add().WithStorage("LAKE", "s3://bucket/exports?region=eu-central-1");

        var env = await EnvOf(studio.Resource);

        Assert.Equal("s3://bucket/exports?region=eu-central-1", env["WDS_CONN_LAKE"]);
        Assert.Equal("storage", env["WDS_CONN_LAKE_ENGINE"]);
    }

    [Fact]
    public async Task WithStorage_CarriesReadOnlyAndTheProductionColour()
    {
        var studio = Add().WithStorage("ARCHIVE", "gs://bucket/2026",
            readOnly: true, group: "lakes", color: "#e03131");

        var env = await EnvOf(studio.Resource);

        // Both refuse an upload and a delete in the studio; the colour is also what marks it in
        // the explorer.
        Assert.Equal("true", env["WDS_CONN_ARCHIVE_READONLY"]);
        Assert.Equal("#e03131", env["WDS_CONN_ARCHIVE_COLOR"]);
        Assert.Equal("lakes", env["WDS_CONN_ARCHIVE_GROUP"]);
    }

    [Theory]
    [InlineData("azblob://account/container")]
    [InlineData("file:///data/incoming")]
    [InlineData("gcs://bucket")]
    public void WithStorage_AcceptsEveryProvidersUrl(string url) =>
        Add().WithStorage("DROP", url);

    [Theory]
    [InlineData("Host=db;Database=shop")]
    [InlineData("https://example.com/not-storage")]
    [InlineData("bucket")]
    public void WithStorage_RefusesSomethingThatIsNotAStorageUrl(string url) =>
        // Attaching a database URL as storage would produce a connection that fails on first use,
        // which is a worse way to find out than an exception in the app host.
        Assert.Throws<ArgumentException>(() => Add().WithStorage("LAKE", url));

    [Fact]
    public async Task WithStorage_TakesAUrlThatIsOnlyKnownOnceTheStackRuns()
    {
        var builder = DistributedApplication.CreateBuilder();
        var keys = builder.AddResource(new FakeBlobResource("keys", "wds-secret"));

        // A MinIO in the same app host: its endpoint and keys are resources, not literals.
        var studio = builder.AddWebDataStudio()
            .WithStorage("LAKE", ReferenceExpression.Create(
                $"s3://lake?access=demo&secret={keys.Resource.ConnectionStringExpression}"));

        Assert.Equal("s3://lake?access=demo&secret=wds-secret",
            await ValueOf(studio.Resource, "WDS_CONN_LAKE"));
        Assert.Equal("storage", (await EnvOf(studio.Resource))["WDS_CONN_LAKE_ENGINE"]);
    }

    [Fact]
    public async Task WithSchemas_LimitsWhatAConnectionReads()
    {
        var studio = Add().WithStorage("LAKE", "s3://bucket").WithSchemas("shop", "public", "sales");

        var env = await EnvOf(studio.Resource);

        Assert.Equal("public,sales", env["WDS_CONN_SHOP_SCHEMAS"]);
    }

    [Fact]
    public async Task WithSchemas_WithNothingNamedReadsEverything()
    {
        var studio = Add().WithSchemas("shop");

        // No variable rather than an empty one: empty already means "all of them" in the studio, and
        // writing it down would only invite the question.
        Assert.DoesNotContain("WDS_CONN_SHOP_SCHEMAS", (await EnvOf(studio.Resource)).Keys);
    }

    [Fact]
    public async Task WithExportTemplates_MountsTheFolderReadOnly()
    {
        var directory = Directory.CreateTempSubdirectory("wds-templates").FullName;

        try
        {
            var studio = Add().WithExportTemplates(directory);

            var mount = Assert.Single(studio.Resource.Annotations.OfType<ContainerMountAnnotation>(),
                annotation => annotation.Target == "/data/export-templates");

            // The studio reads these; it does not own them.
            Assert.True(mount.IsReadOnly);
            Assert.Equal("/data/export-templates",
                (await EnvOf(studio.Resource))["WDS_EXPORT_TEMPLATES_DIR"]);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task WithBlobStorage_TakesTheContainerFromTheResourceAndTheAccountFromItsConnection()
    {
        var builder = DistributedApplication.CreateBuilder();
        var blobs = builder.AddResource(new FakeBlobResource("exports",
            "DefaultEndpointsProtocol=https;AccountName=acct;AccountKey=k+/="));

        var studio = builder.AddWebDataStudio().WithBlobStorage(blobs);

        Assert.Equal(
            "azblob:///exports?connectionstring=DefaultEndpointsProtocol=https;AccountName=acct;AccountKey=k+/=",
            await ValueOf(studio.Resource, "WDS_CONN_EXPORTS"));
        Assert.Equal("storage", (await EnvOf(studio.Resource))["WDS_CONN_EXPORTS_ENGINE"]);
    }

    [Fact]
    public async Task WithBlobStorage_CanOpenAtAPrefixAndUnderAnotherName()
    {
        var builder = DistributedApplication.CreateBuilder();
        var blobs = builder.AddResource(new FakeBlobResource("blobs",
            "https://acct.blob.core.windows.net/"));

        var studio = builder.AddWebDataStudio()
            .WithBlobStorage(blobs, container: "exports", connectionName: "LAKE", prefix: "2026/08");

        // A service URI rather than a connection string: what a deployed blob resource hands over,
        // and what makes the studio use its own managed identity.
        Assert.Equal("azblob:///exports/2026/08?connectionstring=https://acct.blob.core.windows.net/",
            await ValueOf(studio.Resource, "WDS_CONN_LAKE"));
    }
}
