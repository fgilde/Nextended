using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.ImageGen;

/// <summary>GPU vendor for the image generation container.</summary>
public enum ImageGenGpu
{
    /// <summary>CPU only (works everywhere, slow).</summary>
    None,
    /// <summary>NVIDIA GPU (<c>--gpus all</c>; needs NVIDIA Container Toolkit / Docker Desktop GPU support).</summary>
    Nvidia,
    /// <summary>AMD GPU via ROCm devices (<c>/dev/kfd</c>, <c>/dev/dri</c>).</summary>
    Amd,
}

/// <summary>Options for <see cref="ImageGenerationBuilderExtensions.AddImageGeneration"/>.</summary>
public sealed class ImageGenerationOptions
{
    /// <summary>Container image (without tag). Default: <c>localai/localai</c>.</summary>
    public string Image { get; set; } = "localai/localai";

    /// <summary>
    /// Image tag. Default: the all-in-one NVIDIA CUDA 12 build which bundles a ready
    /// <c>stablediffusion</c> model. Use e.g. <c>latest-gpu-nvidia-cuda-12</c> for a slim
    /// image that only loads what you <c>AddModel</c>.
    /// </summary>
    public string Tag { get; set; } = "latest-aio-gpu-nvidia-cuda-12";

    /// <summary>GPU vendor. Default: <see cref="ImageGenGpu.Nvidia"/>.</summary>
    public ImageGenGpu Gpu { get; set; } = ImageGenGpu.Nvidia;

    /// <summary>Fixed host port for the endpoint (random if null).</summary>
    public int? HostPort { get; set; }

    /// <summary>
    /// AIO profile (<c>cpu</c>, <c>gpu-8g</c>, <c>apple</c>). LocalAI's AIO images detect the GPU
    /// via <c>lspci</c>, which fails inside Docker Desktop/WSL2 even when <c>--gpus all</c> works —
    /// so when <see cref="Gpu"/> is <see cref="ImageGenGpu.Nvidia"/> and an AIO tag is used,
    /// this defaults to <c>gpu-8g</c> to force GPU mode. Set explicitly to override.
    /// </summary>
    public string? AioProfile { get; set; }

    /// <summary>Optional API key the backend requires (sets LocalAI <c>API_KEY</c>).</summary>
    public string? ApiKey { get; set; }

    /// <summary>Extra environment variables for the container.</summary>
    public IDictionary<string, string> Environment { get; } = new Dictionary<string, string>();
}

/// <summary>
/// Aspire hosting extension for a self-hosted, OpenAI-compatible image generation service —
/// the text-to-image counterpart of <c>AddOllama</c>.
/// </summary>
public static class ImageGenerationBuilderExtensions
{
    /// <summary>
    /// Adds an OpenAI-compatible image generation container (LocalAI).
    /// </summary>
    /// <example>
    /// <code>
    /// var imagegen = builder.AddImageGeneration("imagegen")
    ///     .WithDataVolume()
    ///     .AddModel(KnownImageModel.Flux1Schnell)
    ///     .WithOpenWebUI();
    ///
    /// builder.AddProject&lt;Projects.Web&gt;("web").WithImageGeneration(imagegen);
    /// </code>
    /// </example>
    public static IResourceBuilder<ImageGenerationResource> AddImageGeneration(
        this IDistributedApplicationBuilder builder,
        string name,
        Action<ImageGenerationOptions>? configure = null)
    {
        var options = new ImageGenerationOptions();
        configure?.Invoke(options);

        var resource = new ImageGenerationResource(name);
        var rb = builder.AddResource(resource)
            .WithImage(options.Image, options.Tag)
            .WithHttpEndpoint(port: options.HostPort, targetPort: ImageGenerationResource.DefaultTargetPort, name: ImageGenerationResource.HttpEndpointName)
            .WithHttpHealthCheck("/readyz");

        switch (options.Gpu)
        {
            case ImageGenGpu.Nvidia:
                rb.WithContainerRuntimeArgs("--gpus", "all");
                break;
            case ImageGenGpu.Amd:
                rb.WithContainerRuntimeArgs("--device", "/dev/kfd", "--device", "/dev/dri");
                break;
        }

        // AIO images detect the GPU via lspci, which does not see the WSL2/Docker-Desktop GPU.
        // Forcing the profile skips detection; the CUDA backends are selected by the image anyway.
        var isAio = options.Tag.Contains("-aio-", StringComparison.OrdinalIgnoreCase) || options.Tag.StartsWith("latest-aio", StringComparison.OrdinalIgnoreCase);
        var profile = options.AioProfile ?? (isAio && options.Gpu == ImageGenGpu.Nvidia ? "gpu-8g" : null);
        if (profile is not null)
            rb.WithEnvironment("PROFILE", profile);

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
            rb.WithEnvironment("API_KEY", options.ApiKey);

        resource.GpuEnabled = options.Gpu != ImageGenGpu.None;

        // Deferred: when models were added via AddModel/AddHuggingFaceModel, MODELS lists
        // exactly those — this also overrides the AIO images' full default model set
        // (embeddings, tts, vision, ...), so only what you asked for gets downloaded and loaded.
        rb.WithEnvironment(context =>
        {
            if (resource.Models.Count > 0)
                context.EnvironmentVariables["MODELS"] = string.Join(",", resource.Models.Select(m => m.Reference));
        });

        foreach (var (key, value) in options.Environment)
            rb.WithEnvironment(key, value);

        return rb;
    }

