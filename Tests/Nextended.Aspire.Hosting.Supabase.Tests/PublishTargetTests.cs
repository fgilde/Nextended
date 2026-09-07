using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Builders;
using Xunit;

namespace Nextended.Aspire.Hosting.Supabase.Tests;

/// <summary>
/// <see cref="SupabaseBuilderExtensions.PublishTarget"/> decides whether the stack stamps
/// Azure Container Apps annotations. The default must stay ACA (previous behaviour); with
/// a generic container environment the annotations have to stay away, otherwise publishing
/// fails with "configured to publish as an Azure Container App, but there are no
/// AzureContainerAppEnvironmentResource resources".
/// </summary>
[Collection(SupabaseModelCollection.Name)]
public class PublishTargetTests : IDisposable
{
    public PublishTargetTests() => SupabaseBuilderExtensions.PublishTarget = SupabasePublishTarget.AzureContainerApps;

    // Static switch: always restore the default so test order cannot leak.
    public void Dispose() => SupabaseBuilderExtensions.PublishTarget = SupabasePublishTarget.AzureContainerApps;

    private static IDistributedApplicationBuilder PublishBuilder() =>
        DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--operation", "publish", "--publisher", "manifest"],
        });

    private static int AcaAnnotationCount(IDistributedApplicationBuilder builder) =>
        builder.Resources
            .SelectMany(r => r.Annotations)
            .Count(a => a.GetType().Name.Contains("ContainerApp", StringComparison.Ordinal));

    [Fact]
    public void Default_is_azure_container_apps()
    {
        Assert.Equal(SupabasePublishTarget.AzureContainerApps, SupabaseBuilderExtensions.PublishTarget);
    }

    [Fact]
    public void Azure_target_stamps_container_app_annotations()
    {
        var builder = PublishBuilder();
        builder.AddSupabase("sb");

        Assert.True(AcaAnnotationCount(builder) > 0, "no Azure Container Apps annotations were applied");
    }

    [Fact]
    public void Container_environment_target_stamps_none()
    {
        SupabaseBuilderExtensions.PublishTarget = SupabasePublishTarget.ContainerEnvironment;

        var builder = PublishBuilder();
        builder.AddSupabase("sb");

        Assert.Equal(0, AcaAnnotationCount(builder));
    }

    [Fact]
    public void Container_environment_target_builds_the_same_stack_minus_the_azure_environment()
    {
        static string[] StackResources(IDistributedApplicationBuilder builder) =>
            builder.Resources.Select(r => r.Name).Where(n => n.StartsWith("sb", StringComparison.Ordinal))
                .OrderBy(n => n, StringComparer.Ordinal).ToArray();

        var azure = PublishBuilder();
        azure.AddSupabase("sb");
        var azureStack = StackResources(azure);
        // The Azure Container Apps annotations make Aspire add its own environment resource.
        Assert.Contains("azure-environment", azure.Resources.Select(r => r.Name));

        SupabaseBuilderExtensions.PublishTarget = SupabasePublishTarget.ContainerEnvironment;
        var container = PublishBuilder();
        container.AddSupabase("sb");

        // Every Supabase resource is identical — only the implicit Azure environment is gone,
        // which is exactly what lets another compute environment publish this stack.
        Assert.Equal(azureStack, StackResources(container));
        Assert.DoesNotContain("azure-environment", container.Resources.Select(r => r.Name));
    }
}
