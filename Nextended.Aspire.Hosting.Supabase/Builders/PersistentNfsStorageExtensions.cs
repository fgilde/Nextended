using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AppContainers;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Network;
using Azure.Provisioning.Storage;
using System.Linq;
using StorageFileShare = Azure.Provisioning.Storage.FileShare;

// Azure.Provisioning.Network.* is gated behind an experimental diagnostic (AZPROVISION001).
// We use it deliberately (VNet/subnet for ACA + NFS); suppress for this whole file.
#pragma warning disable AZPROVISION001

namespace Nextended.Aspire.Hosting.Supabase.Builders;

/// <summary>
/// Persistent file storage for the Supabase storage container on Azure Container Apps.
///
/// supabase-storage uses the local FILE backend. Azure Files SMB is incompatible with it
/// (EINVAL on the backend's open flags) and ephemeral disk loses uploads on every restart.
/// The only Azure-native, in-region, POSIX option that works is an Azure Files **NFS** share —
/// which is only reachable from a VNet. This wires, entirely in code (all in the ACA
/// environment's own bicep module so the resources cross-reference directly):
///   1. a VNet + a subnet delegated to the ACA managed environment (+ a Microsoft.Storage
///      service endpoint), and the env's VnetConfiguration pointing at that subnet,
///   2. a Premium FileStorage account with an NFS file share, locked to the subnet
///      (NFS is unencrypted, so Azure only allows it network-isolated),
///   3. a managedEnvironmentStorage (NfsAzureFile) on the ACA environment, named
///      <paramref name="nfsEnvStorageName"/>.
/// Consumers mount that env-storage by name — either the storage container itself (via
/// <see cref="SupabaseBuilderExtensions.PersistentStorageVolumeName"/>; note Azure Files NFS
/// has no xattr, which the FILE backend needs) or a MinIO fronting it (see
/// MinioOnNfsStorageExtensions). Files survive restarts and redeploys.
///
/// NOTE: VNet/subnet are built with Azure.Provisioning.Network directly rather than Aspire's
/// experimental AddSubnet — the latter emits an invalid fully-qualified child name (BCP170).
/// </summary>
public static class PersistentNfsStorageExtensions
{
    public static void AddSupabaseNfsStorage(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<AzureContainerAppEnvironmentResource> containerEnv,
        string nfsEnvStorageName, string shareName = "supabasestorage", string vnetName = "supabaseStorageVnet", int shareQuotaGiB = 100,
        string? postgresEnvStorageName = null, string postgresShareName = "postgresdata", int postgresShareQuotaGiB = 100)
    {
        containerEnv.ConfigureInfrastructure(infra =>
        {
            var env = infra.GetProvisionableResources()
                .OfType<ContainerAppManagedEnvironment>()
                .Single();

            // 1) VNet + subnet delegated to the ACA managed environment. A Microsoft.Storage
            //    service endpoint lets us lock the NFS storage account to this subnet.

            // Bicep identifiers only allow [A-Za-z0-9_], but callers pass real Azure resource
            // names like "promote-vnet". Normalize the identifier and set the Name explicitly.
            var vnet = new VirtualNetwork(Azure.Provisioning.Infrastructure.NormalizeBicepIdentifier(vnetName))
            {
                Name = vnetName
            };
            vnet.AddressSpace = new VirtualNetworkAddressSpace();
            vnet.AddressSpace.AddressPrefixes.Add("10.10.0.0/16");
            infra.Add(vnet);

            var subnet = new SubnetResource("acaInfra")
            {
                Parent = vnet,
                AddressPrefix = "10.10.0.0/23",
            };
            subnet.Delegations.Add(new ServiceDelegation
            {
                Name = "aca-environments",
                ServiceName = "Microsoft.App/environments",
            });
            subnet.ServiceEndpoints.Add(new ServiceEndpointProperties { Service = "Microsoft.Storage" });
            infra.Add(subnet);

            // Put the ACA environment into the subnet.
            env.VnetConfiguration = new ContainerAppVnetConfiguration
            {
                InfrastructureSubnetId = subnet.Id,
            };

            // 2) Premium FileStorage account + NFS share, locked to the subnet.
            var storage = new StorageAccount("supabaseNfsStorage")
            {
                Kind = StorageKind.FileStorage,
                Sku = new StorageSku { Name = StorageSkuName.PremiumLrs },
                EnableHttpsTrafficOnly = false, // NFS is unencrypted in transit
            };
            storage.NetworkRuleSet = new StorageAccountNetworkRuleSet
            {
                DefaultAction = StorageNetworkDefaultAction.Deny,
            };
            storage.NetworkRuleSet.VirtualNetworkRules.Add(new StorageAccountVirtualNetworkRule
            {
                VirtualNetworkResourceId = subnet.Id,
            });
            infra.Add(storage);

            var fileService = new FileService("default") { Parent = storage };
            infra.Add(fileService);

            var share = new StorageFileShare(Azure.Provisioning.Infrastructure.NormalizeBicepIdentifier(shareName))
            {
                Parent = fileService,
                Name = shareName,
                EnabledProtocol = FileShareEnabledProtocol.Nfs,
                ShareQuota = shareQuotaGiB,
            };
            infra.Add(share);

            // 3) managedEnvironmentStorage (NFS) on the ACA environment.
            var envStorage = new ContainerAppManagedEnvironmentStorage(Azure.Provisioning.Infrastructure.NormalizeBicepIdentifier(nfsEnvStorageName))
            {
                Parent = env,
                Name = nfsEnvStorageName,
                Properties = new ManagedEnvironmentStorageProperties
                {
                    NfsAzureFile = new ContainerAppNfsAzureFileProperties
                    {
                        Server = BicepFunction.Concat(storage.Name, ".file.core.windows.net"),
                        ShareName = BicepFunction.Concat("/", storage.Name, "/", shareName),
                        AccessMode = ContainerAppAccessMode.ReadWrite,
                    },
                },
            };
            infra.Add(envStorage);

            // Optional 2nd share on the SAME account for the PostgreSQL data directory. Kept
            // separate from the storage share so the DB cluster files and the object store never
            // share a filesystem tree. Consumed via SupabaseBuilderExtensions.PostgresDataVolumeName.
            if (!string.IsNullOrEmpty(postgresEnvStorageName))
            {
                var pgShare = new StorageFileShare("postgresDataShare")
                {
                    Parent = fileService,
                    Name = postgresShareName,
                    EnabledProtocol = FileShareEnabledProtocol.Nfs,
                    ShareQuota = postgresShareQuotaGiB,
                };
                infra.Add(pgShare);

                var pgEnvStorage = new ContainerAppManagedEnvironmentStorage(postgresEnvStorageName)
                {
                    Parent = env,
                    Name = postgresEnvStorageName,
                    Properties = new ManagedEnvironmentStorageProperties
                    {
                        NfsAzureFile = new ContainerAppNfsAzureFileProperties
                        {
                            Server = BicepFunction.Concat(storage.Name, ".file.core.windows.net"),
                            ShareName = BicepFunction.Concat("/", storage.Name, "/", postgresShareName),
                            AccessMode = ContainerAppAccessMode.ReadWrite,
                        },
                    },
                };
                infra.Add(pgEnvStorage);
            }
        });
    }
}
