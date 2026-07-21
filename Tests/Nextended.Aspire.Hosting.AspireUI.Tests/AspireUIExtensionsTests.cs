using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.AspireUI;
using Xunit;

namespace Nextended.Aspire.Hosting.AspireUI.Tests;

public class AspireUIExtensionsTests
{
    private static IResourceBuilder<AspireUIResource> Add()
        => DistributedApplication.CreateBuilder().AddAspireUI();

    [Fact]
    public void AddAspireUI_CreatesContainer_WithImageEndpointSocketAndVolume()
    {
        var res = Add().Resource;

        Assert.Equal("aspireui", res.Name);

        var img = Assert.Single(res.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Contains("aspireui", img.Image);
        Assert.Equal("latest", img.Tag);

        var http = Assert.Single(res.Annotations.OfType<EndpointAnnotation>(), e => e.Name == "http");
        Assert.Equal(8080, http.TargetPort);

        var mounts = res.Annotations.OfType<ContainerMountAnnotation>().ToList();
        Assert.Contains(mounts, m => (m.Source ?? "").Contains("docker.sock"));       // host docker socket
        Assert.Contains(mounts, m => m.Target == "/data");                            // data volume
    }

    [Fact]
    public void WithAdminUser_RecordsUsername()
    {
        var res = Add().WithAdminUser("admin", "change-me-please").Resource;
        Assert.Equal("admin", res.AdminUsername);
    }

    [Fact]
    public void WithSeedStack_RecordsNameAndProjects()
    {
        var res = Add().WithSeedStack("Demo", @"C:\src\Api\Api.csproj", @"C:\src\Worker\Worker.csproj").Resource;
        Assert.Equal("Demo", res.SeedStackName);
        Assert.Equal(2, res.SeedProjects.Count);
        Assert.Contains(res.SeedProjects, p => p.Contains("Api.csproj"));
    }

    [Fact]
    public void CustomNameAndImage_Honored()
    {
        var res = DistributedApplication.CreateBuilder().AddAspireUI("ui", image: "myrepo/aspireui", tag: "v1").Resource;
        Assert.Equal("ui", res.Name);
        var img = Assert.Single(res.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("myrepo/aspireui", img.Image);
        Assert.Equal("v1", img.Tag);
    }
}