    /// <summary>
    /// Registers a model to install from the LocalAI gallery on startup.
    /// Accepts gallery names, huggingface/OCI URIs or config URLs — and implicitly
    /// <see cref="KnownImageModel"/> values or plain strings. The first added model becomes
    /// the default injected by <see cref="WithImageGeneration{T}"/>.
    /// Adding models replaces the AIO images' bundled default set: only what you add is
    /// downloaded and loaded. Combine with <see cref="WithDataVolume"/> so downloads
    /// survive restarts.
    /// </summary>
    public static IResourceBuilder<ImageGenerationResource> AddModel(
        this IResourceBuilder<ImageGenerationResource> builder,
        ImageModel model)
    {
        builder.Resource.Models.Add(model);
        return builder;
    }

    /// <summary>
    /// Registers a HuggingFace-hosted diffusers model (e.g. SDXL fine-tunes like RealVisXL or
    /// the UnfilteredAI NSFW models) that is not part of the LocalAI gallery. A model config
    /// yaml is generated and bind-mounted into the container; LocalAI downloads the weights
    /// from HuggingFace on startup. Combine with <see cref="WithDataVolume"/>.
    /// </summary>
    /// <param name="builder">The image generation resource builder.</param>
    /// <param name="known">A curated, verified HF model.</param>
    /// <param name="name">Optional model id consumers use (defaults to a slug of the enum member).</param>
    public static IResourceBuilder<ImageGenerationResource> AddHuggingFaceModel(
        this IResourceBuilder<ImageGenerationResource> builder,
        KnownHuggingFaceImageModel known,
        string? name = null)
        => builder.AddHuggingFaceModel(
            name ?? known.ToString().ToLowerInvariant(),
            ImageModel.RepoOf(known),
            steps: ImageModel.StepsOf(known),
            f16: ImageModel.F16Of(known));

    /// <summary>
    /// Registers any HuggingFace-hosted diffusers model by repo id, e.g.
    /// <c>AddHuggingFaceModel("realvis", "SG161222/RealVisXL_V4.0")</c>.
    /// </summary>
    /// <param name="builder">The image generation resource builder.</param>
    /// <param name="name">Model id consumers use (also shown in /v1/models).</param>
    /// <param name="hfRepo">HuggingFace repo id in diffusers format (owner/repo).</param>
    /// <param name="pipelineType">diffusers pipeline; default fits SDXL-class models.</param>
    /// <param name="steps">Sampler steps (turbo models want ~6-8).</param>
    /// <param name="f16">Load the fp16 file variant — only for repos that publish <c>*.fp16.safetensors</c>.</param>
    public static IResourceBuilder<ImageGenerationResource> AddHuggingFaceModel(
        this IResourceBuilder<ImageGenerationResource> builder,
        string name,
        string hfRepo,
        string pipelineType = "StableDiffusionXLPipeline",
        int steps = 25,
        bool f16 = false)
    {
        var resource = builder.Resource;

        if (resource.HfConfigDir is null)
        {
            resource.HfConfigDir = Path.Combine(
                builder.ApplicationBuilder.AppHostDirectory, "obj", "imagegen", resource.Name);
            Directory.CreateDirectory(resource.HfConfigDir);
            builder.WithBindMount(resource.HfConfigDir, "/hf-configs", isReadOnly: true);
        }

        var safeName = new string(name.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '-').ToArray());
        // f16 defaults OFF: setting it makes diffusers request the "fp16" file variant,
        // which many HF repos don't ship (→ "variant=fp16, but no such modeling files").
        // Loading the default variant works everywhere; enable f16 only for repos that
        // actually publish fp16 weights. On GPU we still run on CUDA (diffusers.cuda).
        var yaml = $"""
            name: {safeName}
            backend: diffusers
            step: {steps}
            {(f16 ? "f16: true\n" : "")}parameters:
              model: {hfRepo}
            diffusers:
              cuda: {(resource.GpuEnabled ? "true" : "false")}
              pipeline_type: {pipelineType}
            """;
        File.WriteAllText(Path.Combine(resource.HfConfigDir, $"{safeName}.yaml"), yaml);

