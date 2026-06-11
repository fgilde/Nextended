using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.Supabase.Builders;

/// <summary>
/// Durable object storage for supabase-storage on Azure Container Apps, via a bundled MinIO
/// (S3) server backed by the persistent Azure Files NFS share.
///
/// supabase-storage's FILE backend cannot run durably on ACA: the only persistent volume ACA
/// can mount is Azure Files, and SMB rejects the backend's open flags (EINVAL) while NFS 4.1
/// has no extended attributes (xattr -> ENOTSUP), which the FILE backend requires for object
/// metadata. So instead we run MinIO on the NFS share (MinIO keeps its metadata in its own
/// xl.meta files — no xattr) and switch supabase-storage to the S3 backend pointing at it.
///
/// This wires (publish only):
///   1. a MinIO container, mounting the NFS env-storage named <paramref name="nfsEnvStorageName"/>
///      (created by PersistentNfsStorageExtensions.AddSupabaseNfsStorage — pass the SAME name)
///      at /data, internal-only S3 ingress on :9000, pinned to a single writer,
///   2. a one-shot mc init container that waits for MinIO and creates the bucket (idempotent),
///   3. <see cref="SupabaseBuilderExtensions.StorageS3Backend"/> pointing the storage container at MinIO.
///
/// MinIO/mc are public images, so ACA pulls them directly (no local build/push, unaffected by
/// the docker registry push path). NOTE: MinIO discourages network filesystems for large
/// clusters, but a single-node single-drive instance on NFS is fine for this low-traffic app.
/// </summary>
public static class MinioOnNfsStorageExtensions
{
    private const string MinioImage = "minio/minio";
    private const string MinioTag = "RELEASE.2025-09-07T16-13-09Z";
    private const string McImage = "minio/mc";
    private const string McTag = "RELEASE.2025-08-13T08-35-41Z";

    // The single S3 bucket that backs all Supabase storage buckets. Must be DNS-compliant.
    private const string Bucket = "supabase-storage";

    // Root-credential defaults: MinIO is internal-only (no external ingress), so these never
    // leave the ACA environment — but real deployments should still pass their own values.

    public static IResourceBuilder<ContainerResource> AddMinioS3OnNfs(this IDistributedApplicationBuilder builder,
        string nfsEnvStorageName,
        string rootUser = "minio-admin",
        string rootPassword = "Minio-Nfs-2026-secure!")
    {
        if (string.IsNullOrWhiteSpace(nfsEnvStorageName))
        {
            // An empty name would silently emit storageName: '' in the volume bicep and fail at
            // deploy (or worse, leave MinIO without its persistent mount). Fail fast instead.
            throw new ArgumentException(
                "nfsEnvStorageName must be the name of the managedEnvironmentStorage to mount " +
                "(the same name passed to AddSupabaseNfsStorage).", nameof(nfsEnvStorageName));
        }

        // 1) MinIO server, backed by the NFS share.
        var minio = builder.AddContainer("minio", MinioImage, MinioTag)
            .WithEnvironment("MINIO_ROOT_USER", rootUser)
            .WithEnvironment("MINIO_ROOT_PASSWORD", rootPassword)
            .WithArgs("server", "/data", "--console-address", ":9001")
            .WithEndpoint(targetPort: 9000, name: "s3", scheme: "http", isExternal: false)
            .WithContainerRuntimeArgs("--restart=on-failure:10");

        minio.PublishAsAzureContainerApp((infra, app) =>
        {
            app.Configuration.Ingress.AllowInsecure = true;
            app.Configuration.Ingress.Transport =
                Azure.Provisioning.AppContainers.ContainerAppIngressTransportMethod.Http;
            // Single-node single-drive on a network share -> exactly one writer.
            app.Template.Scale.MinReplicas = 1;
            app.Template.Scale.MaxReplicas = 1;

            const string volName = "minio-data";
            app.Template.Volumes.Add(new Azure.Provisioning.AppContainers.ContainerAppVolume
            {
                Name = volName,
                StorageType = Azure.Provisioning.AppContainers.ContainerAppStorageType.NfsAzureFile,
                StorageName = nfsEnvStorageName,
            });
            app.Template.Containers[0].Value.VolumeMounts.Add(
                new Azure.Provisioning.AppContainers.ContainerAppVolumeMount
                {
                    VolumeName = volName,
                    MountPath = "/data",
                });
        });

        // Internal S3 endpoint the storage container + init job connect to. Use the endpoint's
        // OWN url (resolves to https://minio.internal.<env-domain> in ACA — the same form the
        // other internal supabase services use, TLS-terminated at the ingress and forwarded to
        // :9000), NOT a manual host:port which would wrongly emit http:// on the :443 ingress.
        var s3Ep = minio.GetEndpoint("s3");
        var endpointExpr = ReferenceExpression.Create($"{s3Ep}");

        // 2) One-shot bucket bootstrap (supabase-storage's S3 backend never creates the bucket).
        //    Loop mc mb until MinIO is reachable, then idle so ACA keeps the (healthy) replica
        //    instead of restart-looping a short-lived container. Idempotent on restart.
        //    --insecure: the internal ingress cert isn't worth verifying for in-env traffic.
        const string initScript =
            "mc --insecure alias set m \"$MINIO_ENDPOINT\" \"$MINIO_USER\" \"$MINIO_PASS\" >/dev/null 2>&1; " +
            "until mc --insecure mb --ignore-existing m/\"$BUCKET\"; do echo 'waiting for minio...'; sleep 2; done; " +
            "echo 'bucket ready'; tail -f /dev/null";

        var minioInit = builder.AddContainer("minio-init", McImage, McTag)
            .WithEntrypoint("/bin/sh")
            .WithArgs("-c", initScript)
            .WithEnvironment("MINIO_ENDPOINT", endpointExpr)
            .WithEnvironment("MINIO_USER", rootUser)
            .WithEnvironment("MINIO_PASS", rootPassword)
            .WithEnvironment("BUCKET", Bucket)
            .WithContainerRuntimeArgs("--restart=on-failure:10")
            .WaitFor(minio);

        minioInit.PublishAsAzureContainerApp((infra, app) =>
        {
            app.Template.Scale.MinReplicas = 1;
            app.Template.Scale.MaxReplicas = 1;
        });

        // 3) Point supabase-storage's S3 backend at MinIO (read in AddSupabase, so this must be
        //    set before builder.AddSupabase(...) is called).
        SupabaseBuilderExtensions.StorageS3Backend = new SupabaseBuilderExtensions.SupabaseStorageS3Options
        {
            Endpoint = endpointExpr,
            Bucket = Bucket,
            AccessKey = rootUser,
            SecretKey = rootPassword,
            Region = "us-east-1",
            ForcePathStyle = true,
        };

        return minio;

    }
}
