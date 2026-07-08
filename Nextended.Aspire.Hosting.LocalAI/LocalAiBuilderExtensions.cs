using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.LocalAI;

/// <summary>GPU vendor for the LocalAI container.</summary>
public enum LocalAiGpu
{
    /// <summary>CPU only (works everywhere, slow).</summary>
    None,
    /// <summary>NVIDIA GPU (<c>--gpus all</c>; needs NVIDIA Container Toolkit / Docker Desktop GPU support).</summary>
    Nvidia,
    /// <summary>AMD GPU via ROCm devices (<c>/dev/kfd</c>, <c>/dev/dri</c>).</summary>
    Amd,
}

/// <summary>Options for <see cref="LocalAiBuilderExtensions.AddLocalAI"/>.</summary>
public sealed class LocalAiOptions
{
    /// <summary>Container image (without tag). Default: <c>localai/localai</c>.</summary>
    public string Image { get; set; } = "localai/localai";

    /// <summary>
    /// Image tag. Default: the standard NVIDIA CUDA 12 build (currently LocalAI 4.x) — this is what
    /// brings <b>video generation</b> and the <b>ace-step sound</b> backend. It's a slim image that
    /// only loads what you add via <c>AddModel</c>/<c>AddTextModel</c>/…; backends download on demand.
    /// NOTE: the all-in-one (<c>-aio-</c>) tags are frozen at v3.12.1 upstream and lack video/sound —
    /// only pick one (e.g. <c>latest-aio-gpu-nvidia-cuda-12</c>) if you specifically want the bundled
    /// default model set and can do without the newer backends.
    /// </summary>
    public string Tag { get; set; } = "latest-gpu-nvidia-cuda-12";

    /// <summary>GPU vendor. Default: <see cref="LocalAiGpu.Nvidia"/>.</summary>
    public LocalAiGpu Gpu { get; set; } = LocalAiGpu.Nvidia;

    /// <summary>Fixed host port for the endpoint (random if null).</summary>
    public int? HostPort { get; set; }

    /// <summary>
    /// AIO profile (<c>cpu</c>, <c>gpu-8g</c>, <c>apple</c>). LocalAI's AIO images detect the GPU
    /// via <c>lspci</c>, which fails inside Docker Desktop/WSL2 even when <c>--gpus all</c> works —
    /// so when <see cref="Gpu"/> is <see cref="LocalAiGpu.Nvidia"/> and an AIO tag is used,
    /// this defaults to <c>gpu-8g</c> to force GPU mode. Set explicitly to override.
    /// </summary>
    public string? AioProfile { get; set; }

    /// <summary>Optional API key the backend requires (sets LocalAI <c>API_KEY</c>).</summary>
    public string? ApiKey { get; set; }

    /// <summary>Extra environment variables for the container.</summary>
    public IDictionary<string, string> Environment { get; } = new Dictionary<string, string>();
}

/// <summary>
/// Options for the <c>WithOpenWebUI(...)</c> overloads. Defaults reproduce the built-in wiring
/// (LocalAI registered as an OpenAI-compatible connection + image generation, env authoritative).
/// Override to change auth, persistence, the image model/tag, or add arbitrary env.
/// </summary>
public sealed class OpenWebUiOptions
{
    /// <summary>Container image — only used when a NEW Open WebUI is created. Default <c>ghcr.io/open-webui/open-webui</c>.</summary>
    public string Image { get; set; } = "ghcr.io/open-webui/open-webui";

    /// <summary>Image tag (new WebUI only). Default <c>main</c>.</summary>
    public string Tag { get; set; } = "main";

    /// <summary>
    /// <c>WEBUI_AUTH</c>. <c>null</c> = leave untouched (a reused WebUI keeps its own setting);
    /// a newly-created WebUI defaults to <c>false</c> (no login, dev-friendly).
    /// </summary>
    public bool? Auth { get; set; }

    /// <summary>
    /// <c>ENABLE_PERSISTENT_CONFIG</c>. Default <c>false</c> so this env is authoritative on every start —
    /// otherwise Open WebUI freezes these values in its DB on first run and ignores later env changes
    /// (that's why a reused WebUI wouldn't pick up the config). Set <c>true</c> to let the UI persist changes.
    /// </summary>
    public bool PersistentConfig { get; set; }

    /// <summary>Register LocalAI as an OpenAI-compatible model connection (chat/LLM/vision list). Default <c>true</c>.</summary>
    public bool RegisterOpenAiModels { get; set; } = true;

