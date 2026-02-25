using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Resources;

namespace Nextended.Aspire.Hosting.Supabase.Builders;

/// <summary>
/// Provides extension methods for configuring the Supabase Storage API.
/// </summary>
public static class StorageBuilderExtensions
{
    #region Direct Stack Methods (Aspire-Standard Pattern)

    /// <summary>
    /// Sets the maximum file size limit in bytes.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="bytes">The maximum file size in bytes.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> WithStorageFileSizeLimit(
        this IResourceBuilder<SupabaseStackResource> builder,
        long bytes)
    {
        var stack = builder.Resource;
        if (stack.Storage is null)
            throw new InvalidOperationException("Storage not configured. Ensure AddSupabase() has been called.");

        stack.Storage.Resource.FileSizeLimit = bytes;
        stack.Storage.WithEnvironment("FILE_SIZE_LIMIT", bytes.ToString());
        return builder;
    }

    /// <summary>
    /// Sets the storage backend type.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="backend">The storage backend type.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> WithStorageBackend(
        this IResourceBuilder<SupabaseStackResource> builder,
        string backend)
    {
        var stack = builder.Resource;
        if (stack.Storage is null)
            throw new InvalidOperationException("Storage not configured. Ensure AddSupabase() has been called.");

        stack.Storage.Resource.Backend = backend;
        stack.Storage.WithEnvironment("STORAGE_BACKEND", backend);
        return builder;
    }

    /// <summary>
    /// Enables or disables image transformation.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="enabled">Whether image transformation is enabled.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> WithStorageImageTransformation(
        this IResourceBuilder<SupabaseStackResource> builder,
        bool enabled = true)
    {
        var stack = builder.Resource;
        if (stack.Storage is null)
            throw new InvalidOperationException("Storage not configured. Ensure AddSupabase() has been called.");

        stack.Storage.Resource.EnableImageTransformation = enabled;
        stack.Storage.WithEnvironment("ENABLE_IMAGE_TRANSFORMATION", enabled ? "true" : "false");
        return builder;
    }

    #endregion

    #region Legacy ConfigureStorage (Obsolete)

    /// <summary>
    /// Configures the Storage API settings.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="configure">Configuration action for the Storage resource builder.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> ConfigureStorage(
        this IResourceBuilder<SupabaseStackResource> builder,
        Action<IResourceBuilder<SupabaseStorageResource>> configure)
    {
        var stack = builder.Resource;
        if (stack.Storage is null)
            throw new InvalidOperationException("Storage not configured. Ensure AddSupabase() has been called.");

        configure(stack.Storage);
        return builder;
    }

    #endregion

    #region Sub-Resource Methods (for use with ConfigureStorage)

    /// <summary>
    /// Sets the maximum file size limit in bytes.
    /// </summary>
    public static IResourceBuilder<SupabaseStorageResource> WithFileSizeLimit(
        this IResourceBuilder<SupabaseStorageResource> builder,
        long bytes)
    {
        builder.Resource.FileSizeLimit = bytes;
        builder.WithEnvironment("FILE_SIZE_LIMIT", bytes.ToString());
        return builder;
    }

    /// <summary>
    /// Sets the storage backend type.
    /// </summary>
    public static IResourceBuilder<SupabaseStorageResource> WithBackend(
        this IResourceBuilder<SupabaseStorageResource> builder,
        string backend)
    {
        builder.Resource.Backend = backend;
        builder.WithEnvironment("STORAGE_BACKEND", backend);
        return builder;
    }

    /// <summary>
    /// Enables or disables image transformation.
    /// </summary>
    public static IResourceBuilder<SupabaseStorageResource> WithImageTransformation(
        this IResourceBuilder<SupabaseStorageResource> builder,
        bool enabled = true)
    {
        builder.Resource.EnableImageTransformation = enabled;
        builder.WithEnvironment("ENABLE_IMAGE_TRANSFORMATION", enabled ? "true" : "false");
        return builder;
    }

    #endregion
}