        resource.Models.Add(new ImageModel(safeName) { Reference = $"/hf-configs/{safeName}.yaml" });
        return builder;
    }

    /// <summary>
    /// Persists downloaded models AND backend runtimes in named volumes
    /// (<c>{name}-models</c> at <c>/models</c>, <c>{name}-backends</c> at <c>/backends</c>),
    /// so restarts don't re-download gigabytes.
    /// </summary>
    public static IResourceBuilder<ImageGenerationResource> WithDataVolume(
        this IResourceBuilder<ImageGenerationResource> builder,
        string? volumeName = null)
    {
        var baseName = volumeName ?? builder.Resource.Name;
        return builder
            .WithVolume(volumeName ?? $"{baseName}-models", "/models")
            .WithVolume($"{baseName}-backends", "/backends");
    }

    /// <summary>
    /// Adds an Open WebUI container wired to this image generation service
    /// (image generation via the OpenAI-compatible endpoint; chat models served by the
    /// same LocalAI instance also work). Dev-time only: excluded from the publish manifest.
    /// Note: LocalAI additionally ships its own WebUI on the service endpoint itself.
    /// </summary>
    public static IResourceBuilder<ImageGenerationResource> WithOpenWebUI(
        this IResourceBuilder<ImageGenerationResource> builder,
        int? hostPort = null,
        string? name = null)
    {
        var appBuilder = builder.ApplicationBuilder;
        var uiName = name ?? $"{builder.Resource.Name}-webui";
        var apiBase = ReferenceExpression.Create($"{builder.Resource.HttpEndpoint}/v1");

        appBuilder.AddResource(new ImageGenOpenWebUIResource(uiName))
            .WithImage("ghcr.io/open-webui/open-webui", "main")
            .WithHttpEndpoint(port: hostPort, targetPort: 8080, name: "http")
            .WithVolume($"{uiName}-data", "/app/backend/data")
            .WithEnvironment("WEBUI_AUTH", "False")
            .WithEnvironment("ENABLE_IMAGE_GENERATION", "True")
            .WithEnvironment("IMAGE_GENERATION_ENGINE", "openai")
            .WithEnvironment("IMAGES_OPENAI_API_BASE_URL", apiBase)
            .WithEnvironment("IMAGES_OPENAI_API_KEY", "sk-local")
            // Deferred: picks up models even when AddModel is chained after WithOpenWebUI.
            .WithEnvironment(ctx => ctx.EnvironmentVariables["IMAGE_GENERATION_MODEL"] = builder.Resource.DefaultModel)
            .WithEnvironment("OPENAI_API_BASE_URL", apiBase)
            .WithEnvironment("OPENAI_API_KEY", "sk-local")
            .WithReference(builder.Resource.HttpEndpoint)
            .WaitFor(builder)
            .WithParentRelationship(builder.Resource)
            .ExcludeFromManifest();

        return builder;
    }

    /// <summary>
    /// Reuses an EXISTING Open WebUI (e.g. the one added by the Ollama integration's
    /// <c>WithOpenWebUI()</c>) and extends it with image generation against this service,
    /// instead of spinning up a second Open WebUI. Pass the resource you already have,
    /// e.g. <c>builder.Resources.OfType&lt;OpenWebUIResource&gt;().FirstOrDefault()</c>.
    /// </summary>
    public static IResourceBuilder<ImageGenerationResource> WithOpenWebUI(
        this IResourceBuilder<ImageGenerationResource> builder,
        IResourceWithEnvironment existingOpenWebUi)
    {
        AttachImageGeneration(builder.Resource, existingOpenWebUi);
        return builder;
    }

    /// <summary>
    /// Wires image generation into an already-present Open WebUI if one exists in the app
    /// (matched by resource type), otherwise creates a new one. Handy when Ollama already
    /// added an Open WebUI and you just want your image models to show up there too.
    /// </summary>
    public static IResourceBuilder<ImageGenerationResource> WithOpenWebUI(
        this IResourceBuilder<ImageGenerationResource> builder,
        bool useExistingIfFound,
        int? hostPort = null,
        string? name = null)
    {
        if (useExistingIfFound)
        {
            // Duck-typed lookup so this package needs no dependency on the Ollama integration.
            var existing = builder.ApplicationBuilder.Resources
                .OfType<IResourceWithEnvironment>()
                .FirstOrDefault(r => r.GetType().Name == "OpenWebUIResource");
            if (existing is not null)
            {
                AttachImageGeneration(builder.Resource, existing);
                return builder;
            }
        }
        return builder.WithOpenWebUI(hostPort, name);
    }

    /// <summary>Adds the image-generation env vars to any Open WebUI resource (deferred resolution).</summary>
    private static void AttachImageGeneration(ImageGenerationResource imageGen, IResourceWithEnvironment webui)
    {
        var apiBase = ReferenceExpression.Create($"{imageGen.HttpEndpoint}/v1");
        webui.Annotations.Add(new EnvironmentCallbackAnnotation(ctx =>
        {
            ctx.EnvironmentVariables["ENABLE_IMAGE_GENERATION"] = "True";
            ctx.EnvironmentVariables["IMAGE_GENERATION_ENGINE"] = "openai";
            ctx.EnvironmentVariables["IMAGES_OPENAI_API_BASE_URL"] = apiBase;
            ctx.EnvironmentVariables["IMAGES_OPENAI_API_KEY"] = "sk-local";
            ctx.EnvironmentVariables["IMAGE_GENERATION_MODEL"] = imageGen.DefaultModel;
        }));
    }

    /// <summary>
    /// Adds a standalone <see href="https://github.com/vladmandic/sdnext">SD.Next</see> image
    /// studio (full txt2img/img2img UI, model &amp; LoRA management, Civitai/HuggingFace downloads) —
    /// the practical UI for experimenting with (also NSFW) image models. Runs its own GPU
    /// container with its own models (does not proxy through LocalAI); complementary to
    /// the Open WebUI overloads. Dev-time only (excluded from the publish manifest).
    /// </summary>
    /// <param name="builder">The image generation resource builder (parent for grouping/GPU).</param>
    /// <param name="hostPort">Fixed host port for the SD.Next UI (default random).</param>
    /// <param name="image">Container image (default: <c>vladmandic/sdnext-cuda</c>; use a ROCm/IPEX image for other GPUs).</param>
    /// <param name="tag">Image tag (default <c>latest</c>).</param>
    /// <param name="name">Resource name (default <c>{name}-sdnext</c>).</param>
    public static IResourceBuilder<ImageGenerationResource> WithSdNextUi(
        this IResourceBuilder<ImageGenerationResource> builder,
        int? hostPort = null,
        string image = "vladmandic/sdnext-cuda",
        string tag = "latest",
        string? name = null)
    {
        var appBuilder = builder.ApplicationBuilder;
        var uiName = name ?? $"{builder.Resource.Name}-sdnext";

        var rb = appBuilder.AddResource(new SdNextResource(uiName))
            .WithImage(image, tag)
            .WithHttpEndpoint(port: hostPort, targetPort: 7860, name: "http")
            .WithVolume($"{uiName}-models", "/mnt/models")   // Checkpoints, LoRAs, VAE …
            .WithVolume($"{uiName}-data", "/mnt/data")        // Config, Outputs, Cache
            .WithParentRelationship(builder.Resource)
            .ExcludeFromManifest();

        // GPU folgt der Konfiguration der Image-Gen-Resource (Default: NVIDIA).
        if (builder.Resource.GpuEnabled)
            rb.WithContainerRuntimeArgs("--gpus", "all");

        return builder;
    }

    /// <summary>
    /// Wires a consumer (frontend/API) to the image generation service by injecting
    /// <c>IMAGE_PROVIDER=openai-compatible</c>, <c>IMAGE_API_BASE</c>, <c>IMAGE_MODEL</c>
    /// (and <c>IMAGE_API_KEY</c> when set).
    /// </summary>
    /// <param name="builder">The consuming resource.</param>
    /// <param name="imageGen">The image generation resource.</param>
    /// <param name="model">Model override; defaults to the service's default model.</param>
    /// <param name="apiKey">API key override, when the backend was configured with one.</param>
    public static IResourceBuilder<T> WithImageGeneration<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<ImageGenerationResource> imageGen,
        ImageModel? model = null,
        string? apiKey = null)
        where T : IResourceWithEnvironment
    {
        builder
            .WithReference(imageGen.Resource.HttpEndpoint)
            .WithEnvironment("IMAGE_PROVIDER", "openai-compatible")
            .WithEnvironment("IMAGE_API_BASE", imageGen.Resource.HttpEndpoint)
            // Deferred: resolves the default model at startup, regardless of call order.
            .WithEnvironment(ctx => ctx.EnvironmentVariables["IMAGE_MODEL"] = model?.Name ?? imageGen.Resource.DefaultModel);

        if (!string.IsNullOrWhiteSpace(apiKey))
            builder.WithEnvironment("IMAGE_API_KEY", apiKey);

        return builder;
    }
}
