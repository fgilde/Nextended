namespace Nextended.Aspire.Hosting.Supabase.Builders;

/// <summary>
/// Default images of <c>AddMinioS3OnNfs</c> (<see cref="MinioOnNfsStorageExtensions"/>).
/// </summary>
/// <remarks>
/// MinIO stopped publishing images: minio/minio and minio/mc are gone from Docker Hub and quay.io,
/// and dl.min.io answers 410. These are the same releases, rebuilt from the official binaries of
/// the GitHub releases (checksum-verified) — see tools/container-mirror/minio in this repository.
/// Override per container with the standard Aspire APIs: <c>WithImageRegistry</c>,
/// <c>WithImage</c> and <c>WithImageTag</c> on the returned server builder, and inside
/// <c>configureInit</c> for the bucket-init container.
/// </remarks>
public static class MinioContainerImageTags
{
    /// <summary>Registry of both default images.</summary>
    public const string Registry = "ghcr.io";

    /// <summary>MinIO server image.</summary>
    public const string Image = "fgilde/minio";

    /// <summary>MinIO server release.</summary>
    public const string Tag = "RELEASE.2025-09-07T16-13-09Z";

    /// <summary>MinIO client (mc) image used by the bucket-init container.</summary>
    public const string ClientImage = "fgilde/minio-mc";

    /// <summary>MinIO client release.</summary>
    public const string ClientTag = "RELEASE.2025-08-13T08-35-41Z";
}
