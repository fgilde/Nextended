using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Nextended.Aspire.Hosting.WebDataStudio.Tests;

/// What the studio looks like when the app host is publishing rather than running.
///
/// The one that matters: Aspire turns a named volume into an Azure Files share on Container Apps,
/// and the studio keeps its connections, history and layouts in SQLite. SQLite on SMB either
/// crawls or blocks, and because both of its stores are process-wide, one blocked call took every
/// later request with it — a deployed studio answered /api/connections with a hang and then a 500
/// while everything that did not touch storage kept working. A published studio therefore gets no
/// volume unless the app host asks for one.
public class PublishModeTests
{
    private static IDistributedApplicationBuilder PublishBuilder() =>
        DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--operation", "publish", "--publisher", "manifest", "--output-path", "."],
        });

    private static IEnumerable<ContainerMountAnnotation> MountsOf(IResource resource) =>
        resource.Annotations.OfType<ContainerMountAnnotation>();

    [Fact]
    public void Running_locally_keeps_the_data_volume()
    {
        var studio = DistributedApplication.CreateBuilder().AddWebDataStudio();

        var mount = Assert.Single(MountsOf(studio.Resource));
        Assert.Equal("/data", mount.Target);
        Assert.Equal(ContainerMountType.Volume, mount.Type);
    }

    [Fact]
    public void Publishing_leaves_data_in_the_container()
    {
        var builder = PublishBuilder();
        Assert.True(builder.ExecutionContext.IsPublishMode, "the test builder is not in publish mode");

        var studio = builder.AddWebDataStudio();

        Assert.Empty(MountsOf(studio.Resource));
    }

    [Fact]
    public void Publishing_still_honours_a_volume_the_app_host_asked_for()
    {
        // Opt-in: somebody who knows their share behaves can still have persistence.
        var studio = PublishBuilder().AddWebDataStudio().WithDataVolume("studio-data");

        var mount = Assert.Single(MountsOf(studio.Resource));
        Assert.Equal("/data", mount.Target);
        Assert.Equal("studio-data", mount.Source);
    }

    [Fact]
    public void A_bind_mount_replaces_the_volume_in_either_mode()
    {
        var studio = DistributedApplication.CreateBuilder().AddWebDataStudio()
            .WithDataBindMount("/srv/wds");

        var mount = Assert.Single(MountsOf(studio.Resource));
        Assert.Equal(ContainerMountType.BindMount, mount.Type);
    }
}