    /// <summary>Wire image generation against LocalAI's OpenAI-compatible images endpoint. Default <c>true</c>.</summary>
    public bool EnableImageGeneration { get; set; } = true;

    /// <summary>Image-generation model Open WebUI uses. Default: the LocalAI default image model.</summary>
    public string? ImageGenerationModel { get; set; }

    /// <summary>API key sent to LocalAI for the OpenAI/images calls. Default <c>sk-local</c>.</summary>
    public string ApiKey { get; set; } = "sk-local";

    /// <summary>Extra environment variables for the Open WebUI container (applied last — overrides the above).</summary>
    public IDictionary<string, string> Environment { get; } = new Dictionary<string, string>();
}

/// <summary>
/// Aspire hosting extension for a self-hosted, OpenAI-compatible multimodal AI service (LocalAI):
/// image generation, text-to-speech, speech-to-text, video generation, chat and embeddings —
/// the self-hosted counterpart of <c>AddOllama</c> for everything beyond text.
/// </summary>
public static class LocalAiBuilderExtensions
{
    /// <summary>
    /// Adds a self-hosted, OpenAI-compatible multimodal AI container (LocalAI). One service serves
    /// images (<c>/v1/images/generations</c>), speech (<c>/v1/audio/speech</c>), transcription
    /// (<c>/v1/audio/transcriptions</c>), video (<c>/video</c>), chat and embeddings.
    /// </summary>
    /// <example>
    /// <code>
    /// var ai = builder.AddLocalAI("localai")
    ///     .WithDataVolume()
    ///     .AddModel(KnownImageModel.Flux1Schnell)
    ///     .AddTextToSpeechModel(KnownTextToSpeechModel.QwenTts)
    ///     .AddSpeechToTextModel(KnownSpeechToTextModel.WhisperBase)
    ///     .WithOpenWebUI();
    ///
    /// builder.AddProject&lt;Projects.Web&gt;("web").WithLocalAI(ai);
    /// </code>
    /// </example>
    public static IResourceBuilder<LocalAiResource> AddLocalAI(
        this IDistributedApplicationBuilder builder,
        string name,
        Action<LocalAiOptions>? configure = null)
    {
        var options = new LocalAiOptions();
        configure?.Invoke(options);

        var resource = new LocalAiResource(name);
        var rb = builder.AddResource(resource)
            .WithImage(options.Image, options.Tag)
            .WithHttpEndpoint(port: options.HostPort, targetPort: LocalAiResource.DefaultTargetPort, name: LocalAiResource.HttpEndpointName)
            .WithHttpHealthCheck("/readyz");

        switch (options.Gpu)
        {
            case LocalAiGpu.Nvidia:
                rb.WithContainerRuntimeArgs("--gpus", "all");
                break;
            case LocalAiGpu.Amd:
                rb.WithContainerRuntimeArgs("--device", "/dev/kfd", "--device", "/dev/dri");
                break;
        }

        // AIO images detect the GPU via lspci, which does not see the WSL2/Docker-Desktop GPU.
        // Forcing the profile skips detection; the CUDA backends are selected by the image anyway.
        var isAio = options.Tag.Contains("-aio-", StringComparison.OrdinalIgnoreCase) || options.Tag.StartsWith("latest-aio", StringComparison.OrdinalIgnoreCase);
        var profile = options.AioProfile ?? (isAio && options.Gpu == LocalAiGpu.Nvidia ? "gpu-8g" : null);
        if (profile is not null)
            rb.WithEnvironment("PROFILE", profile);

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
            rb.WithEnvironment("API_KEY", options.ApiKey);

        resource.GpuEnabled = options.Gpu != LocalAiGpu.None;

        // Deferred: when models were added (image/tts/stt/video/sound), MODELS lists exactly those —
        // this also overrides the AIO images' full default model set (embeddings, tts, vision, ...),
        // so only what you asked for gets downloaded and loaded.
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
    /// Registers a text-to-image model to install from the LocalAI gallery on startup.
    /// Accepts gallery names, huggingface/OCI URIs or config URLs — and implicitly
    /// <see cref="KnownImageModel"/> values or plain strings. The first image model added becomes
    /// the default injected as <c>IMAGE_MODEL</c> by <see cref="WithLocalAI{T}"/>.
    /// Adding models replaces the AIO images' bundled default set: only what you add is
    /// downloaded and loaded. Combine with <see cref="WithDataVolume"/> so downloads
    /// survive restarts.
    /// </summary>
    public static IResourceBuilder<LocalAiResource> AddModel(
        this IResourceBuilder<LocalAiResource> builder,
        ImageModel model)
    {
        builder.Resource.Models.Add(new RegisteredModel(model.Name, model.Reference, ModelModality.Image));
        return builder;
    }

    // ---- AddModel convenience overloads ---------------------------------------------------------
    // So you can call AddModel(...) for ANY curated Known* enum and overload resolution (by the
    // enum's type) routes it to the right modality method — no need to remember AddTextToSpeechModel
    // vs. AddVideoModel vs. AddHuggingFaceModel etc. (KnownImageModel already flows into
    // AddModel(ImageModel) via its implicit conversion.)

    /// <summary>Convenience: routes a curated HuggingFace image model to <c>AddHuggingFaceModel</c>.</summary>
    public static IResourceBuilder<LocalAiResource> AddModel(this IResourceBuilder<LocalAiResource> builder, KnownHuggingFaceImageModel model) => builder.AddHuggingFaceModel(model);

    /// <summary>Convenience: routes a chat / LLM (incl. vision) model to <c>AddTextModel</c>.</summary>
    public static IResourceBuilder<LocalAiResource> AddModel(this IResourceBuilder<LocalAiResource> builder, KnownTextModel model) => builder.AddTextModel(model);

    /// <summary>Convenience: routes an embedding model to <c>AddEmbeddingModel</c>.</summary>
    public static IResourceBuilder<LocalAiResource> AddModel(this IResourceBuilder<LocalAiResource> builder, KnownEmbeddingModel model) => builder.AddEmbeddingModel(model);

    /// <summary>Convenience: routes a text-to-speech model to <c>AddTextToSpeechModel</c>.</summary>
    public static IResourceBuilder<LocalAiResource> AddModel(this IResourceBuilder<LocalAiResource> builder, KnownTextToSpeechModel model) => builder.AddTextToSpeechModel(model);

    /// <summary>Convenience: routes a speech-to-text model to <c>AddSpeechToTextModel</c>.</summary>
    public static IResourceBuilder<LocalAiResource> AddModel(this IResourceBuilder<LocalAiResource> builder, KnownSpeechToTextModel model) => builder.AddSpeechToTextModel(model);

    /// <summary>Convenience: routes a video model to <c>AddVideoModel</c>.</summary>
    public static IResourceBuilder<LocalAiResource> AddModel(this IResourceBuilder<LocalAiResource> builder, KnownVideoModel model) => builder.AddVideoModel(model);

    /// <summary>Convenience: routes a sound / music model to <c>AddSoundModel</c>.</summary>
    public static IResourceBuilder<LocalAiResource> AddModel(this IResourceBuilder<LocalAiResource> builder, KnownSoundModel model) => builder.AddSoundModel(model);

    /// <summary>
    /// Registers a text-to-speech model from the LocalAI gallery (served on
    /// <c>/v1/audio/speech</c>). The first TTS model added becomes the default injected as
    /// <c>TTS_MODEL</c> by <see cref="WithLocalAI{T}"/>.
    /// </summary>
    public static IResourceBuilder<LocalAiResource> AddTextToSpeechModel(
        this IResourceBuilder<LocalAiResource> builder,
        KnownTextToSpeechModel model)
        => builder.AddTextToSpeechModel(GalleryNames.Of(model));

    /// <summary>Registers any TTS gallery model by its exact gallery name (served on <c>/v1/audio/speech</c>).</summary>
    public static IResourceBuilder<LocalAiResource> AddTextToSpeechModel(
        this IResourceBuilder<LocalAiResource> builder,
        string galleryName)
    {
        builder.Resource.Models.Add(new RegisteredModel(galleryName, galleryName, ModelModality.TextToSpeech));
        return builder;
    }

    /// <summary>
    /// Registers a speech-to-text (whisper) model from the LocalAI gallery (served on
    /// <c>/v1/audio/transcriptions</c>). The first STT model added becomes the default injected
    /// as <c>STT_MODEL</c> by <see cref="WithLocalAI{T}"/>.
    /// </summary>
    public static IResourceBuilder<LocalAiResource> AddSpeechToTextModel(
        this IResourceBuilder<LocalAiResource> builder,
        KnownSpeechToTextModel model)
        => builder.AddSpeechToTextModel(GalleryNames.Of(model));

    /// <summary>Registers any STT gallery model by its exact gallery name (served on <c>/v1/audio/transcriptions</c>).</summary>
    public static IResourceBuilder<LocalAiResource> AddSpeechToTextModel(
        this IResourceBuilder<LocalAiResource> builder,
        string galleryName)
    {
        builder.Resource.Models.Add(new RegisteredModel(galleryName, galleryName, ModelModality.SpeechToText));
        return builder;
    }

    /// <summary>
    /// Registers a text/image-to-video model from the LocalAI gallery (served on <c>POST /video</c>).
    /// The first video model added becomes the default injected as <c>VIDEO_MODEL</c> by
    /// <see cref="WithLocalAI{T}"/>. Video weights are large (many GB) and generation is slow —
    /// combine with <see cref="WithDataVolume"/>.
    /// </summary>
    public static IResourceBuilder<LocalAiResource> AddVideoModel(
        this IResourceBuilder<LocalAiResource> builder,
        KnownVideoModel model)
        => builder.AddVideoModel(GalleryNames.Of(model));

    /// <summary>Registers any video gallery model by its exact gallery name (served on <c>POST /video</c>).</summary>
    public static IResourceBuilder<LocalAiResource> AddVideoModel(
        this IResourceBuilder<LocalAiResource> builder,
        string galleryName)
    {
        builder.Resource.Models.Add(new RegisteredModel(galleryName, galleryName, ModelModality.Video));
        return builder;
    }

    /// <summary>
    /// Registers a text-to-sound / MUSIC generation model from the LocalAI gallery (served on the
    /// ElevenLabs-compatible <c>POST /v1/sound-generation</c>). The first sound model added becomes
    /// the default injected as <c>SOUND_MODEL</c> by <see cref="WithLocalAI{T}"/>. Weights are large
    /// (many GB) and generation is GPU-bound — combine with <see cref="WithDataVolume"/>.
    /// </summary>
    public static IResourceBuilder<LocalAiResource> AddSoundModel(
        this IResourceBuilder<LocalAiResource> builder,
        KnownSoundModel model)
        => builder.AddSoundModel(GalleryNames.Of(model));

    /// <summary>Registers any sound/music gallery model by its exact gallery name (served on <c>POST /v1/sound-generation</c>).</summary>
    public static IResourceBuilder<LocalAiResource> AddSoundModel(
        this IResourceBuilder<LocalAiResource> builder,
        string galleryName)
    {
        builder.Resource.Models.Add(new RegisteredModel(galleryName, galleryName, ModelModality.Sound));
        return builder;
    }

    /// <summary>
    /// Registers a chat / LLM text model from the LocalAI gallery (served on <c>/v1/chat/completions</c>);
    /// includes vision-capable multimodal models. The first text model added becomes the default injected
    /// as <c>TEXT_MODEL</c> by <see cref="WithLocalAI{T}"/>. Any of the 1000+ gallery LLMs works via the
    /// <c>AddTextModel(string)</c> overload — this is LocalAI's <c>AddOllama</c>-style role for text.
    /// </summary>
    public static IResourceBuilder<LocalAiResource> AddTextModel(
        this IResourceBuilder<LocalAiResource> builder,
        KnownTextModel model)
        => builder.AddTextModel(GalleryNames.Of(model));

    /// <summary>Registers any chat/LLM gallery model by its exact gallery name (served on <c>/v1/chat/completions</c>).</summary>
    public static IResourceBuilder<LocalAiResource> AddTextModel(
        this IResourceBuilder<LocalAiResource> builder,
        string galleryName)
    {
        builder.Resource.Models.Add(new RegisteredModel(galleryName, galleryName, ModelModality.Text));
        return builder;
    }

    /// <summary>
    /// Registers a text-embedding model from the LocalAI gallery (served on <c>/v1/embeddings</c>).
    /// The first embedding model added becomes the default injected as <c>EMBEDDING_MODEL</c> by
    /// <see cref="WithLocalAI{T}"/>.
    /// </summary>
    public static IResourceBuilder<LocalAiResource> AddEmbeddingModel(
        this IResourceBuilder<LocalAiResource> builder,
        KnownEmbeddingModel model)
        => builder.AddEmbeddingModel(GalleryNames.Of(model));

    /// <summary>Registers any embedding gallery model by its exact gallery name (served on <c>/v1/embeddings</c>).</summary>
    public static IResourceBuilder<LocalAiResource> AddEmbeddingModel(
        this IResourceBuilder<LocalAiResource> builder,
        string galleryName)
    {
        builder.Resource.Models.Add(new RegisteredModel(galleryName, galleryName, ModelModality.Embedding));
        return builder;
    }

    /// <summary>
    /// Registers a text-embedding model via a GENERATED model config (erzwingt <c>embeddings: true</c> und
    /// ein explizites Embed-Backend), bind-gemountet in den Container. Nutze das, wenn ein Gallery-Modell
    /// beim <c>/v1/embeddings</c>-Aufruf „Method not implemented" liefert (dann implementiert das per Gallery
    /// gewählte Backend keine Embeddings). Das Default-Backend <c>sentencetransformers</c> lädt beliebige
    /// HuggingFace-Sentence-Transformers-Modelle zuverlässig als Embedder. Der erste Embedding-Eintrag wird
    /// von <see cref="WithLocalAI{T}"/> als <c>EMBEDDING_MODEL</c> injiziert. Mit <see cref="WithDataVolume"/>
    /// kombinieren (Backend + Gewichte werden beim ersten Start geladen).
    /// </summary>
    /// <param name="name">Modell-Id für Konsumenten (auch der EMBEDDING_MODEL-Wert + /v1/models-Eintrag).</param>
    /// <param name="model">HuggingFace-Repo (z. B. <c>sentence-transformers/all-MiniLM-L6-v2</c>) bzw. für
    /// <c>backend: llama-cpp</c> eine GGUF-URL/-Datei.</param>
    /// <param name="backend">LocalAI-Embed-Backend: <c>sentencetransformers</c> (Default), <c>llama-cpp</c>,
    /// <c>bert-embeddings</c>, …</param>
    public static IResourceBuilder<LocalAiResource> AddEmbeddingModel(
        this IResourceBuilder<LocalAiResource> builder,
        string name,
        string model,
        string backend = "sentencetransformers")
    {
        var resource = builder.Resource;
        if (resource.HfConfigDir is null)
        {
            resource.HfConfigDir = Path.Combine(
                builder.ApplicationBuilder.AppHostDirectory, "obj", "localai", resource.Name);
            Directory.CreateDirectory(resource.HfConfigDir);
            builder.WithBindMount(resource.HfConfigDir, "/hf-configs", isReadOnly: true);
        }

        var safeName = new string(name.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '-').ToArray());
        var yaml = $"""
            name: {safeName}
            backend: {backend}
            embeddings: true
            parameters:
              model: {model}
            """;
        File.WriteAllText(Path.Combine(resource.HfConfigDir, $"{safeName}.yaml"), yaml);

        resource.Models.Add(new RegisteredModel(safeName, $"/hf-configs/{safeName}.yaml", ModelModality.Embedding));
        return builder;
    }

