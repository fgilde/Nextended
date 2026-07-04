# Nextended.Aspire.Hosting.ImageGen

Self-hosted, **OpenAI-compatible image generation** (text-to-image) for .NET Aspire —
the missing counterpart to `AddOllama`. Runs [LocalAI](https://github.com/mudler/LocalAI)
as a container resource, exposes `POST /v1/images/generations`, supports NVIDIA/AMD GPUs,
gallery model management and an optional Open WebUI.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var imagegen = builder.AddImageGeneration("imagegen")   // NVIDIA GPU + AIO image by default
    .WithDataVolume()                                   // persist model downloads
    .AddModel(KnownImageModel.Flux1Schnell)             // install from the LocalAI gallery
    .AddModel("dreamshaper")                            // implicit string -> ImageModel
    .WithOpenWebUI();                                   // dev-time UI (excluded from publish)

builder.AddProject<Projects.Web>("web")
    .WithImageGeneration(imagegen);                     // injects IMAGE_PROVIDER / IMAGE_API_BASE / IMAGE_MODEL
```

## What the consumer gets

`WithImageGeneration(...)` injects environment variables into the consuming resource:

| Variable | Value |
|---|---|
| `IMAGE_PROVIDER` | `openai-compatible` |
| `IMAGE_API_BASE` | the service endpoint (call `{IMAGE_API_BASE}/v1/images/generations`) |
| `IMAGE_MODEL` | the default model (first `AddModel`, else the AIO-bundled `stablediffusion`) |
| `IMAGE_API_KEY` | only when configured |

Any OpenAI images client works unchanged:

```http
POST {IMAGE_API_BASE}/v1/images/generations
{ "model": "flux.1-schnell", "prompt": "a lighthouse at dawn", "size": "1024x1024", "response_format": "b64_json" }
```

## Options

```csharp
builder.AddImageGeneration("imagegen", o =>
{
    o.Gpu = ImageGenGpu.Nvidia;              // None | Nvidia | Amd
    o.Image = "localai/localai";
    o.Tag = "latest-aio-gpu-nvidia-cuda-12"; // AIO = batteries included; slim tags also work
    o.HostPort = 5069;                       // fixed host port (optional)
    o.AioProfile = "gpu-8g";                 // force AIO profile (see GPU note below)
    o.ApiKey = "my-secret";                  // require a bearer key
});
```

## Models

`AddModel` accepts `KnownImageModel` values, plain gallery names, huggingface/OCI URIs or
config URLs. Known models include (gallery names in parentheses):

- `StableDiffusionAio` (`stablediffusion`, bundled with AIO images)
- `StableDiffusion15` / `StableDiffusion3Medium` / `StableDiffusion35Medium` / `StableDiffusion35Large`
- `DreamShaper` (`dreamshaper`)
- `Flux1Schnell` / `Flux1Dev` / `Flux1KontextDev` / `Flux2Dev`
- `Flux1DevUncensored` (`flux.1-dev-ggml-abliterated-v2-q8_0`) — no content filtering, GGML Q8 (~13 GB)
- `Flux1DevUncensoredDiffusers` (`flux.1dev-abliteratedv2`) — same model as full fp16 via the
  python diffusers backend (~24 GB, heavy VRAM)
- `QwenImage`, `Chroma1Hd`, `ZImageTurbo`

Additional gallery models: `Flux1KreaDev`, `Flux2Klein4b`/`Flux2Klein9b`, `Ideogram4`,
`ZImageTurboGgml`.

### HuggingFace models (not in the gallery)

`AddHuggingFaceModel` loads any diffusers-format HF repo by generating a model config —
including curated, verified NSFW-capable SDXL models for adult platforms:

```csharp
imagegen
    .AddHuggingFaceModel(KnownHuggingFaceImageModel.RealVisXL4)      // photorealistic, NSFW-capable
    .AddHuggingFaceModel(KnownHuggingFaceImageModel.NsfwGenV2)       // explicit content, unfiltered
    .AddHuggingFaceModel("mymodel", "SG161222/RealVisXL_V5.0");      // any repo id
```

Known HF models: `RealVisXL4`, `RealVisXL5`, `NsfwGenV2`, `NsfwGenAnime`, `NsfwV1`,
`OmnigenXL`, `OmnigenXLNsfw`, `WaiIllustriousSDXL`, `PonyDiffusionV6XL`,
`DreamShaperXLTurbo` (8 steps), `AnimagineXL4`, `PlaygroundV25`.
These run on LocalAI's python `diffusers` backend (first use downloads the backend + weights;
SDXL-class models want ≥8 GB free VRAM). The correct fp16 file-variant flag is set
**automatically per known model** (repos ship either `*.fp16.safetensors` or default-named
weights — the wrong flag fails to load). For custom repos via the string overload, pass
`f16: true` only if the repo ships fp16-variant files.

> **Changing/adding models on an existing stack:** LocalAI imports each model config into its
> `/models` volume on first start and does **not** overwrite it later. After changing models
> (or this package), drop the volume so configs re-import: `docker volume rm <name>-models`.

> Note: LocalAI gallery names (used with `AddModel`) and HuggingFace repos (used with
> `AddHuggingFaceModel`) are different namespaces. A gallery name like `nsfw-v1` does **not**
> exist in the gallery — use `AddHuggingFaceModel(KnownHuggingFaceImageModel.NsfwV1)` instead.

Browse everything at <https://localai.io/gallery.html>.

Behavior notes:

- **No `AddModel`** → AIO images load their bundled default set (image-gen, chat, tts, …).
- **With `AddModel`** → only the added models are downloaded/loaded (the AIO default set
  is overridden via the `MODELS` env var).
- `WithDataVolume()` persists `/models` **and** `/backends` — both downloads are multi-GB
  and only happen once.

## GPU notes

- **NVIDIA**: the container gets `--gpus all`. Requirements: recent NVIDIA driver +
  Docker Desktop (WSL2) or NVIDIA Container Toolkit on Linux.
  Verify passthrough with: `docker run --rm --gpus all ubuntu nvidia-smi`
- **Docker Desktop / WSL2 quirk**: LocalAI's AIO images detect GPUs via `lspci`, which does
  not see the WSL2 GPU even when `--gpus all` works. This package therefore forces
  `PROFILE=gpu-8g` for NVIDIA + AIO automatically (override via `AioProfile`).
- First start downloads several GB (backends + models) — be patient, then use
  `WithDataVolume()` so it only happens once.

## UIs

- **LocalAI WebUI** is built in — open the resource endpoint in the browser (has a real
  text-to-image tab, unlike Open WebUI's chat-centric flow).
- **SD.Next** (recommended for image work): `WithSdNextUi()` adds a full
  [SD.Next](https://github.com/vladmandic/sdnext) studio — proper txt2img/img2img UI, model &
  LoRA management, Civitai/HuggingFace downloads. Runs its own GPU container with its own
  models (independent of LocalAI). Default image `vladmandic/sdnext-cuda:latest`, UI on port 7860.
- **Open WebUI**: `WithOpenWebUI()` adds a `ghcr.io/open-webui/open-webui` container. Overloads let
  you reuse an existing Open WebUI (e.g. the one from the Ollama integration) instead of a second one:
  ```csharp
  var ollama = builder.AddOllama("ollama").WithOpenWebUI();  // Ollama's Open WebUI
  var imagegen = builder.AddImageGeneration("imagegen").AddModel(KnownImageModel.Flux1Schnell)
      .WithOpenWebUI(useExistingIfFound: true);              // reuse it, add image models
  // or pass it explicitly:
  var ui = builder.Resources.OfType<OpenWebUIResource>().FirstOrDefault();
  if (ui is not null) imagegen.WithOpenWebUI(ui);
  ```
  Note Open WebUI's image generation is awkward — you chat with a *text* model and press the image
  button; selecting an image model as a chat model yields `unimplemented` (it hits
  `/v1/chat/completions`). Prefer SD.Next or the LocalAI WebUI for pure image generation.

```csharp
var imagegen = builder.AddImageGeneration("imagegen")
    .WithDataVolume()
    .AddHuggingFaceModel(KnownHuggingFaceImageModel.NsfwV1)
    .WithSdNextUi()      // full image studio on :7860
    .WithOpenWebUI();    // optional, chat-first
```

All UIs are dev-time only (`ExcludeFromManifest`).
