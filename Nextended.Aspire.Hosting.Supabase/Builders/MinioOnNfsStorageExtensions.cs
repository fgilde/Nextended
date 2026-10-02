using System.ComponentModel;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.Supabase.Builders;

/// <summary>
/// Durable object storage for supabase-storage on Azure Container Apps, via a bundled MinIO
/// (S3) server backed by the persistent Azure Files NFS share.
/// </summary>
public static class MinioOnNfsStorageExtensions
{
    // The single S3 bucket that backs all Supabase storage buckets. Must be DNS-compliant.
    private const string Bucket = "supabase-storage";

    /// <summary>
    /// Adds a MinIO server on the NFS share plus a container that creates the bucket, and points
    /// the Supabase storage API at it as its S3 backend.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <param name="nfsEnvStorageName">The managedEnvironmentStorage to mount (the name passed to AddSupabaseNfsStorage).</param>
    /// <param name="rootUser">MinIO root user. MinIO is internal-only, but real deployments should pass their own.</param>
    /// <param name="rootPassword">MinIO root password.</param>
    /// <param name="configureInit">
    /// Configures the bucket-init container, e.g. its image. The server is configured on the
    /// returned builder. Defaults for both: <see cref="MinioContainerImageTags"/>.
    /// </param>
    /// <returns>The MinIO server container.</returns>
    public static IResourceBuilder<ContainerResource> AddMinioS3OnNfs(this IDistributedApplicationBuilder builder,
        string nfsEnvStorageName,
        string rootUser = "minio-admin",
        string rootPassword = "Minio-Nfs-2026-secure!",
        Action<IResourceBuilder<ContainerResource>>? configureInit = null)
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
        var minio = builder.AddContainer("minio", MinioContainerImageTags.Image, MinioContainerImageTags.Tag)
            .WithImageRegistry(MinioContainerImageTags.Registry)
            .WithEnvironment("MINIO_ROOT_USER", rootUser)
            .WithEnvironment("MINIO_ROOT_PASSWORD", rootPassword)
            .WithArgs("server", "/data", "--console-address", ":9001")
            .WithEndpoint(targetPort: 9000, name: "s3", scheme: "http", isExternal: false)
            .WithContainerRuntimeArgs("--restart=on-failure:10");

        minio.PublishAsAcaWhenTargeted((infra, app) =>
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

        var minioInit = builder.AddContainer("minio-init", MinioContainerImageTags.ClientImage, MinioContainerImageTags.ClientTag)
            .WithImageRegistry(MinioContainerImageTags.Registry)
            .WithEntrypoint("/bin/sh")
            .WithArgs("-c", initScript)
            .WithEnvironment("MINIO_ENDPOINT", endpointExpr)
            .WithEnvironment("MINIO_USER", rootUser)
            .WithEnvironment("MINIO_PASS", rootPassword)
            .WithEnvironment("BUCKET", Bucket)
            .WithContainerRuntimeArgs("--restart=on-failure:10")
            .WaitFor(minio);

        minioInit.PublishAsAcaWhenTargeted((infra, app) =>
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

        configureInit?.Invoke(minioInit);
        return minio;
    }

    /// <summary>
    /// The signature of releases before <c>configureInit</c> existed, kept so assemblies compiled
    /// against them still bind. Source callers resolve to the overload above.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IResourceBuilder<ContainerResource> AddMinioS3OnNfs(this IDistributedApplicationBuilder builder,
        string nfsEnvStorageName, string rootUser, string rootPassword)
        => builder.AddMinioS3OnNfs(nfsEnvStorageName, rootUser, rootPassword, configureInit: null);
}