    /// <summary>
    /// Registers a HuggingFace-hosted diffusers image model (e.g. SDXL fine-tunes like RealVisXL or
    /// the UnfilteredAI NSFW models) that is not part of the LocalAI gallery. A model config
    /// yaml is generated and bind-mounted into the container; LocalAI downloads the weights
    /// from HuggingFace on startup. Combine with <see cref="WithDataVolume"/>.
    /// </summary>
    /// <param name="builder">The LocalAI resource builder.</param>
    /// <param name="known">A curated, verified HF model.</param>
    /// <param name="name">Optional model id consumers use (defaults to a slug of the enum member).</param>
    public static IResourceBuilder<LocalAiResource> AddHuggingFaceModel(
        this IResourceBuilder<LocalAiResource> builder,
        KnownHuggingFaceImageModel known,
        string? name = null)
        => builder.AddHuggingFaceModel(
            name ?? known.ToString().ToLowerInvariant(),
            ImageModel.RepoOf(known),
            steps: ImageModel.StepsOf(known),
            f16: ImageModel.F16Of(known));

    /// <summary>
    /// Registers any HuggingFace-hosted diffusers image model by repo id, e.g.
    /// <c>AddHuggingFaceModel("realvis", "SG161222/RealVisXL_V4.0")</c>.
    /// </summary>
    /// <param name="builder">The LocalAI resource builder.</param>
    /// <param name="name">Model id consumers use (also shown in /v1/models).</param>
    /// <param name="hfRepo">HuggingFace repo id in diffusers format (owner/repo).</param>
    /// <param name="pipelineType">diffusers pipeline; default fits SDXL-class models.</param>
    /// <param name="steps">Sampler steps (turbo models want ~6-8).</param>
    /// <param name="f16">Load the fp16 file variant — only for repos that publish <c>*.fp16.safetensors</c>.</param>
    public static IResourceBuilder<LocalAiResource> AddHuggingFaceModel(
        this IResourceBuilder<LocalAiResource> builder,
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
                builder.ApplicationBuilder.AppHostDirectory, "obj", "localai", resource.Name);
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

