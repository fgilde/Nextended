using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Builders;
using Xunit;

namespace Nextended.Aspire.Hosting.Supabase.Tests;

/// <summary>
/// MinIO stopped publishing images (Docker Hub and quay.io both dropped minio/minio and
/// minio/mc), and a registry reports an unknown repository as 401 — Azure Container Apps failed
/// with "authentication required". The defaults now point at a mirror, and both containers can
/// be pointed elsewhere with the standard Aspire image APIs.
/// </summary>
[Collection(SupabaseModelCollection.Name)]
public class MinioImageTests : IDisposable
{
    // AddMinioS3OnNfs sets this static so AddSupabase picks up the S3 backend; never let it leak.
    public void Dispose() => SupabaseBuilderExtensions.StorageS3Backend = null;

    private static IDistributedApplicationBuilder Builder() =>
        DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = [] });

    private static string ImageOf(IDistributedApplicationBuilder builder, string resource)
    {
        var image = builder.Resources.Single(r => r.Name == resource)
            .Annotations.OfType<ContainerImageAnnotation>().Single();
        return $"{image.Registry}/{image.Image}:{image.Tag}";
    }

    [Fact]
    public void Both_containers_default_to_the_mirrored_releases()
    {
        var builder = Builder();
        builder.AddMinioS3OnNfs("nfs");

        Assert.Equal("ghcr.io/fgilde/minio:RELEASE.2025-09-07T16-13-09Z", ImageOf(builder, "minio"));
        Assert.Equal("ghcr.io/fgilde/minio-mc:RELEASE.2025-08-13T08-35-41Z", ImageOf(builder, "minio-init"));
    }

    [Fact]
    public void The_server_image_is_overridden_on_the_returned_builder()
    {
        var builder = Builder();
        builder.AddMinioS3OnNfs("nfs")
            .WithImageRegistry("registry.example")
            .WithImage("team/minio")
            .WithImageTag("2026.1");

        Assert.Equal("registry.example/team/minio:2026.1", ImageOf(builder, "minio"));
        // The init container keeps its own default.
        Assert.Equal("ghcr.io/fgilde/minio-mc:RELEASE.2025-08-13T08-35-41Z", ImageOf(builder, "minio-init"));
    }

    [Fact]
    public void The_init_image_is_overridden_through_configureInit()
    {
        var builder = Builder();
        builder.AddMinioS3OnNfs("nfs", configureInit: init => init
            .WithImageRegistry("registry.example")
            .WithImage("team/mc")
            .WithImageTag("2026.1"));

        Assert.Equal("registry.example/team/mc:2026.1", ImageOf(builder, "minio-init"));
        Assert.Equal("ghcr.io/fgilde/minio:RELEASE.2025-09-07T16-13-09Z", ImageOf(builder, "minio"));
    }

    [Fact]
    public void A_call_without_configureInit_still_binds_and_keeps_the_credentials()
    {
        // The positional three-argument call of earlier releases must keep compiling and behaving.
        var builder = Builder();
        builder.AddMinioS3OnNfs("nfs", "custom-admin", "custom-password");

        Assert.NotNull(SupabaseBuilderExtensions.StorageS3Backend);
        Assert.Equal("custom-admin", SupabaseBuilderExtensions.StorageS3Backend!.AccessKey);
        Assert.Equal("custom-password", SupabaseBuilderExtensions.StorageS3Backend.SecretKey);
    }
}
