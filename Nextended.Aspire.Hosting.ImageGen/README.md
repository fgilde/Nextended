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

## Generating images from your app

The endpoint speaks the **OpenAI Images API** (`POST /v1/images/generations`) and returns a
base64 PNG. Read the three env vars `WithImageGeneration` injects and post a request — no SDK
required. Prefer calling it **server-side**: if you set an `ApiKey`, it must never reach the
browser, and LocalAI is meant to live on a trusted network, not be exposed publicly.

**C# (from the consuming service):**

```csharp
var baseUrl = builder.Configuration["IMAGE_API_BASE"]!;   // injected, e.g. http://localhost:5069
var model   = builder.Configuration["IMAGE_MODEL"]!;      // the default model
var apiKey  = builder.Configuration["IMAGE_API_KEY"];     // only if you set o.ApiKey

using var http = new HttpClient();
if (!string.IsNullOrEmpty(apiKey))
    http.DefaultRequestHeaders.Authorization = new("Bearer", apiKey);

var res = await http.PostAsJsonAsync($"{baseUrl}/v1/images/generations", new
{
    model,
    prompt = "a lighthouse at dawn, cinematic",
    size = "1024x1024",
    n = 1,
    response_format = "b64_json",
});
res.EnsureSuccessStatusCode();

using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
var b64 = doc.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetString();
await File.WriteAllBytesAsync("out.png", Convert.FromBase64String(b64!));
```

**TypeScript / JavaScript (Node, TanStack Start server fn, Next route handler, …):**

```ts
// Keep this on the server — never ship IMAGE_API_KEY to the browser.
const base   = process.env.IMAGE_API_BASE!;   // injected by WithImageGeneration
const model  = process.env.IMAGE_MODEL!;
const apiKey = process.env.IMAGE_API_KEY;      // optional

const res = await fetch(`${base}/v1/images/generations`, {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    ...(apiKey ? { Authorization: `Bearer ${apiKey}` } : {}),
  },
  body: JSON.stringify({
    model,
    prompt: "a lighthouse at dawn, cinematic",
    size: "1024x1024",
    n: 1,
    response_format: "b64_json",
  }),
});
if (!res.ok) throw new Error(`image backend ${res.status}: ${await res.text()}`);

const json = await res.json();
// OpenAI Images shape; extract defensively — some models/backends return a url instead.
const b64 = json.data?.[0]?.b64_json ?? json.images?.[0]?.b64_json;
const bytes = Buffer.from(b64, "base64");      // -> save, upload to storage, or return as data URI
```

Return the bytes to the browser as a normal image response or a `data:image/png;base64,…` URI.
That server-proxy shape is exactly what the promote.me app uses (`image-provider.server.ts`),
which lets it swap the Lovable gateway and this self-hosted backend behind one call site.

**Calling it straight from the browser** is possible — it's plain OpenAI-compatible HTTP — but
only in a trusted/dev setup: there must be **no API key** (nothing to leak) and the backend host
must be reachable from the browser with permissive CORS. For anything user-facing, proxy through
your own backend (as above) so you keep control of auth, rate limiting and prompt policy.

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

There are two model namespaces, added with two different methods:

