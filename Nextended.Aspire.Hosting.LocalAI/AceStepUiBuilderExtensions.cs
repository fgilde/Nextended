using System.Diagnostics;
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
    /// <summary>
    /// Build the ACE-Step server from <see cref="ApiRepository"/>@<see cref="ApiGitRef"/> via a generated
    /// Dockerfile instead of pulling <see cref="ApiImage"/>:<see cref="ApiTag"/>. Default <c>true</c>, and
    /// deliberately so: ace-step-ui calls the Gradio <c>/generation_wrapper</c> with POSITIONAL arguments,
    /// so server and UI must pair exactly. The UI's argument list matches the ACE-Step <b>v0.1.4</b>
    /// signature; newer releases inserted parameters, shifting positions (symptom:
    /// <i>"Value: is not in the list of choices ['euler','heun']"</i>) — and GHCR offers no v0.1.4 image,
    /// hence the source build. Set <c>false</c> only with a matching prebuilt image (see <see cref="ApiTag"/>).
    /// </summary>
    public bool BuildApiFromSource { get; set; } = true;

    /// <summary>Git repository the ACE-Step server is built from (source build only). Default <c>https://github.com/ace-step/ACE-Step-1.5</c>.</summary>
    public string ApiRepository { get; set; } = "https://github.com/ace-step/ACE-Step-1.5";

    /// <summary>
    /// Git branch/tag of <see cref="ApiRepository"/> to build (source build only). Default <c>v0.1.4</c> —
    /// the revision whose <c>/generation_wrapper</c> signature the UI is built against. If you change it,
    /// pin <see cref="UiGitRef"/> to a UI revision matching that server (see <see cref="BuildApiFromSource"/>).
    /// </summary>
    public string ApiGitRef { get; set; } = "v0.1.4";

    /// <summary>ACE-Step server image (without tag) — only used when <see cref="BuildApiFromSource"/> is <c>false</c>. Default <c>ghcr.io/ace-step/ace-step-1.5</c>.</summary>
    public string ApiImage { get; set; } = "ghcr.io/ace-step/ace-step-1.5";

    /// <summary>
    /// ACE-Step server image tag (see <see cref="ApiImage"/>). Default <c>latest</c>. WARNING: released
    /// images (0.1.8/latest) do NOT match the current UI's positional Gradio signature — only use a
    /// prebuilt image together with a <see cref="UiGitRef"/> built against exactly that server version.
    /// </summary>
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

    /// <summary>ACE-Step DiT model config (<c>ACESTEP_CONFIG_PATH</c>). Default <c>acestep-v15-turbo</c>; <c>null</c>/empty = server default.</summary>
    public string? ConfigPath { get; set; } = "acestep-v15-turbo";

    /// <summary>
    /// ACE-Step language model (<c>ACESTEP_LM_MODEL_PATH</c>) powering the UI's "thinking"/enhance features.
    /// Default <c>acestep-5Hz-lm-4B</c> (~8 GB extra VRAM); set <c>null</c>/empty to skip loading a LM.
    /// </summary>
    public string? LmModelPath { get; set; } = "acestep-5Hz-lm-4B";

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
        var apiResource = new AceStepApiResource(apiName);
        IResourceBuilder<AceStepApiResource> api;
        if (options.BuildApiFromSource)
        {
            // Match the UI: build the server from source at the pinned ref. The repo is cloned/updated on
            // the host (docker build needs a local context; offline is fine once cloned). The Dockerfile is
            // GENERATED next to the checkout — older refs like v0.1.4 ship none, and it must work with or
            // without a tracked uv.lock (hence `uv sync --frozen || uv sync`).
            var apiDir = Path.Combine(appBuilder.AppHostDirectory, "obj", "localai", apiName);
            var srcDir = Path.Combine(apiDir, "src");
            EnsureGitCheckout(options.ApiRepository, options.ApiGitRef, srcDir);
            // Keep .git out of the build context (smaller + stable layer cache); only when the
            // checkout ships no .dockerignore itself (untracked files survive the refresh reset).
            var dockerIgnore = Path.Combine(srcDir, ".dockerignore");
            if (!File.Exists(dockerIgnore))
                File.WriteAllText(dockerIgnore, ".git\n");
            var dockerfilePath = Path.Combine(apiDir, "Dockerfile");
            // ReplaceLineEndings: the heredoc entrypoint must be LF — CRLF makes bash fail ("bash\r").
            File.WriteAllText(dockerfilePath, $$"""
                # Generated by Nextended.Aspire.Hosting.LocalAI — builds {{options.ApiRepository}}@{{options.ApiGitRef}}.
                FROM nvidia/cuda:12.8.1-runtime-ubuntu22.04
                ENV DEBIAN_FRONTEND=noninteractive LANG=C.UTF-8 LC_ALL=C.UTF-8
                RUN apt-get update && apt-get install -y --no-install-recommends \
                        software-properties-common build-essential git curl wget \
                        libsndfile1 libsndfile1-dev ffmpeg libffi-dev libssl-dev \
                    && add-apt-repository ppa:deadsnakes/ppa && apt-get update \
                    && apt-get install -y --no-install-recommends python3.11 python3.11-dev python3.11-venv \
                    && rm -rf /var/lib/apt/lists/*
                COPY --from=ghcr.io/astral-sh/uv:latest /uv /uvx /bin/
                WORKDIR /app
                COPY . /app/
                # Pin torchcodec to the release matching torch 2.10 (per the official torchcodec<->torch
                # table). v0.1.4 ships no uv.lock, so an unconstrained `uv sync` pulls the newest torchcodec
                # (0.14, built for torch 2.11 / CUDA 13) — it then fails to load on this CUDA-12.8 image and
                # audio decoding (cover/reference input) dies with "Source audio is invalid". text2music is
                # unaffected because it decodes no input. The `>=0.9.1` spec becomes `==0.10.0`.
                RUN sed -i -E 's/torchcodec[><=!~ ]*[0-9][^";]*/torchcodec==0.10.0/' pyproject.toml
                RUN uv sync --frozen --no-dev --python python3.11 || uv sync --no-dev --python python3.11
                RUN mkdir -p /app/checkpoints /app/gradio_outputs /app/output
                ENV GRADIO_SERVER_NAME=0.0.0.0 ACESTEP_API_HOST=0.0.0.0 TOKENIZERS_PARALLELISM=false
                EXPOSE 7860 8001
                COPY <<'ENTRYPOINT_EOF' /app/docker-entrypoint.sh
                #!/usr/bin/env bash
                set -e
                INIT_ARGS=""
                if [ "${ACESTEP_INIT_SERVICE:-true}" = "true" ]; then
                    INIT_ARGS="--init_service true"
                    [ -n "${ACESTEP_CONFIG_PATH:-}" ]   && INIT_ARGS="${INIT_ARGS} --config_path ${ACESTEP_CONFIG_PATH}"
                    [ -n "${ACESTEP_LM_MODEL_PATH:-}" ] && INIT_ARGS="${INIT_ARGS} --init_llm true --lm_model_path ${ACESTEP_LM_MODEL_PATH}"
                fi
                # --no-sync: run in the image's prebuilt venv as-is. Without it, `uv run` re-resolves the
                # (lockfile-less) project on every start and reinstalls torchcodec 0.14, undoing the pin above.
                if [ "${ACESTEP_MODE:-gradio}" = "api" ]; then
                    exec uv run --no-sync python -m acestep.api_server --host "${ACESTEP_API_HOST:-0.0.0.0}" --port "${ACESTEP_API_PORT:-8001}" ${ACESTEP_EXTRA_ARGS:-}
                else
                    exec uv run --no-sync python -m acestep.acestep_v15_pipeline --server-name "${GRADIO_SERVER_NAME:-0.0.0.0}" --port "${GRADIO_PORT:-7860}" ${INIT_ARGS} ${ACESTEP_EXTRA_ARGS:-}
                fi
                ENTRYPOINT_EOF
                RUN chmod +x /app/docker-entrypoint.sh
                ENTRYPOINT ["/app/docker-entrypoint.sh"]
                """.ReplaceLineEndings("\n"));

            apiResource.Annotations.Add(new ContainerImageAnnotation { Image = apiName, Tag = "latest" });
            api = appBuilder.AddResource(apiResource).WithDockerfile(srcDir, dockerfilePath);
        }
        else
        {
            api = appBuilder.AddResource(apiResource).WithImage(options.ApiImage, options.ApiTag);
        }
        api = api
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

        // Always set both (empty = feature off) so the prebuilt image's env defaults can't override
        // a deliberate null and behavior matches the source-built image.
        api.WithEnvironment("ACESTEP_CONFIG_PATH", options.ConfigPath ?? "");
        api.WithEnvironment("ACESTEP_LM_MODEL_PATH", options.LmModelPath ?? "");

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
        File.WriteAllText(Path.Combine(contextDir, "Dockerfile"), $$"""
            # Generated by Nextended.Aspire.Hosting.LocalAI — builds {{options.UiRepository}} from source.
            FROM {{options.NodeImage}}
            RUN apt-get update \
             && apt-get install -y --no-install-recommends git python3 build-essential ca-certificates ffmpeg \
             && rm -rf /var/lib/apt/lists/*
            RUN git clone --depth 1 --branch {{options.UiGitRef}} {{options.UiRepository}} /app
            WORKDIR /app
            # Fix an upstream bug in buildGradioArgs: it passes is_format_caption_state, but that is a
            # gr.State — @gradio/client skips State slots itself, so sending it shifts every argument
            # after index 36 by one position (breaking latent/normalization/autogen values server-side).
            # If upstream removes the line, this sed is a no-op.
            RUN sed -i '/params.isFormatCaption ?? false,/d' /app/server/src/services/acestep.ts
            # Container resilience: the Python-spawn fallback can never work here (no local ACE-Step
            # install), so instead of falling into it while the server container is still loading
            # models (cold start takes minutes), wait for the Gradio API — up to 15 min, with status.
            RUN sed -i "s|const gradioUp = await isGradioAvailable();|let gradioUp = await isGradioAvailable(); for (let w = 0; w < 90 \&\& !gradioUp; w++) { job.stage = 'Waiting for ACE-Step server to come online...'; await new Promise(r => setTimeout(r, 10000)); gradioUp = await isGradioAvailable(); }|" /app/server/src/services/acestep.ts
            # Upload fix for cover/reference audio: a bare Blob reaches Gradio without a filename, so the
            # server stores it extension-less and cannot decode it ("Source audio is invalid, unreadable,
            # or silent"). A File with its original name keeps the extension intact.
            RUN sed -i 's|const blob = new Blob(\[buffer\], { type: mimeType });|const blob = new File([buffer], path.basename(filePath), { type: mimeType });|' /app/server/src/services/acestep.ts
            RUN npm install --no-audit --no-fund
            WORKDIR /app/server
            RUN npm install --no-audit --no-fund
            RUN mkdir -p /app/server/data /app/server/public/audio
            WORKDIR /app
            EXPOSE 3000 3001
            CMD ["sh", "-c", "(cd /app/server && npm run dev &) ; exec npm run dev -- --host 0.0.0.0 --port 3000"]
            """.ReplaceLineEndings("\n"));

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

    /// <summary>
    /// Clones <paramref name="repository"/>@<paramref name="gitRef"/> into <paramref name="dir"/> (shallow),
    /// or refreshes an existing checkout to that ref. A failed refresh (offline) keeps the existing
    /// checkout; the initial clone is required and throws with a clear message.
    /// </summary>
    private static void EnsureGitCheckout(string repository, string gitRef, string dir)
    {
        if (!Directory.Exists(Path.Combine(dir, ".git")))
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            Directory.CreateDirectory(Path.GetDirectoryName(dir)!);
            // autocrlf=false: a Windows checkout would otherwise stamp CRLF into the repo's Dockerfile,
            // whose heredoc-generated entrypoint script then fails in the container ("bash\r: not found").
            if (!RunGit($"clone -c core.autocrlf=false --depth 1 --branch {gitRef} {repository} \"{dir}\"", workDir: null))
                throw new InvalidOperationException(
                    $"Konnte '{repository}' ({gitRef}) nicht nach '{dir}' klonen — git muss installiert und das Repo erreichbar sein. " +
                    "Alternativ BuildApiFromSource=false setzen, um das fertige Image zu verwenden.");
        }
        else if (RunGit($"fetch --depth 1 origin {gitRef}", dir))
        {
            RunGit("reset --hard FETCH_HEAD", dir);
        }
    }

    /// <summary>Runs git with output passing through to the AppHost console; <c>false</c> on failure (incl. git missing).</summary>
    private static bool RunGit(string args, string? workDir)
    {
        try
        {
            var psi = new ProcessStartInfo("git", args) { UseShellExecute = false };
            if (workDir is not null) psi.WorkingDirectory = workDir;
            using var process = Process.Start(psi);
            if (process is null) return false;
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
