using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.N8n.Resources;

namespace Nextended.Aspire.Hosting.N8n.Builders;

/// <summary>
/// Provides fluent configuration extensions for an <see cref="N8nResource"/>.
/// </summary>
public static class N8nConfigurationExtensions
{
    /// <summary>
    /// Sets the n8n encryption key used to encrypt stored credentials.
    /// The key MUST stay stable across restarts, otherwise existing credentials become unreadable.
    /// </summary>
    public static IResourceBuilder<N8nResource> WithEncryptionKey(
        this IResourceBuilder<N8nResource> builder, string encryptionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptionKey);
        builder.Resource.EncryptionKey = encryptionKey;
        builder.Resource.EncryptionKeyParameter = null;
        return builder;
    }

    /// <summary>
    /// Sets the n8n encryption key from an Aspire parameter, so the secret flows through user
    /// secrets locally and Key Vault on deployment. Keep the underlying value stable across restarts.
    /// </summary>
    public static IResourceBuilder<N8nResource> WithEncryptionKey(
        this IResourceBuilder<N8nResource> builder, IResourceBuilder<ParameterResource> encryptionKey)
    {
        ArgumentNullException.ThrowIfNull(encryptionKey);
        builder.Resource.EncryptionKeyParameter = encryptionKey.Resource;
        return builder;
    }

    /// <summary>
    /// Enables HTTP basic authentication for the n8n editor with the given credentials.
    /// </summary>
    /// <remarks>
    /// This sets the legacy <c>N8N_BASIC_AUTH_*</c> variables and only has an effect on n8n
    /// versions &lt; 1.0. Modern n8n (the default image) uses the built-in owner-account / user
    /// management model instead, which is set up interactively on first launch — these variables
    /// are ignored there.
    /// </remarks>
    public static IResourceBuilder<N8nResource> WithBasicAuth(
        this IResourceBuilder<N8nResource> builder, string user, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(user);
        ArgumentNullException.ThrowIfNull(password);
        builder.Resource.BasicAuthUser = user;
        builder.Resource.BasicAuthPassword = password;
        return builder;
    }

    /// <summary>
    /// Sets the timezone used by n8n for scheduling (cron) and date handling, e.g. "Europe/Berlin".
    /// </summary>
    public static IResourceBuilder<N8nResource> WithTimezone(
        this IResourceBuilder<N8nResource> builder, string timezone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(timezone);
        builder.Resource.Timezone = timezone;
        return builder;
    }

    /// <summary>
    /// Sets the public webhook base URL used to build production webhook URLs (useful behind a proxy).
    /// In publish mode this defaults to the n8n public endpoint when not set explicitly.
    /// </summary>
    public static IResourceBuilder<N8nResource> WithWebhookUrl(
        this IResourceBuilder<N8nResource> builder, string webhookUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(webhookUrl);
        builder.Resource.WebhookUrl = webhookUrl;
        return builder;
    }

    /// <summary>
    /// Sets the public editor base URL. In publish mode this defaults to the n8n public endpoint
    /// when not set explicitly.
    /// </summary>
    public static IResourceBuilder<N8nResource> WithEditorBaseUrl(
        this IResourceBuilder<N8nResource> builder, string editorBaseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(editorBaseUrl);
        builder.Resource.EditorBaseUrl = editorBaseUrl;
        return builder;
    }

    /// <summary>
    /// Overrides the n8n container image tag (default: <c>1.110.1</c>).
    /// </summary>
    public static IResourceBuilder<N8nResource> WithImageTag(
        this IResourceBuilder<N8nResource> builder, string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        return builder.WithImage(builder.Resource.Image, tag);
    }

    /// <summary>
    /// Overrides the n8n container image and tag (default: <c>n8nio/n8n:1.110.1</c>).
    /// </summary>
    public static IResourceBuilder<N8nResource> WithImage(
        this IResourceBuilder<N8nResource> builder, string image, string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(image);
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        builder.Resource.Image = image;
        builder.Resource.ImageTag = tag;
        ContainerResourceBuilderExtensions.WithImage(builder, image, tag);
        return builder;
    }

    /// <summary>
    /// Adds or overrides a raw n8n environment variable. Use this to set any n8n option that
    /// is not covered by a dedicated fluent method.
    /// </summary>
    public static IResourceBuilder<N8nResource> WithEnvironmentVariable(
        this IResourceBuilder<N8nResource> builder, string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        builder.WithEnvironment(name, value);
        return builder;
    }

    /// <summary>
    /// Sets the fixed host port for the n8n editor (local development only).
    /// </summary>
    public static IResourceBuilder<N8nResource> WithHostPort(
        this IResourceBuilder<N8nResource> builder, int port)
    {
        builder.Resource.HostPort = port;
        var endpoint = builder.Resource.Annotations
            .OfType<EndpointAnnotation>()
            .FirstOrDefault(e => e.Name == N8nResource.HttpEndpointName);
        if (endpoint is not null)
            endpoint.Port = port;
        return builder;
    }
}
