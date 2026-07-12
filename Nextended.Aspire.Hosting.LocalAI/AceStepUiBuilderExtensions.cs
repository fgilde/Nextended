using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.LocalAI;

/// <summary>
/// Options for <see cref="AceStepUiBuilderExtensions.WithAceStepUi"/>. Defaults run the official
/// ACE-Step 1.5 server image plus the <see href="https://github.com/fspecii/ace-step-ui">ace-step-ui</see>
/// studio built from source (the repo ships no container image).
/// </summary>
public sealed class AceStepUiOptions
{
    /// <summary>ACE-Step server image (without tag). Default <c>ghcr.io/ace-step/ace-step-1.5</c>.</summary>
    public string ApiImage { get; set; } = "ghcr.io/ace-step/ace-step-1.5";

    /// <summary>ACE-Step server image tag. Default <c>latest</c>.</summary>
    public string ApiTag { get; set; } = "latest";

    /// <summary>
    /// Fixed host port for the ACE-Step server (random if null). The UI talks to it internally either way;
    /// the endpoint also serves ACE-Step's own Gradio UI, handy for debugging.
    /// </summary>
    public int? ApiHostPort { get; set; }

    /// <summary>Git repository the UI is built from. Default <c>https://github.com/fspecii/ace-step-ui</c>.</summary>
    public string UiRepository { get; set; } = "https://github.com/fspecii/ace-step-ui";

    /// <summary>Git branch/tag of <see cref="UiRepository"/> to build. Default <c>main</c> — pin a tag/commit-ish branch for reproducible builds.</summary>
    public string UiGitRef { get; set; } = "main";

    /// <summary>Base image for the generated UI Dockerfile. Default <c>node:22-bookworm</c>.</summary>
    public string NodeImage { get; set; } = "node:22-bookworm";

    /// <summary>ACE-Step DiT model config (<c>ACESTEP_CONFIG_PATH</c>, e.g. <c>acestep-v15-turbo</c>). <c>null</c> = image default.</summary>
    public string? ConfigPath { get; set; }

    /// <summary>ACE-Step language-model path (<c>ACESTEP_LM_MODEL_PATH</c>, e.g. <c>acestep-5Hz-lm-4B</c>). <c>null</c> = image default.</summary>
    public string? LmModelPath { get; set; }

    /// <summary>Optional Pexels API key for the UI's video-background feature (<c>PEXELS_API_KEY</c>).</summary>
    public string? PexelsApiKey { get; set; }

    /// <summary>Extra environment variables for the UI container (applied last — overrides the built-in wiring).</summary>
    public IDictionary<string, string> Environment { get; } = new Dictionary<string, string>();

    /// <summary>Extra environment variables for the ACE-Step server container (applied last).</summary>
    public IDictionary<string, string> ApiEnvironment { get; } = new Dictionary<string, string>();
}

