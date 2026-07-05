using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.ImageGen;

/// <summary>
/// An OpenAI-compatible image generation service (backend: LocalAI).
/// Exposes <c>/v1/images/generations</c> so any OpenAI-images client can consume it,
/// plus LocalAI's built-in WebUI on the same endpoint.
/// </summary>
public sealed class ImageGenerationResource(string name) : ContainerResource(name)
{
    /// <summary>Default internal container port LocalAI listens on.</summary>
    public const int DefaultTargetPort = 8080;

    /// <summary>Name of the primary HTTP endpoint.</summary>
    public const string HttpEndpointName = "http";

    /// <summary>The HTTP endpoint that serves the OpenAI-compatible API (and LocalAI WebUI).</summary>
    public EndpointReference HttpEndpoint => new(this, HttpEndpointName);

    /// <summary>Models registered via <c>AddModel</c> (installed from the LocalAI gallery on startup).</summary>
    public IList<ImageModel> Models { get; } = [];

    /// <summary>Whether the container was configured with GPU acceleration (used for generated configs).</summary>
    internal bool GpuEnabled { get; set; }

    /// <summary>Host directory with generated HuggingFace model configs (bind-mounted once).</summary>
    internal string? HfConfigDir { get; set; }

    /// <summary>
    /// The model consumers use by default (first <c>AddModel</c> wins; falls back to the
    /// AIO-bundled <c>stablediffusion</c> when no model was added explicitly).
    /// </summary>
    public string DefaultModel => Models.Count > 0 ? Models[0].Name : ImageModel.NameOf(KnownImageModel.StableDiffusionAio);
}

/// <summary>An Open WebUI container wired to an <see cref="ImageGenerationResource"/>.</summary>
public sealed class ImageGenOpenWebUIResource(string name) : ContainerResource(name);

/// <summary>A standalone SD.Next image-generation studio attached to the stack.</summary>
public sealed class SdNextResource(string name) : ContainerResource(name);