        resource.Models.Add(new RegisteredModel(safeName, $"/hf-configs/{safeName}.yaml", ModelModality.Image));
        return builder;
    }

    /// <summary>
    /// Persists downloaded models AND backend runtimes in named volumes
    /// (<c>{name}-models</c> at <c>/models</c>, <c>{name}-backends</c> at <c>/backends</c>),
    /// so restarts don't re-download gigabytes.
    /// </summary>
    public static IResourceBuilder<LocalAiResource> WithDataVolume(
        this IResourceBuilder<LocalAiResource> builder,
        string? volumeName = null)
    {
        var baseName = volumeName ?? builder.Resource.Name;
        return builder
            .WithVolume(volumeName ?? $"{baseName}-models", "/models")
            .WithVolume($"{baseName}-backends", "/backends");
    }

    /// <summary>
    /// Adds a NEW Open WebUI container wired to this service: registers LocalAI as an OpenAI-compatible
    /// connection (chat/LLM + vision models appear) and enables image generation against it.
    /// Dev-time only: excluded from the publish manifest. (LocalAI additionally ships its own WebUI on
    /// the service endpoint.) Pass <paramref name="configure"/> to tweak defaults (auth, persistent
    /// config, image model, image tag, extra env …).
    /// </summary>
    public static IResourceBuilder<LocalAiResource> WithOpenWebUI(
        this IResourceBuilder<LocalAiResource> builder,
        int? hostPort = null,
        string? name = null,
        Action<OpenWebUiOptions>? configure = null)
    {
        var appBuilder = builder.ApplicationBuilder;
        var uiName = name ?? $"{builder.Resource.Name}-webui";
        var apiBase = ReferenceExpression.Create($"{builder.Resource.HttpEndpoint}/v1");
        var options = new OpenWebUiOptions { Auth = false };   // a brand-new WebUI defaults to no-auth (dev)
        configure?.Invoke(options);

        appBuilder.AddResource(new LocalAiOpenWebUIResource(uiName))
            .WithImage(options.Image, options.Tag)
            .WithHttpEndpoint(port: hostPort, targetPort: 8080, name: "http")
            .WithVolume($"{uiName}-data", "/app/backend/data")
            // Deferred: resolves the default model + reads options even when AddModel is chained after WithOpenWebUI.
            .WithEnvironment(ctx => ApplyOpenWebUiEnv(ctx, builder.Resource, options, apiBase))
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
    public static IResourceBuilder<LocalAiResource> WithOpenWebUI(
        this IResourceBuilder<LocalAiResource> builder,
        IResourceWithEnvironment existingOpenWebUi,
        Action<OpenWebUiOptions>? configure = null)
    {
        AttachLocalAiToOpenWebUI(builder.Resource, existingOpenWebUi, configure);
        return builder;
    }

    /// <summary>
    /// Wires image generation into an already-present Open WebUI if one exists in the app
    /// (matched by resource type), otherwise creates a new one. Handy when Ollama already
    /// added an Open WebUI and you just want your image models to show up there too.
    /// </summary>
    public static IResourceBuilder<LocalAiResource> WithOpenWebUI(
        this IResourceBuilder<LocalAiResource> builder,
        bool useExistingIfFound,
        int? hostPort = null,
        string? name = null,
        Action<OpenWebUiOptions>? configure = null)
    {
        if (useExistingIfFound)
        {
            // Duck-typed lookup so this package needs no dependency on the Ollama integration.
            var existing = builder.ApplicationBuilder.Resources
                .OfType<IResourceWithEnvironment>()
                .FirstOrDefault(r => r.GetType().Name == "OpenWebUIResource");
            if (existing is not null)
            {
                AttachLocalAiToOpenWebUI(builder.Resource, existing, configure);
                return builder;
            }
        }
        return builder.WithOpenWebUI(hostPort, name, configure);
    }

    /// <summary>
    /// Wires LocalAI into an EXISTING Open WebUI: registers it as an OpenAI-compatible connection
    /// (so its chat/LLM models show up alongside e.g. Ollama's — Ollama keeps working via its
    /// separate <c>OLLAMA_*</c> connection) AND enables image generation against it. Deferred env.
    /// </summary>
    private static void AttachLocalAiToOpenWebUI(LocalAiResource localAi, IResourceWithEnvironment webui, Action<OpenWebUiOptions>? configure)
    {
        // A reused WebUI keeps its own auth unless the caller opts in (Auth stays null => WEBUI_AUTH untouched).
        var options = new OpenWebUiOptions();
        configure?.Invoke(options);
        var apiBase = ReferenceExpression.Create($"{localAi.HttpEndpoint}/v1");
        webui.Annotations.Add(new EnvironmentCallbackAnnotation(ctx => ApplyOpenWebUiEnv(ctx, localAi, options, apiBase)));
    }

    /// <summary>
    /// Writes the Open WebUI env from <see cref="OpenWebUiOptions"/> into <paramref name="ctx"/> (shared by
    /// the new-WebUI and reuse-existing paths). CRUCIAL: <c>ENABLE_PERSISTENT_CONFIG=False</c> (default) makes
    /// this env authoritative every start — otherwise Open WebUI freezes the values in its DB on first run
    /// and ignores later changes (which is why a reused WebUI wouldn't pick up the connection/image config).
    /// User-supplied <see cref="OpenWebUiOptions.Environment"/> entries are applied last and win.
    /// </summary>
    private static void ApplyOpenWebUiEnv(EnvironmentCallbackContext ctx, LocalAiResource localAi, OpenWebUiOptions o, ReferenceExpression apiBase)
    {
        if (o.Auth.HasValue) ctx.EnvironmentVariables["WEBUI_AUTH"] = o.Auth.Value ? "True" : "False";
        ctx.EnvironmentVariables["ENABLE_PERSISTENT_CONFIG"] = o.PersistentConfig ? "True" : "False";
        if (o.RegisterOpenAiModels)
        {
            // Model list / chat: register LocalAI as an OpenAI-compatible backend (Ollama, if present, keeps
            // its own separate OLLAMA_* connection, so both model sets show up).
            ctx.EnvironmentVariables["ENABLE_OPENAI_API"] = "True";
            ctx.EnvironmentVariables["OPENAI_API_BASE_URL"] = apiBase;
            ctx.EnvironmentVariables["OPENAI_API_KEY"] = o.ApiKey;
        }
        if (o.EnableImageGeneration)
        {
            ctx.EnvironmentVariables["ENABLE_IMAGE_GENERATION"] = "True";
            ctx.EnvironmentVariables["IMAGE_GENERATION_ENGINE"] = "openai";
            ctx.EnvironmentVariables["IMAGES_OPENAI_API_BASE_URL"] = apiBase;
            ctx.EnvironmentVariables["IMAGES_OPENAI_API_KEY"] = o.ApiKey;
            ctx.EnvironmentVariables["IMAGE_GENERATION_MODEL"] = o.ImageGenerationModel ?? localAi.DefaultModel;
        }
        foreach (var (key, value) in o.Environment) ctx.EnvironmentVariables[key] = value;
    }

    /// <summary>
    /// Adds a standalone <see href="https://github.com/vladmandic/sdnext">SD.Next</see> image
    /// studio (full txt2img/img2img UI, model &amp; LoRA management, Civitai/HuggingFace downloads) —
    /// the practical UI for experimenting with (also NSFW) image models. Runs its own GPU
    /// container with its own models (does not proxy through LocalAI); complementary to
    /// the Open WebUI overloads. Dev-time only (excluded from the publish manifest).
    /// </summary>
    /// <param name="builder">The LocalAI resource builder (parent for grouping/GPU).</param>
    /// <param name="hostPort">Fixed host port for the SD.Next UI (default random).</param>
    /// <param name="image">Container image (default: <c>vladmandic/sdnext-cuda</c>; use a ROCm/IPEX image for other GPUs).</param>
    /// <param name="tag">Image tag (default <c>latest</c>).</param>
    /// <param name="name">Resource name (default <c>{name}-sdnext</c>).</param>
    /// <param name="shareHfCacheWithLocalAi">
    /// When <c>true</c>, mounts one shared HuggingFace-cache volume into BOTH this SD.Next container
    /// and the LocalAI container and points <c>HF_HOME</c> at it — so any <b>HuggingFace / diffusers</b>
    /// model downloaded by one is reused by the other (both resolve by repo id from the same cache, no
    /// re-download; it also persists LocalAI's diffusers downloads). NOTE: this only covers HF/diffusers
    /// models (e.g. the SDXL fine-tunes added via <see cref="AddHuggingFaceModel(IResourceBuilder{LocalAiResource}, KnownHuggingFaceImageModel, string?)"/>);
    /// LocalAI's GGML/gallery-specific image formats cannot be shared with SD.Next (different layout),
    /// and SD.Next still loads a shared model by its repo id (it won't auto-list them as local checkpoints).
    /// </param>
    public static IResourceBuilder<LocalAiResource> WithSdNextUi(
        this IResourceBuilder<LocalAiResource> builder,
        int? hostPort = null,
        string image = "vladmandic/sdnext-cuda",
        string tag = "latest",
        string? name = null,
        bool shareHfCacheWithLocalAi = false)
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

        // GPU follows the LocalAI resource's configuration (default: NVIDIA).
        if (builder.Resource.GpuEnabled)
            rb.WithContainerRuntimeArgs("--gpus", "all");

        // Shared HuggingFace cache: one volume mounted in both containers with HF_HOME pointing at it,
        // so HF/diffusers weights download once and are reused by both LocalAI and SD.Next.
        if (shareHfCacheWithLocalAi)
        {
            var hfCacheVolume = $"{builder.Resource.Name}-hfcache";
            builder.WithVolume(hfCacheVolume, "/hf-cache").WithEnvironment("HF_HOME", "/hf-cache");
            rb.WithVolume(hfCacheVolume, "/hf-cache").WithEnvironment("HF_HOME", "/hf-cache");
        }

        return builder;
    }

    /// <summary>
    /// Wires a consumer (frontend/API) to the LocalAI service. Injects one shared base URL plus
    /// the default model per modality: <c>AI_API_BASE</c>, <c>IMAGE_MODEL</c> and — when a model
    /// of that kind was added — <c>TTS_MODEL</c>, <c>STT_MODEL</c>, <c>VIDEO_MODEL</c>, <c>SOUND_MODEL</c>,
    /// <c>TEXT_MODEL</c>, <c>EMBEDDING_MODEL</c> (and <c>AI_API_KEY</c> when set). For backwards
    /// compatibility it also injects the
    /// <c>IMAGE_PROVIDER</c>/<c>IMAGE_API_BASE</c> pair so existing image clients keep working.
    /// </summary>
    /// <param name="builder">The consuming resource.</param>
    /// <param name="localAi">The LocalAI resource.</param>
    /// <param name="imageModel">Image-model override; defaults to the service's default image model.</param>
    /// <param name="apiKey">API key override, when the backend was configured with one.</param>
    public static IResourceBuilder<T> WithLocalAI<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<LocalAiResource> localAi,
        ImageModel? imageModel = null,
        string? apiKey = null)
        where T : IResourceWithEnvironment
    {
        var res = localAi.Resource;
        builder
            .WithReference(res.HttpEndpoint)
            .WithEnvironment("AI_PROVIDER", "openai-compatible")
            .WithEnvironment("AI_API_BASE", res.HttpEndpoint)
            // Back-compat: keep the IMAGE_* pair so existing image clients need no changes.
            .WithEnvironment("IMAGE_PROVIDER", "openai-compatible")
            .WithEnvironment("IMAGE_API_BASE", res.HttpEndpoint)
            // Deferred: resolves per-modality default models at startup, regardless of call order.
            .WithEnvironment(ctx =>
            {
                ctx.EnvironmentVariables["IMAGE_MODEL"] = imageModel?.Name ?? res.DefaultModel;
                var tts = res.DefaultModelFor(ModelModality.TextToSpeech);
                if (tts is not null) ctx.EnvironmentVariables["TTS_MODEL"] = tts;
                var stt = res.DefaultModelFor(ModelModality.SpeechToText);
                if (stt is not null) ctx.EnvironmentVariables["STT_MODEL"] = stt;
                var video = res.DefaultModelFor(ModelModality.Video);
                if (video is not null) ctx.EnvironmentVariables["VIDEO_MODEL"] = video;
                var sound = res.DefaultModelFor(ModelModality.Sound);
                if (sound is not null) ctx.EnvironmentVariables["SOUND_MODEL"] = sound;
                var text = res.DefaultModelFor(ModelModality.Text);
                if (text is not null) ctx.EnvironmentVariables["TEXT_MODEL"] = text;
                var embedding = res.DefaultModelFor(ModelModality.Embedding);
                if (embedding is not null) ctx.EnvironmentVariables["EMBEDDING_MODEL"] = embedding;
            });

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            builder.WithEnvironment("AI_API_KEY", apiKey);
            builder.WithEnvironment("IMAGE_API_KEY", apiKey);
        }

        return builder;
    }
}