/// <summary>
/// Adds the <see href="https://github.com/fspecii/ace-step-ui">ace-step-ui</see> music studio
/// (a local Suno-style UI: song library, lyrics editor, stem separation, audio editor) to the stack.
/// </summary>
public static class AceStepUiBuilderExtensions
{
    /// <summary>
    /// Adds a full local music-generation studio: the official <b>ACE-Step 1.5</b> server
    /// (Gradio mode with <c>--enable-api</c> — the API surface ace-step-ui generates through)
    /// plus the <b>ace-step-ui</b> frontend wired to it.
    /// <para>
    /// IMPORTANT: ace-step-ui speaks ACE-Step's own REST API — <b>not</b> LocalAI's
    /// OpenAI/ElevenLabs-compatible <c>/v1/sound-generation</c>. So this runs its own ACE-Step GPU
    /// container with its own model weights (analogous to <see cref="LocalAiBuilderExtensions.WithSdNextUi"/>);
    /// it does not proxy through LocalAI, and models added via <c>AddSoundModel</c> are not shared with it.
    /// Weights (several GB) download into the <c>{name}-checkpoints</c> volume on first start.
    /// </para>
    /// <para>
    /// The UI repo ships no container image, so a Dockerfile is generated that clones and builds it
    /// from source on first run (pin <see cref="AceStepUiOptions.UiGitRef"/> for reproducibility).
    /// Dev-time only: both containers are excluded from the publish manifest.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// var ai = builder.AddLocalAI("localai")
    ///     .WithDataVolume()
    ///     .AddModel(KnownImageModel.Flux1Schnell)
    ///     .WithAceStepUi();   // music studio on a random port (UI), ACE-Step API alongside
    /// </code>
    /// </example>
    /// <param name="builder">The LocalAI resource builder (parent for grouping/GPU).</param>
    /// <param name="hostPort">Fixed host port for the UI (default random).</param>
    /// <param name="name">Resource name of the UI (default <c>{name}-acestep-ui</c>); the server becomes <c>{name}-acestep</c>.</param>
    /// <param name="configure">Tweak images, git ref, model config, Pexels key, extra env …</param>
    public static IResourceBuilder<LocalAiResource> WithAceStepUi(
        this IResourceBuilder<LocalAiResource> builder,
        int? hostPort = null,
        string? name = null,
        Action<AceStepUiOptions>? configure = null)
    {
        var appBuilder = builder.ApplicationBuilder;
        var options = new AceStepUiOptions();
        configure?.Invoke(options);

        var uiName = name ?? $"{builder.Resource.Name}-acestep-ui";
        var apiName = name is null ? $"{builder.Resource.Name}-acestep" : $"{name}-api";

        // --- ACE-Step 1.5 server (Gradio mode + REST routes) ---------------------------------------
        // ace-step-ui generates through the GRADIO app (@gradio/client → /generation_wrapper) and uses
        // the REST routes the Gradio app registers with --enable-api (/health, /v1/models, /query_result …).
        // The plain REST mode (ACESTEP_MODE=api) does NOT mount Gradio — the UI would then fall back to
        // spawning a local Python process, which cannot work from inside the UI container.
        var api = appBuilder.AddResource(new AceStepApiResource(apiName))
            .WithImage(options.ApiImage, options.ApiTag)
            .WithHttpEndpoint(port: options.ApiHostPort, targetPort: 7860, name: "http")
            .WithEnvironment("ACESTEP_MODE", "gradio")
            .WithEnvironment("ACESTEP_EXTRA_ARGS", "--enable-api")
            // /health comes from --enable-api; keeps the UI waiting until the server actually
            // accepts requests (first start downloads several GB of weights before that).
            .WithHttpHealthCheck("/health")
            .WithVolume($"{apiName}-checkpoints", "/app/checkpoints")   // model weights (several GB)
            .WithVolume($"{apiName}-outputs", "/app/gradio_outputs")
            .WithVolume($"{apiName}-datasets", "/app/datasets")          // shared with the UI for LoRA training uploads
            .WithParentRelationship(builder.Resource)
            .ExcludeFromManifest();

        if (options.ConfigPath is not null)
            api.WithEnvironment("ACESTEP_CONFIG_PATH", options.ConfigPath);
        if (options.LmModelPath is not null)
            api.WithEnvironment("ACESTEP_LM_MODEL_PATH", options.LmModelPath);

        // GPU follows the LocalAI resource's configuration (default: NVIDIA).
        if (builder.Resource.GpuEnabled)
            api.WithContainerRuntimeArgs("--gpus", "all");

        foreach (var (key, value) in options.ApiEnvironment)
            api.WithEnvironment(key, value);

        // --- ace-step-ui (built from source via a generated Dockerfile) ---------------------------
        // The repo has no image and no Dockerfile: frontend (Vite, :3000) + Express backend (:3001)
        // in one container; the Vite dev server proxies /api etc. to 127.0.0.1:3001, so only :3000
        // needs an endpoint. Dev-mode processes are fine here — the resource is dev-time only.
        var contextDir = Path.Combine(appBuilder.AppHostDirectory, "obj", "localai", uiName);
        Directory.CreateDirectory(contextDir);
        File.WriteAllText(Path.Combine(contextDir, "Dockerfile"), $"""
            # Generated by Nextended.Aspire.Hosting.LocalAI — builds {options.UiRepository} from source.
            FROM {options.NodeImage}
            RUN apt-get update \
             && apt-get install -y --no-install-recommends git python3 build-essential ca-certificates \
             && rm -rf /var/lib/apt/lists/*
            RUN git clone --depth 1 --branch {options.UiGitRef} {options.UiRepository} /app
            WORKDIR /app
            RUN npm install --no-audit --no-fund
            WORKDIR /app/server
            RUN npm install --no-audit --no-fund
            RUN mkdir -p /app/server/data /app/server/public/audio
            WORKDIR /app
            EXPOSE 3000 3001
            CMD ["sh", "-c", "(cd /app/server && npm run dev &) ; exec npm run dev -- --host 0.0.0.0 --port 3000"]
            """);

        // WithDockerfile only REPLACES an existing image annotation (AddContainer normally creates it) —
        // a custom resource added via AddResource needs the placeholder annotation up front.
        var uiResource = new AceStepUiResource(uiName);
        uiResource.Annotations.Add(new ContainerImageAnnotation { Image = uiName, Tag = "latest" });

        var ui = appBuilder.AddResource(uiResource)
            .WithDockerfile(contextDir)
            .WithHttpEndpoint(port: hostPort, targetPort: 3000, name: "http")
            .WithVolume($"{uiName}-data", "/app/server/data")            // SQLite (songs, playlists, users)
            .WithVolume($"{uiName}-audio", "/app/server/public/audio")   // generated tracks
            .WithVolume($"{apiName}-datasets", "/app/datasets")          // same volume as the server (training data)
            .WithEnvironment("ACESTEP_API_URL", api.Resource.GetEndpoint("http"))
            .WithEnvironment("DATASETS_DIR", "/app/datasets")
            .WithEnvironment("DATASETS_UPLOADS_DIR", "/app/datasets/uploads")
            .WaitFor(api)
            .WithParentRelationship(builder.Resource)
            .ExcludeFromManifest();

        if (!string.IsNullOrWhiteSpace(options.PexelsApiKey))
            ui.WithEnvironment("PEXELS_API_KEY", options.PexelsApiKey);

        foreach (var (key, value) in options.Environment)
            ui.WithEnvironment(key, value);

        return builder;
    }
}
