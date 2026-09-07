using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Nextended.Aspire.Hosting.WebDataStudio.Tests;

/// <summary>
/// Two switches said in the app host rather than in six environment variables: which folders the
/// studio may read database files from, and what its own URL is allowed to open.
/// </summary>
public class WebDataStudioUrlTests
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
    public async Task WithOpenFromUrl_WritesTheWordsTheStudioExpects()
    {
        var studio = Add().WithOpenFromUrl(files: true, downloads: true, connectionStrings: false,
            hosts: ["data.example", "*.blob.core.windows.net"], keep: UrlConnections.Session,
            writable: false, maxMegabytes: 64);

        var env = await EnvOf(studio.Resource);

        Assert.Equal("file,download", env["WDS_OPEN_FROM_URL"]);
        Assert.Equal("data.example,*.blob.core.windows.net", env["WDS_OPEN_FROM_URL_HOSTS"]);
        Assert.Equal("session", env["WDS_OPEN_FROM_URL_KEEP"]);
        Assert.Equal("false", env["WDS_OPEN_FROM_URL_WRITABLE"]);
        Assert.Equal("64", env["WDS_OPEN_FROM_URL_MAX_MB"]);
    }

    [Fact]
    public async Task WithOpenFromUrl_TakesAConnectionStringOnlyWhenItIsAskedForByName()
    {
        var studio = Add().WithOpenFromUrl(files: false, connectionStrings: true);

        Assert.Equal("connection-string", (await EnvOf(studio.Resource))["WDS_OPEN_FROM_URL"]);
    }

    [Fact]
    public async Task WithOpenFromUrl_KeepsThemInTheStoreWhenAsked()
    {
        var studio = Add().WithOpenFromUrl(keep: UrlConnections.Store, writable: true);

        var env = await EnvOf(studio.Resource);

        Assert.Equal("store", env["WDS_OPEN_FROM_URL_KEEP"]);
        Assert.Equal("true", env["WDS_OPEN_FROM_URL_WRITABLE"]);
    }

    /// <summary>
    /// A fetcher that takes its address from a link is a way to reach the addresses only this
    /// network can, so the app host says no while the stack is being described rather than the
    /// studio saying no to every link later.
    /// </summary>
    [Fact]
    public void WithOpenFromUrl_RefusesADownloadWithoutHosts()
    {
        var error = Assert.Throws<ArgumentException>(() => Add().WithOpenFromUrl(downloads: true));

        Assert.Contains("hosts", error.Message);
    }

    [Fact]
    public void WithOpenFromUrl_RefusesAllowingNothing()
    {
        Assert.Throws<ArgumentException>(() =>
            Add().WithOpenFromUrl(files: false, downloads: false, connectionStrings: false));
    }

    [Fact]
    public void WithOpenFromUrl_RefusesAMeaninglessSizeCap()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Add().WithOpenFromUrl(maxMegabytes: 0));
    }

    [Fact]
    public async Task WithDatabaseFiles_MountsTheFolderAndNamesItAsARoot()
    {
        var folder = Directory.CreateTempSubdirectory("wds-file-roots").FullName;

        try
        {
            var studio = Add().WithDatabaseFiles(folder);
            var env = await EnvOf(studio.Resource);

            Assert.Equal("/data/files/database-files", env["WDS_FILE_ROOTS"]);

            var mount = Assert.Single(studio.Resource.Annotations.OfType<ContainerMountAnnotation>(),
                m => m.Target == "/data/files/database-files");

            Assert.Equal(folder, mount.Source);
            Assert.True(mount.IsReadOnly);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    /// <summary>Two folders are two roots, and the second does not write over the first.</summary>
    [Fact]
    public async Task WithDatabaseFiles_KeepsWhatAnEarlierCallNamed()
    {
        var first = Directory.CreateTempSubdirectory("wds-file-roots-a").FullName;
        var second = Directory.CreateTempSubdirectory("wds-file-roots-b").FullName;

        try
        {
            var studio = Add().WithDatabaseFiles(first).WithDatabaseFiles(second, name: "exports");

            Assert.Equal("/data/files/database-files,/data/files/exports",
                (await EnvOf(studio.Resource))["WDS_FILE_ROOTS"]);
        }
        finally
        {
            Directory.Delete(first, true);
            Directory.Delete(second, true);
        }
    }

    [Fact]
    public void WithDatabaseFiles_RefusesAFolderThatIsNotThere()
    {
        Assert.Throws<DirectoryNotFoundException>(() =>
            Add().WithDatabaseFiles(Path.Combine(Path.GetTempPath(), "wds-not-there-" + Guid.NewGuid())));
    }
}