- **`AddModel`** — installs a model from the [LocalAI gallery](https://localai.io/gallery.html).
  Accepts `KnownImageModel` values, plain gallery names, huggingface/OCI URIs or config URLs.
- **`AddHuggingFaceModel`** — loads any diffusers-format HuggingFace repo (not in the gallery)
  by generating a model config on the fly.

The name the consumer uses to call the model (the `model` field in the API request, and what
`WithImageGeneration` injects as `IMAGE_MODEL`) is the **gallery name** for `AddModel`, or the
model id / enum slug for `AddHuggingFaceModel`.

### LocalAI gallery models (`AddModel`)

| `KnownImageModel` | Gallery name (API `model`) | Notes |
|---|---|---|
| `StableDiffusionAio` | `stablediffusion` | Bundled with AIO images; the default when no `AddModel` is called |
| `StableDiffusion15` | `sd-1.5-ggml` | Classic SD 1.5, small & fast (GGML) |
| `StableDiffusion3Medium` | `stable-diffusion-3-medium` | SD 3 Medium |
| `StableDiffusion35Medium` | `sd-3.5-medium-ggml` | SD 3.5 Medium (GGML) |
| `StableDiffusion35Large` | `sd-3.5-large-ggml` | SD 3.5 Large (GGML), higher quality & VRAM |
| `DreamShaper` | `dreamshaper` | Popular general-purpose SD 1.5 fine-tune |
| `Flux1Schnell` | `flux.1-schnell` | FLUX.1 [schnell], fast few-step |
| `Flux1Dev` | `flux.1-dev-ggml` | FLUX.1 [dev] (GGML) |
| `Flux1DevUncensored` | `flux.1-dev-ggml-abliterated-v2-q8_0` | FLUX.1 [dev] abliterated — no content filter, GGML Q8 (~13 GB) |
| `Flux1DevUncensoredDiffusers` | `flux.1dev-abliteratedv2` | Same model, full fp16 via the python diffusers backend (~24 GB, heavy VRAM) |
| `Flux1KontextDev` | `flux.1-kontext-dev` | FLUX.1 Kontext [dev] (instruction / image editing) |
| `Flux1KreaDev` | `flux.1-krea-dev-ggml` | FLUX.1 Krea [dev] (GGML) |
| `Flux2Dev` | `flux.2-dev` | FLUX.2 [dev] |
| `Flux2Klein4b` | `flux.2-klein-4b` | FLUX.2 Klein, 4B params (lighter) |
| `Flux2Klein9b` | `flux.2-klein-9b` | FLUX.2 Klein, 9B params |
| `Ideogram4` | `ideogram-4-q8_0-ggml` | Ideogram 4 (GGML Q8) |
| `ZImageTurbo` | `z-image-turbo-diffusers` | Z-Image Turbo (diffusers backend) |
| `ZImageTurboGgml` | `Z-Image-Turbo` | Z-Image Turbo (GGML build) |
| `QwenImage` | `qwen-image` | Qwen-Image |
| `Chroma1Hd` | `chroma1-hd` | Chroma1 HD |

```csharp
imagegen
    .AddModel(KnownImageModel.Flux1Schnell)          // enum -> gallery name
    .AddModel(KnownImageModel.Flux1DevUncensored)    // uncensored FLUX (GGML Q8)
    .AddModel("dreamshaper");                         // implicit string -> ImageModel
```

### HuggingFace diffusers models (`AddHuggingFaceModel`)

Not in the LocalAI gallery — loaded from a HuggingFace repo by generating a model config that
is bind-mounted into the container; LocalAI downloads the weights on startup. Includes curated,
verified NSFW-capable SDXL models for adult platforms.

```csharp
imagegen
    .AddHuggingFaceModel(KnownHuggingFaceImageModel.RealVisXL4)      // photorealistic, NSFW-capable
    .AddHuggingFaceModel(KnownHuggingFaceImageModel.NsfwGenV2)       // explicit content, unfiltered
    .AddHuggingFaceModel("mymodel", "SG161222/RealVisXL_V5.0");      // any repo id (string overload)
```

| `KnownHuggingFaceImageModel` | HuggingFace repo | Notes |
|---|---|---|
| `RealVisXL4` | `SG161222/RealVisXL_V4.0` | Photorealistic SDXL, NSFW-capable |
| `RealVisXL5` | `SG161222/RealVisXL_V5.0` | Photorealistic SDXL v5 |
| `RealVisXL5Lightning` | `SG161222/RealVisXL_V5.0_Lightning` | Lightning variant — **6 steps** |
| `NsfwGenV2` | `UnfilteredAI/NSFW-gen-v2` | Explicit content, unfiltered |
| `NsfwGenAnime` | `UnfilteredAI/NSFW-GEN-ANIME` | Explicit anime, unfiltered |
| `NsfwV1` | `stablediffusionapi/nsfw` | Explicit content |
| `OmnigenXL` | `stablediffusionapi/omnigen-xl` | OmniGen XL |
| `OmnigenXLNsfw` | `stablediffusionapi/omnigenxlnsfwsfw-v10` | OmniGen XL, NSFW |
| `WaiIllustriousSDXL` | `votepurchase/waiIllustriousSDXL_v150` | Illustrious / anime SDXL |
| `PonyDiffusionV6XL` | `John6666/pony-diffusion-v6-xl-sdxl-spo` | Pony Diffusion v6 XL |
| `DreamShaperXLTurbo` | `Lykon/dreamshaper-xl-v2-turbo` | Turbo — **8 steps** |
| `DreamShaper8` | `Lykon/dreamshaper-8` | DreamShaper 8 (SD 1.5) |
| `AnimagineXL4` | `cagliostrolab/animagine-xl-4.0` | Anime SDXL v4 |
| `AnimagineXL31` | `cagliostrolab/animagine-xl-3.1` | Anime SDXL v3.1 |
| `PlaygroundV25` | `playgroundai/playground-v2.5-1024px-aesthetic` | Playground v2.5 |
| `SdxlBase` | `stabilityai/stable-diffusion-xl-base-1.0` | Stock SDXL base 1.0 |
| `MajicMixRealistic` | `digiplay/majicMIX_realistic_v7` | Photoreal SD 1.5 |
| `JuggernautXLv9` | `RunDiffusion/Juggernaut-XL-v9` | Juggernaut XL v9 |
| `EpicRealismXL` | `misri/epicrealismXL_v7FinalDestination` | epiCRealism XL |
| `EpicRealism15` | `emilianJR/epiCRealism` | epiCRealism (SD 1.5) |
| `RealisticVision51` | `stablediffusionapi/realistic-vision-v51` | Realistic Vision v5.1 |

These run on LocalAI's python `diffusers` backend (first use downloads the backend + weights;
SDXL-class models want ≥8 GB free VRAM). Sampler `steps` and the fp16 file-variant flag are set
**automatically per known model** — turbo/lightning models use fewer steps, and repos that don't
ship `*.fp16.safetensors` load their default-named weights (the wrong flag fails to load). For
custom repos via the string overload, tune `steps` and pass `f16: true` only if the repo ships
fp16-variant files:

```csharp
imagegen.AddHuggingFaceModel(
    name: "my-turbo",
    hfRepo: "some/sdxl-turbo-repo",
    pipelineType: "StableDiffusionXLPipeline",   // default; fits SDXL-class models
    steps: 8,                                     // turbo/lightning want ~6-8
    f16: true);                                   // only if the repo publishes fp16 files
```

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
