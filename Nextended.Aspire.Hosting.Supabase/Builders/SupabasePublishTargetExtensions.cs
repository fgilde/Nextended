using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AppContainers;
using Azure.Provisioning.AppContainers;

namespace Nextended.Aspire.Hosting.Supabase.Builders;

/// <summary>Where <c>aspire publish</c> should deploy the Supabase stack to.</summary>
public enum SupabasePublishTarget
{
    /// <summary>Azure Container Apps (default, unchanged behaviour).</summary>
    AzureContainerApps,

    /// <summary>
    /// A generic container environment such as <c>AddDockerComposeEnvironment(...)</c>.
    /// The stack then emits no Azure Container Apps annotations, so publishing works
    /// without an <c>AddAzureContainerAppEnvironment(...)</c> in the model.
    /// </summary>
    ContainerEnvironment,
}

/// <summary>
/// Applies Azure Container Apps annotations only when the stack actually targets ACA.
/// Every Supabase resource used to call <c>PublishAsAzureContainerApp(...)</c> unconditionally in
/// publish mode, which made any other compute environment fail validation with
/// "configured to publish as an Azure Container App, but there are no
/// AzureContainerAppEnvironmentResource resources".
/// </summary>
internal static class SupabasePublishTargetExtensions
{
    internal static IResourceBuilder<T> PublishAsAcaWhenTargeted<T>(
        this IResourceBuilder<T> builder,
        Action<AzureResourceInfrastructure, ContainerApp> configure)
        where T : ContainerResource
    {
        return SupabaseBuilderExtensions.PublishTarget == SupabasePublishTarget.AzureContainerApps
            ? builder.PublishAsAzureContainerApp(configure)
            : builder;
    }
}
