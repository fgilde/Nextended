---
title: Nextended.Aspire.Hosting.LocalAI — API-Referenz
---

# Nextended.Aspire.Hosting.LocalAI — API-Referenz

🇬🇧 [This page in English](/projects/aspire-localai-api)

Die vollständige öffentliche Oberfläche von `Nextended.Aspire.Hosting.LocalAI`, erzeugt aus der gebauten Assembly.

::: info Generiert
Diese Seite wird von `tools/ApiRef` aus der kompilierten Assembly erzeugt — sie zeigt auch Member ohne XML-Kommentar und kann daher nicht vom Code abweichen. Nicht von Hand bearbeiten.
:::

↩ [Zurück zur Paketseite](/de/projects/aspire-localai)

## Nextended.Aspire.Hosting.LocalAI

### `AceStepApiResource`

`class`

The ACE-Step 1.5 server (Gradio + REST API) backing an `AceStepUiResource`.

**Konstruktoren**

- `AceStepApiResource(string name)`
  <br>The ACE-Step 1.5 server (Gradio + REST API) backing an `AceStepUiResource`.

### `AceStepUiBuilderExtensions`

`static class`

Adds the `ace-step-ui` music studio (a local Suno-style UI: song library, lyrics editor, stem separation, audio editor) to the stack.

### `AceStepUiOptions`

`class`

Options for `WithAceStepUi`. Defaults run the official ACE-Step 1.5 server image plus the `ace-step-ui` studio built from source (the repo ships no container image).

**Konstruktoren**

- `AceStepUiOptions()`

**Eigenschaften**

- `ApiEnvironment : IDictionary<string, string> { get; }`
  <br>Extra environment variables for the ACE-Step server container (applied last).
- `ApiGitRef : string { get; set; }`
  <br>Git branch/tag of `ApiRepository` to build (source build only). Default `v0.1.4` — the revision whose `/generation_wrapper` signature the UI is built against. If you change it, pin `UiGitRef` to a UI revision matching that server (see `BuildApiFromSource`).
- `ApiHostPort : int? { get; set; }`
  <br>Fixed host port for the ACE-Step server (random if null). The UI talks to it internally either way; the endpoint also serves ACE-Step's own Gradio UI, handy for debugging.
- `ApiImage : string { get; set; }`
  <br>ACE-Step server image (without tag) — only used when `BuildApiFromSource` is `false`. Default `ghcr.io/ace-step/ace-step-1.5`.
- `ApiRepository : string { get; set; }`
  <br>Git repository the ACE-Step server is built from (source build only). Default `https://github.com/ace-step/ACE-Step-1.5`.
- `ApiTag : string { get; set; }`
  <br>ACE-Step server image tag (see `ApiImage`). Default `latest`. WARNING: released images (0.1.8/latest) do NOT match the current UI's positional Gradio signature — only use a prebuilt image together with a `UiGitRef` built against exactly that server version.
- `BuildApiFromSource : bool { get; set; }`
  <br>Build the ACE-Step server from `ApiRepository`@`ApiGitRef` via a generated Dockerfile instead of pulling `ApiImage`:`ApiTag`. Default `true`, and deliberately so: ace-step-ui calls the Gradio `/generation_wrapper` with POSITIONAL arguments, so server and UI must pair exactly. The UI's argument list matches the ACE-Step v0.1.4 signature; newer releases inserted parameters, shifting positions (symptom: "Value: is not in the list of choices ['euler','heun']") — and GHCR offers no v0.1.4 image, hence the source build. Set `false` only with a matching prebuilt image (see `ApiTag`).
- `ConfigPath : string { get; set; }`
  <br>ACE-Step DiT model config (`ACESTEP_CONFIG_PATH`). Default `acestep-v15-turbo`; `null`/empty = server default.
- `Environment : IDictionary<string, string> { get; }`
  <br>Extra environment variables for the UI container (applied last — overrides the built-in wiring).
- `LmModelPath : string { get; set; }`
  <br>ACE-Step language model (`ACESTEP_LM_MODEL_PATH`) powering the UI's "thinking"/enhance features. Default `acestep-5Hz-lm-4B` (~8 GB extra VRAM); set `null`/empty to skip loading a LM.
- `NodeImage : string { get; set; }`
  <br>Base image for the generated UI Dockerfile. Default `node:22-bookworm`.
- `PexelsApiKey : string { get; set; }`
  <br>Optional Pexels API key for the UI's video-background feature (`PEXELS_API_KEY`).
- `UiGitRef : string { get; set; }`
  <br>Git branch/tag of `UiRepository` to build. Default `main` — pin a tag/commit-ish branch for reproducible builds.
- `UiRepository : string { get; set; }`
  <br>Git repository the UI is built from. Default `https://github.com/fspecii/ace-step-ui`.

### `AceStepUiResource`

`class`

The ace-step-ui music studio (Suno-style frontend for ACE-Step), built from source.

**Konstruktoren**

- `AceStepUiResource(string name)`
  <br>The ace-step-ui music studio (Suno-style frontend for ACE-Step), built from source.

### `ImageModel`

`class`

A text-to-image model reference. Implicitly convertible from `String` (any LocalAI gallery name, OCI/huggingface URI or config URL) and from `KnownImageModel` for a friendly, discoverable API: `imagegen.AddModel(KnownImageModel.Flux1Schnell); imagegen.AddModel("dreamshaper");`

**Konstruktoren**

- `ImageModel(KnownImageModel model)`
- `ImageModel(string name)`

**Methoden**

- `F16Of(KnownHuggingFaceImageModel model) : bool`
  <br>Whether to load the fp16 file variant. This is per-repo: some repos ship ONLY `*.fp16.safetensors` (need f16=true), others ONLY default-named weights (need f16=false); the wrong setting fails with "variant=fp16, no such files" or "necessary safetensors weights ... (variant=None)". Values verified against each repo's unet folder. "BOTH"-repos use fp16 to save VRAM.
- `NameOf(KnownImageModel model) : string`
  <br>Resolves the gallery name of a `KnownImageModel` via its `DescriptionAttribute`.
- `RepoOf(KnownHuggingFaceImageModel model) : string`
  <br>Resolves the HF repo id of a `KnownHuggingFaceImageModel`.
- `StepsOf(KnownHuggingFaceImageModel model) : int`
  <br>Recommended sampler steps for a known HF model (turbo models need few).

**Eigenschaften**

- `Name : string { get; }`
  <br>The model id consumers pass to the API (and shown in /v1/models).

### `KnownEmbeddingModel`

`enum`

Well-known text-embedding models from the LocalAI gallery, installed via `AddEmbeddingModel` and served on `/v1/embeddings` (semantic search / RAG). Any other works via the string overload.

**Werte**

- `AllMiniLmL6v2`
  <br>all-MiniLM-L6-v2 — sehr kleiner, schneller Sentence-Transformer-Klassiker.
- `AllMiniLmL6v2OpenVino`
  <br>all-MiniLM-L6-v2 (OpenVINO) — CPU-optimierte Variante.
- `BertEmbeddings`
  <br>BERT embeddings — klein, universell, guter Default.
- `BgeM3`
  <br>BGE-M3 (ColBERT) — multilingual, Multi-Vektor.
- `EmbeddingGemma300m`
  <br>Google EmbeddingGemma 300M.
- `GraniteEmbeddingEn`
  <br>IBM Granite Embedding 125M (Englisch).
- `GraniteEmbeddingMulti`
  <br>IBM Granite Embedding 107M (multilingual).
- `MultilingualE5Base`
  <br>Multilingual E5 Base (OpenVINO) — mehrsprachige Embeddings, CPU-optimiert.
- `NomicEmbedText`
  <br>Nomic Embed Text v1.5 — starke, offene Embeddings.
- `Qwen3Embedding0_6b`
  <br>Qwen3 Embedding 0.6B.
- `Qwen3Embedding4b`
  <br>Qwen3 Embedding 4B.
- `Qwen3Embedding8b`
  <br>Qwen3 Embedding 8B.
- `Qwen3VlEmbedding2b`
  <br>Qwen3-VL Embedding 2B — multimodale (Text+Bild) Embeddings.
- `Qwen3VlEmbedding8b`
  <br>Qwen3-VL Embedding 8B — größere multimodale Embeddings.
- `Qwen3VlReranker2b`
  <br>Qwen3-VL Reranker 2B — multimodaler Reranker für RAG.
- `Qwen3VlReranker8b`
  <br>Qwen3-VL Reranker 8B — größerer multimodaler Reranker.
- `value__`

### `KnownHuggingFaceImageLora`

`enum`

Curated, well-known text-to-image LoRA adapters hosted on HuggingFace. A LoRA is NOT a standalone model — it is applied on top of a base checkpoint. `AddModel`/`AddImageLora` generate a diffusers config that (a) downloads the LoRA `.safetensors` into the model dir and (b) applies it on the matching base (here all on SDXL base 1.0). The `DescriptionAttribute` holds the LoRA repo id; base + weight file are resolved in `ImageLora`. These are general-purpose style LoRAs — put your own domain-specific LoRAs directly in your AppHost via `AddImageLora(name, base, "owner/repo")`. Most need a trigger phrase in the prompt (noted below).

**Werte**

- `GraphicNovel`
  <br>Graphic Novel Illustration (blink7630) — inked comic/graphic-novel look. Trigger: "graphic novel illustration".
- `IkeaInstructions`
  <br>IKEA Instructions (ostris) — flat IKEA-manual illustration style. Trigger: "ikea instructions".
- `LineArtManga`
  <br>Line-Art / Manga (artificialguybr, LineAniRedmond V2) — clean line art. Trigger: "LineAniAF" / "lineart".
- `Papercut`
  <br>Papercut (TheLastBen) — layered paper-cutout look. Trigger: "papercut".
- `PixelArt`
  <br>Pixel Art XL (nerijs) — retro pixel-art style. Trigger: "pixel art".
- `ThreeDRender`
  <br>3D Render Style (goofyai) — clean 3D-render aesthetic. Trigger: "3d style" / "3d render".
- `ToyFace`
  <br>Toy Face (CiroN2022) — cute 3D toy/figurine faces. Trigger: "toy_face".
- `value__`

### `KnownHuggingFaceImageModel`

`enum`

Curated text-to-image models hosted on HuggingFace (diffusers format) that are NOT in the LocalAI gallery. Loaded via `AddHuggingFaceModel`, which generates a diffusers model config on the fly. The `DescriptionAttribute` holds the HF repo id. All repos verified public/ungated.

**Werte**

- `AbsoluteReality`
  <br>AbsoluteReality v1.8.1 (SD1.5) — photoreal SD1.5 all-rounder.
- `AnimagineXL31`
  <br>Animagine XL 3.1 — high-quality anime SDXL (predecessor of 4.0).
- `AnimagineXL4`
  <br>Animagine XL 4.0 — high-quality anime SDXL.
- `CounterfeitV3`
  <br>Counterfeit V3.0 (SD1.5) — high-quality anime SD1.5.
- `CyberRealistic`
  <br>CyberRealistic v3.3 (SD1.5) — photoreal SD1.5, renders NSFW.
- `DeliberateV2`
  <br>Deliberate v2 (SD1.5) — versatile, widely used SD1.5 model.
- `DreamShaper7`
  <br>DreamShaper 7 (SD1.5) — popular all-rounder, predecessor of DreamShaper 8.
- `DreamShaper8`
  <br>DreamShaper 8 — extremely popular SD1.5 all-rounder.
- `DreamShaperXLTurbo`
  <br>DreamShaper XL v2 Turbo — fast SDXL turbo (few steps).
- `EpicRealism15`
  <br>epiCRealism (SD1.5) — beloved photoreal SD1.5 model.
- `EpicRealismXL`
  <br>epiCRealism XL v7 (Final Destination) — photorealistic SDXL.
- `IllustriousXL`
  <br>Illustrious XL v0.1 — the base Illustrious anime SDXL model.
- `JuggernautXLv8`
  <br>Juggernaut XL v8 (RunDiffusion) — earlier, very popular photorealistic SDXL.
- `JuggernautXLv9`
  <br>Juggernaut XL v9 — top-tier photorealistic SDXL.
- `MajicMixRealistic`
  <br>majicMIX realistic v7 — photoreal SD1.5 (portraits, Asian aesthetics).
- `MeinaMix`
  <br>MeinaMix v11 (SD1.5) — extremely popular anime SD1.5.
- `NeverEndingDream`
  <br>NeverEnding Dream (SD1.5) — anime/semi-real SD1.5.
- `NoobAiXL`
  <br>NoobAI-XL (NAI-XL) EPS v1.1 — Illustrious-based anime SDXL, uncensored.
- `NsfwGenAnime`
  <br>UnfilteredAI NSFW-GEN-ANIME — anime-style NSFW SDXL.
- `NsfwGenV2`
  <br>UnfilteredAI NSFW-gen v2 — SDXL tuned for explicit content, unfiltered.
- `NsfwV1`
  <br>NSFW v1.0 (SDXL) — the "nsfw-v1" model from LocalAIHub.
- `OmnigenXL`
  <br>OmnigenXL — NSFW/SFW all-rounder SDXL.
- `OmnigenXLNsfw`
  <br>OmnigenXL NSFW/SFW v1.0 — the "omnigen-nsfw-v10" model from LocalAIHub.
- `PlaygroundV25`
  <br>Playground v2.5 — aesthetic-focused SDXL-class model.
- `PonyDiffusionV6XL`
  <br>Pony Diffusion V6 XL (SPO, diffusers) — hugely popular anime/furry SDXL, uncensored.
- `RealVisXL4`
  <br>RealVisXL V4.0 — photorealistic SDXL, renders NSFW without filters.
- `RealVisXL5`
  <br>RealVisXL V5.0 — newest photorealistic RealVis SDXL.
- `RealVisXL5Lightning`
  <br>RealVisXL V5.0 Lightning — photoreal SDXL, ~6 steps (very fast).
- `RealisticVision51`
  <br>Realistic Vision v5.1 — classic photoreal SD1.5.
- `SdxlBase`
  <br>Stable Diffusion XL base 1.0 — the reference SDXL model.
- `SdxlTurbo`
  <br>SDXL Turbo — single-/few-step SDXL from Stability AI (very fast).
- `StableDiffusion15Diffusers`
  <br>Stable Diffusion v1.5 (reference SD1.5 checkpoint, diffusers).
- `WaiIllustriousSDXL`
  <br>waiIllustrious SDXL v15 — anime/illustrious NSFW SDXL.
- `value__`

### `KnownHuggingFaceTextModel`

`enum`

Curated chat/LLM models hosted on HuggingFace that are NOT in the LocalAI gallery (the UnfilteredAI uncensored family). Loaded via the generated-config `AddTextModel(name, model, backend)` — the TEXT counterpart of `AddHuggingFaceModel` (which is image/diffusers-only). Served on `/v1/chat/completions`. The `DescriptionAttribute` carries the load reference: a `huggingface://owner/repo/file.gguf` URI runs on the robust `llama-cpp` backend, a bare `owner/repo` id runs the full safetensors weights on the `vllm` backend (more VRAM, and vllm must support the architecture — verify on your GPU). All repos verified public/text-generation.

**Werte**

- `BadMistral_1_5b`
  <br>UnfilteredAI BADMISTRAL-1.5B — tiny uncensored Mistral-class (safetensors → vllm).
- `DanL3R1_8b`
  <br>UnfilteredAI DAN-L3-R1-8B — uncensored Llama-3 8B ("DAN" reasoning). GGUF f16 → llama-cpp (robust).
- `DanQwen35_4b`
  <br>UnfilteredAI Dan-Qwen3.5-4B — uncensored Qwen3 4B (safetensors → vllm).
- `DanQwen3_1_7b`
  <br>UnfilteredAI DAN-Qwen3-1.7B — small uncensored Qwen3 (safetensors → vllm).
- `HelveteNano`
  <br>UnfilteredAI Helvete-nano — nano uncensored model (safetensors → vllm).
- `NsfwFlash`
  <br>UnfilteredAI NSFW-flash — uncensored NSFW chat model. GGUF Q4_K_M → llama-cpp (robust).
- `UnfilteredAi_1b`
  <br>UnfilteredAI UNfilteredAI-1B — 1B uncensored base (safetensors → vllm).
- `value__`

### `KnownImageModel`

`enum`

Well-known text-to-image models from the LocalAI model gallery. The `DescriptionAttribute` holds the exact installable gallery name.

**Werte**

- `Chroma1Hd`
  <br>Chroma1-HD — 8.9B text-to-image derived from FLUX.1-schnell.
- `DreamShaper`
  <br>DreamShaper — popular general-purpose SD fine-tune.
- `Flux1Dev`
  <br>FLUX.1 dev (GGML quantized).
- `Flux1DevDiffusers`
  <br>FLUX.1 dev (diffusers, full fp16) — reference FLUX.1-dev; large download, heavy VRAM.
- `Flux1DevGgmlQ8`
  <br>FLUX.1 dev (GGML Q8_0) — higher-quality quant than the default GGML build; more VRAM.
- `Flux1DevUncensored`
  <br>FLUX.1 dev abliterated v2 (GGML Q8) — uncensored variant without content filtering. Runs on the lightweight stablediffusion-ggml backend (~13 GB download).
- `Flux1DevUncensoredDiffusers`
  <br>FLUX.1 dev abliterated v2 (full fp16, diffusers backend) — highest quality, but a ~24 GB download and heavy VRAM usage. Prefer `Flux1DevUncensored` locally.
- `Flux1KontextDev`
  <br>FLUX.1 Kontext dev — image editing / in-context generation.
- `Flux1KreaDev`
  <br>FLUX.1 Krea dev (GGML) — photographic aesthetic without the "flux look".
- `Flux1KreaDevGgmlQ8`
  <br>FLUX.1 Krea dev (GGML Q8_0) — higher-quality quant of the photographic Krea model.
- `Flux1Schnell`
  <br>FLUX.1 schnell — fast, high-quality (Apache-2.0).
- `Flux2Dev`
  <br>FLUX.2 dev — newest FLUX generation.
- `Flux2Klein4b`
  <br>FLUX.2 klein 4B — small and fast FLUX.2 (low VRAM).
- `Flux2Klein9b`
  <br>FLUX.2 klein 9B — mid-size FLUX.2.
- `Ideogram4`
  <br>Ideogram 4 (GGML Q8) — very strong text rendering.
- `Ideogram4Iq4nl`
  <br>Ideogram 4 (GGML IQ4_NL) — smaller/faster Ideogram 4 quant with strong text rendering.
- `QwenImage`
  <br>Qwen-Image — strong text rendering inside images.
- `QwenImageEdit`
  <br>Qwen-Image-Edit — instruction-driven image editing (edit an input image from a prompt).
- `QwenImageEdit2509`
  <br>Qwen-Image-Edit 2509 — updated (Sept 2025) Qwen image-editing model.
- `StableDiffusion15`
  <br>Classic Stable Diffusion 1.5 (GGML, small and fast).
- `StableDiffusion35Large`
  <br>Stable Diffusion 3.5 Large (GGML) — high quality, needs more VRAM.
- `StableDiffusion35Medium`
  <br>Stable Diffusion 3.5 Medium (GGML).
- `StableDiffusion3Medium`
  <br>Stable Diffusion 3 Medium.
- `StableDiffusionAio`
  <br>Bundled default of the LocalAI AIO images (no extra download).
- `ZImageDiffusers`
  <br>Z-Image (diffusers) — full (non-turbo) Z-Image, higher quality than the turbo variant.
- `ZImageTurbo`
  <br>Z-Image Turbo (diffusers) — very fast generations.
- `ZImageTurboGgml`
  <br>Z-Image Turbo (GGML) — very fast generations on the ggml backend.
- `ZImageTurboVllm`
  <br>Z-Image Turbo via the vllm-omni backend — fast generations served through vLLM.
- `value__`

### `KnownSoundModel`

`enum`

Well-known text-to-sound / MUSIC generation models from the LocalAI gallery, installed via `AddSoundModel`. Served on the ElevenLabs-compatible `POST /v1/sound-generation` (fields `model_id` + `text`, plus optional music metadata like `lyrics`, `bpm`, `duration_seconds`). Returns binary audio (wav/flac/mp3). Weights are multi-GB and generation is GPU-bound — combine with `WithDataVolume`. Any other gallery model with a `sound_generation` usecase works via the `AddSoundModel(string)` overload.

**Werte**

- `AceStepCppTurbo`
  <br>ACE-Step 1.5 Turbo, native C++/GGML build (acestep-cpp backend). Stereo 48kHz; Q8_0 quant for a good speed/quality balance.
- `AceStepCppTurbo4b`
  <br>ACE-Step 1.5 Turbo (C++/GGML) with the larger 4B LM — higher-quality metadata/code generation.
- `AceStepTurbo`
  <br>ACE-Step 1.5 Turbo — music generation from text/lyrics with BPM/key/time-signature control (ace-step backend). Good default.
- `value__`

### `KnownSpeechToTextModel`

`enum`

Well-known speech-to-text (whisper) models from the LocalAI gallery, installed via `AddSpeechToTextModel`. Served OpenAI-compatibly on `/v1/audio/transcriptions`. Names follow the standard whisper.cpp gallery size variants; any other gallery model works via the `AddSpeechToTextModel(string)` overload.

**Werte**

- `Moonshine`
  <br>Moonshine (crispASR) — fast, low-latency English speech recognition.
- `ParakeetTdt1_1b`
  <br>NVIDIA Parakeet TDT 1.1B (crispASR) — fast, accurate English transcription.
- `Qwen3Asr0_6b`
  <br>Qwen3-ASR 0.6B — small modern multilingual ASR model.
- `Qwen3Asr1_7b`
  <br>Qwen3-ASR 1.7B — larger Qwen3 ASR, higher accuracy.
- `Voxtral`
  <br>Voxtral (crispASR) — Mistral's speech-understanding/transcription model.
- `WhisperBase`
  <br>Whisper base (74M) — good default, fast, low memory.
- `WhisperBaseQ5`
  <br>Whisper base (Q5_1) — quantized base; very light default.
- `WhisperLarge`
  <br>Whisper large (v2, 1.55B) — original large multilingual model.
- `WhisperLargeQ5`
  <br>Whisper large (Q5_0) — quantized large; best-accuracy multilingual at a smaller size.
- `WhisperLargeTurbo`
  <br>Whisper large turbo — distilled large-v3, much faster with near-large accuracy.
- `WhisperLargeTurboQ8`
  <br>Whisper large turbo (Q8_0) — quantized turbo; smaller/faster, minimal quality loss.
- `WhisperMedium`
  <br>Whisper medium (769M) — high accuracy.
- `WhisperMediumQ5`
  <br>Whisper medium (Q5_0) — quantized medium; good accuracy/size balance.
- `WhisperSmall`
  <br>Whisper small (244M) — better accuracy, still light.
- `WhisperSmallQ5`
  <br>Whisper small (Q5_1) — quantized small; light memory footprint.
- `WhisperTiny`
  <br>Whisper tiny (39M) — fastest, tiniest whisper; lowest accuracy.
- `value__`

### `KnownTextModel`

`enum`

Well-known chat / LLM text models from the LocalAI gallery, installed via `AddTextModel` and served on `/v1/chat/completions`. Includes vision-capable multimodal (Qwen3-VL, Gemma 3) and an omni model. The gallery holds 1000+ LLMs — this is a curated pick; ANY other works via the `AddTextModel(string)` overload. MoE sizes like "30b-a3b" = 30B total / 3B active.

**Werte**

- `Aya23_35b`
  <br>Aya 23 35B — größeres mehrsprachiges Cohere-Modell.
- `Aya23_8b`
  <br>Aya 23 8B — mehrsprachiges Modell von Cohere.
- `Codestral22b`
  <br>Codestral 22B v0.1 — starkes Code-Modell.
- `DeepHermes3Llama8b`
  <br>DeepHermes 3 Llama 3 8B (Preview) — Reasoning-fokussiert.
- `DeepHermes3Mistral24b`
  <br>DeepHermes 3 Mistral 24B (Preview).
- `DeepSeekCoderV2Lite`
  <br>DeepSeek-Coder V2 Lite Instruct (MoE) — starkes Coding bei geringem Aktiv-Speicher.
- `DeepSeekOcr`
  <br>DeepSeek-OCR — Dokument-/Bild-zu-Text.
- `DeepSeekR1DistillLlama70b`
  <br>DeepSeek-R1 Distill Llama 70B.
- `DeepSeekR1DistillLlama8b`
  <br>DeepSeek-R1 Distill Llama 8B.
- `DeepSeekR1DistillQwen14b`
  <br>DeepSeek-R1 Distill Qwen 14B.
- `DeepSeekR1DistillQwen32b`
  <br>DeepSeek-R1 Distill Qwen 32B.
- `DeepSeekR1DistillQwen7b`
  <br>DeepSeek-R1 Distill Qwen 7B.
- `DeepSeekV3_2`
  <br>DeepSeek V3.2 — Top-Reasoning (sehr groß).
- `DevstralSmall2507`
  <br>Devstral Small 2507 — Coding-/Agent-optimiert.
- `Dolphin30Llama31_8b`
  <br>Dolphin 3.0 Llama 3.1 8B — hilfsbereiter, unzensierter Allrounder.
- `Dolphin30Llama32_3b`
  <br>Dolphin 3.0 Llama 3.2 3B — kompakt, unzensiert.
- `Dolphin30Mistral24b`
  <br>Dolphin 3.0 Mistral 24B (CognitiveComputations).
- `Falcon3_10b`
  <br>Falcon3 10B Instruct.
- `Falcon3_1b`
  <br>Falcon3 1B Instruct.
- `Falcon3_3b`
  <br>Falcon3 3B Instruct.
- `Gemma3_12b`
  <br>Gemma 3 12B (Vision).
- `Gemma3_1b`
  <br>Gemma 3 1B.
- `Gemma3_27b`
  <br>Gemma 3 27B (Vision).
- `Gemma3_4b`
  <br>Gemma 3 4B (Vision).
- `Gemma3n_E2b`
  <br>Gemma 3n E2B — kleinere Gemma-3n-Variante.
- `Gemma3n_E4b`
  <br>Gemma 3n E4B (effiziente MatFormer-Architektur).
- `Glm47Flash`
  <br>GLM-4.7 Flash — schnelles, starkes Chat-Modell.
- `Glm52`
  <br>GLM 5.2 — starkes, aktuelles Chat-/Reasoning-Modell.
- `GptOss120b`
  <br>gpt-oss 120B — großes offenes OpenAI-Modell.
- `GptOss20b`
  <br>gpt-oss 20B — OpenAIs offenes Modell (mittelgroß).
- `Granite32_8b`
  <br>IBM Granite 3.2 8B Instruct.
- `Granite33_2b`
  <br>IBM Granite 3.3 2B Instruct.
- `Granite33_8b`
  <br>IBM Granite 3.3 8B Instruct.
- `Granite3_1b`
  <br>IBM Granite 3.0 1B (MoE).
- `Granite40HSmall`
  <br>IBM Granite 4.0 H Small (Hybrid).
- `Granite40HTiny`
  <br>IBM Granite 4.0 H Tiny (Hybrid).
- `Granite40Micro`
  <br>IBM Granite 4.0 Micro — sehr kompakt.
- `Hermes3Llama8bLorablated`
  <br>Hermes 3 Llama 3.1 8B (lorablated, unzensiert).
- `Hermes4_14b`
  <br>NousResearch Hermes 4 14B.
- `Hermes4_70b`
  <br>NousResearch Hermes 4 70B.
- `KimiK26`
  <br>Kimi K2.6.
- `KimiK27Code`
  <br>Kimi K2.7 Code — sehr starkes Coding-Modell.
- `Llama31_70b`
  <br>Llama 3.1 70B Instruct.
- `Llama31_8b`
  <br>Llama 3.1 8B Instruct.
- `Llama32_1b`
  <br>Llama 3.2 1B Instruct (Q4).
- `Llama32_3b`
  <br>Llama 3.2 3B Instruct (Q4).
- `Llama33_70b`
  <br>Llama 3.3 70B Instruct.
- `MagistralSmall2509`
  <br>Magistral Small 2509 — Mistrals Reasoning-Modell.
- `MiniCpmV45`
  <br>MiniCPM-V 4.5 — kompaktes, leistungsstarkes Vision-LLM.
- `Mistral7bV03`
  <br>Mistral 7B Instruct v0.3 — kompakter, robuster Klassiker.
- `MistralNemo2407`
  <br>Mistral NeMo Instruct 2407 (12B) — 128k-Kontext, mehrsprachig.
- `MistralSmall24b2501`
  <br>Mistral Small 24B Instruct 2501.
- `MistralSmall31_24b`
  <br>Mistral Small 3.1 24B Instruct 2503.
- `MistralSmall32_24b`
  <br>Mistral Small 3.2 24B Instruct 2506.
- `Pixtral12b`
  <br>Pixtral 12B — Mistrals multimodales (Vision) Modell.
- `Qwen3Coder480b`
  <br>Qwen3-Coder 480B-A35B — Spitzen-Coding (sehr groß).
- `Qwen3Omni30b`
  <br>Qwen3-Omni 30B-A3B Instruct — multimodal (Text/Audio/Bild).
- `Qwen3Vl2b`
  <br>Qwen3-VL 2B Instruct — kompaktes Vision-LLM.
- `Qwen3Vl30b`
  <br>Qwen3-VL 30B-A3B Instruct — starkes Vision-LLM (MoE).
- `Qwen3Vl32b`
  <br>Qwen3-VL 32B Instruct — großes Vision-LLM.
- `Qwen3Vl4b`
  <br>Qwen3-VL 4B Instruct — Vision-LLM.
- `Qwen3Vl8b`
  <br>Qwen3-VL 8B Instruct — Vision-LLM.
- `Qwen3_0_6b`
  <br>Qwen3 0.6B — winzig, für schnelle/lokale Tests.
- `Qwen3_14b`
  <br>Qwen3 14B.
- `Qwen3_235bA22b`
  <br>Qwen3 235B-A22B Instruct 2507 (MoE) — sehr großes Flaggschiff.
- `Qwen3_30bA3b`
  <br>Qwen3 30B-A3B (MoE, 3B aktiv) — stark bei geringem Speed-Kosten.
- `Qwen3_32b`
  <br>Qwen3 32B.
- `Qwen3_4b`
  <br>Qwen3 4B.
- `Qwen3_8b`
  <br>Qwen3 8B — guter Allrounder-Default für eine einzelne GPU.
- `SmolLm2_1_7b`
  <br>SmolLM2 1.7B — sehr leichtgewichtig.
- `value__`

### `KnownTextToSpeechModel`

`enum`

Well-known text-to-speech / audio models from the LocalAI gallery, installed via `AddTextToSpeechModel`. The `DescriptionAttribute` holds the exact installable gallery name. Served on `/v1/audio/speech` (OpenAI-compatible) and `/tts`. Any other gallery model works via the `AddTextToSpeechModel(string)` overload.

**Werte**

- `Chatterbox`
  <br>Chatterbox — expressive TTS with zero-shot voice cloning (Resemble AI).
- `Dia`
  <br>Dia — 1.6B dialogue TTS (Nari Labs); generates lifelike multi-speaker conversation.
- `FishSpeechS2Pro`
  <br>Fish-Speech S2 Pro — high-quality multilingual TTS with voice cloning.
- `KittenTts`
  <br>Kitten-TTS — very small / fast TTS.
- `Kokoro`
  <br>Kokoro — multilingual (incl. German), fast, high quality. Good default.
- `KokoroGerman`
  <br>Kokoro (Rust "kokoros"), German voices.
- `KokoroMultiLang`
  <br>Kokoro multi-language v1.0 (sherpa-onnx) — offline multilingual Kokoro.
- `Kokoros`
  <br>Kokoros (Rust) — the default multilingual Kokoro voice set.
- `KokorosChinese`
  <br>Kokoros (Rust) — Chinese (Mandarin) voices.
- `KokorosJapanese`
  <br>Kokoros (Rust) — Japanese voices.
- `Lfm2AudioTts`
  <br>LiquidAI LFM2.5-Audio 1.5B — TTS variant of the audio foundation model.
- `NeuTtsAir`
  <br>NeuTTS Air — lightweight, natural on-device TTS with instant voice cloning.
- `OmniVoice`
  <br>OmniVoice (cpp) — fast TTS with voice cloning from a reference clip.
- `OmniVoiceHq`
  <br>OmniVoice (cpp, high quality) — higher-fidelity OmniVoice variant.
- `OuteTts`
  <br>OuteTTS — multilingual TTS.
- `ParlerTtsMini`
  <br>Parler-TTS Mini v0.1 — controllable TTS (steer voice/style via a text description).
- `PiperEnglishGbAlan`
  <br>Piper VITS — English (GB) male voice "Alan".
- `PiperEnglishGbVctk`
  <br>Piper VITS — English (GB) multi-speaker "VCTK".
- `PiperEnglishUsAmy`
  <br>Piper VITS — English (US) female voice "Amy".
- `PiperFrenchSiwis`
  <br>Piper VITS — French voice "Siwis".
- `PiperGerman`
  <br>Piper — German voice (Thorsten), small &amp; robust offline.
- `PiperItalianPaola`
  <br>Piper VITS — Italian voice "Paola".
- `PiperSpanishDavefx`
  <br>Piper VITS — Spanish voice "DaveFX".
- `PocketTts`
  <br>Pocket-TTS — small, fast general-purpose TTS.
- `Qwen3Tts0_6b`
  <br>Qwen3-TTS 0.6B — custom-voice TTS (clone a voice from a reference sample).
- `Qwen3Tts1_7b`
  <br>Qwen3-TTS 1.7B — larger custom-voice TTS, higher quality.
- `Supertonic3`
  <br>SuperTonic 3 — fast, expressive neural TTS.
- `VibeVoice`
  <br>VibeVoice — expressive multi-speaker TTS.
- `VibeVoiceCpp`
  <br>VibeVoice (C++/GGML build) — expressive multi-speaker TTS, native backend.
- `VitsLjs`
  <br>VITS — English "LJSpeech" (classic LJS single-speaker voice).
- `VoiceChineseHuayan`
  <br>Piper voice — Chinese (Mandarin) "Huayan", medium quality.
- `VoiceEnglishAmy`
  <br>Piper voice — English (US) female "Amy", medium quality.
- `VoiceEnglishRyan`
  <br>Piper voice — English (US) male "Ryan", high quality.
- `VoiceFrenchTom`
  <br>Piper voice — French male "Tom", medium quality.
- `VoiceGermanThorsten`
  <br>Piper voice — German (Thorsten), medium quality.
- `VoiceGermanThorstenEmotional`
  <br>Piper voice — German (Thorsten), emotional variant.
- `VoiceItalianPaola`
  <br>Piper voice — Italian "Paola", medium quality.
- `VoiceRussianDenis`
  <br>Piper voice — Russian male "Denis", medium quality.
- `VoiceSpanishDavefx`
  <br>Piper voice — Spanish "DaveFX", medium quality.
- `VoxCpm15`
  <br>VoxCPM 1.5 — tokenizer-free, context-aware TTS with voice cloning.
- `value__`

### `KnownVideoModel`

`enum`

Well-known text/image-to-video models from the LocalAI gallery, installed via `AddVideoModel`. Served on `POST /video`. Video weights are large (many GB) and generation is slow / GPU-bound. The gallery is the source of truth — browse `` and pass any exact id via the `AddVideoModel(string)` overload.

**Werte**

- `Ltx2`
  <br>Lightricks LTX-2 — DiT audio-video foundation model; generates synchronized video and audio (image-to-video, diffusers). GPU.
- `Ltx23`
  <br>Lightricks LTX-2.3 — improved LTX-2 with better motion/quality; synchronized audio-video (diffusers). GPU.
- `Wan21FirstLastFrameToVideo720pGgml`
  <br>Wan 2.1 first-last-frame-to-video 14B 720p, GGUF Q4_K_M — interpolates between a start and end image (great for seamless loops).
- `Wan21ImageToVideo480pGgml`
  <br>Wan 2.1 image-to-video 14B 480p, GGUF Q4 — animates a reference image into a short clip.
- `Wan21ImageToVideo720pGgml`
  <br>Wan 2.1 image-to-video 14B 720p, GGUF Q4_K_M — native 720p single-image animation.
- `Wan21TextToVideoGgml`
  <br>Wan 2.1 text-to-video 1.3B, GGUF-quantized (stable-diffusion.cpp). Cheapest Wan; ~10 GB RAM, CPU-offloadable.
- `Wan22ImageToVideo`
  <br>Wan 2.2 image-to-video (14B) via the vllm-omni backend — animates a still image.
- `Wan22TextToVideo`
  <br>Wan 2.2 text-to-video (14B) via the vllm-omni backend. Needs a strong GPU.
- `value__`

### `LocalAiBuilderExtensions`

`static class`

Aspire hosting extension for a self-hosted, OpenAI-compatible multimodal AI service (LocalAI): image generation, text-to-speech, speech-to-text, video generation, chat and embeddings — the self-hosted counterpart of `AddOllama` for everything beyond text.

### `LocalAiGpu`

`enum`

GPU vendor for the LocalAI container.

**Werte**

- `Amd`
  <br>AMD GPU via ROCm devices (`/dev/kfd`, `/dev/dri`).
- `None`
  <br>CPU only (works everywhere, slow).
- `Nvidia`
  <br>NVIDIA GPU (`--gpus all`; needs NVIDIA Container Toolkit / Docker Desktop GPU support).
- `value__`

### `LocalAiOpenWebUIResource`

`class`

An Open WebUI container wired to a `LocalAiResource`.

**Konstruktoren**

- `LocalAiOpenWebUIResource(string name)`
  <br>An Open WebUI container wired to a `LocalAiResource`.

### `LocalAiOptions`

`class`

Options for `AddLocalAI`.

**Konstruktoren**

- `LocalAiOptions()`

**Eigenschaften**

- `AioProfile : string { get; set; }`
  <br>AIO profile (`cpu`, `gpu-8g`, `apple`). LocalAI's AIO images detect the GPU via `lspci`, which fails inside Docker Desktop/WSL2 even when `--gpus all` works — so when `Gpu` is `Nvidia` and an AIO tag is used, this defaults to `gpu-8g` to force GPU mode. Set explicitly to override.
- `ApiKey : string { get; set; }`
  <br>Optional API key the backend requires (sets LocalAI `API_KEY`).
- `Environment : IDictionary<string, string> { get; }`
  <br>Extra environment variables for the container.
- `Gpu : LocalAiGpu { get; set; }`
  <br>GPU vendor. Default: `Nvidia`.
- `HostPort : int? { get; set; }`
  <br>Fixed host port for the endpoint (random if null).
- `Image : string { get; set; }`
  <br>Container image (without tag). Default: `localai/localai`.
- `Tag : string { get; set; }`
  <br>Image tag. Default: the standard NVIDIA CUDA 12 build (currently LocalAI 4.x) — this is what brings video generation and the ace-step sound backend. It's a slim image that only loads what you add via `AddModel`/`AddTextModel`/…; backends download on demand. NOTE: the all-in-one (`-aio-`) tags are frozen at v3.12.1 upstream and lack video/sound — only pick one (e.g. `latest-aio-gpu-nvidia-cuda-12`) if you specifically want the bundled default model set and can do without the newer backends.

### `LocalAiResource`

`class`

A self-hosted, OpenAI-compatible multimodal AI service (backend: LocalAI). One container serves image generation (`/v1/images/generations`), text-to-speech (`/v1/audio/speech`), speech-to-text (`/v1/audio/transcriptions`), video generation (`/video`), sound / music generation (`/v1/sound-generation`), chat and embeddings — plus LocalAI's built-in WebUI, all on the same endpoint.

**Konstruktoren**

- `LocalAiResource(string name)`
  <br>A self-hosted, OpenAI-compatible multimodal AI service (backend: LocalAI). One container serves image generation (`/v1/images/generations`), text-to-speech (`/v1/audio/speech`), speech-to-text (`/v1/audio/transcriptions`), video generation (`/video`), sound / music generation (`/v1/sound-generation`), chat and embeddings — plus LocalAI's built-in WebUI, all on the same endpoint.

**Methoden**

- `DefaultModelFor(ModelModality modality) : string`
  <br>The default model id for a modality (first added of that kind), or `null` if none was added.

**Eigenschaften**

- `DefaultModel : string { get; }`
  <br>The default IMAGE model consumers use (first image model added wins; falls back to the AIO-bundled `stablediffusion` when no image model was added explicitly).
- `Models : IList<RegisteredModel> { get; }`
  <br>Models registered via `AddModel`/`AddTextToSpeechModel`/`AddSpeechToTextModel`/ `AddVideoModel`/`AddSoundModel` (installed from the LocalAI gallery on startup), each tagged with its modality.

**Felder**

- `DefaultTargetPort : int`
  <br>Default internal container port LocalAI listens on.
- `HttpEndpointName : string`
  <br>Name of the primary HTTP endpoint.

### `ModelModality`

`enum`

The kind of generation a model performs — determines which default-model env var it drives.

**Werte**

- `Embedding`
  <br>Text embeddings for semantic search / RAG (`POST /v1/embeddings`).
- `Image`
  <br>Text-to-image (`/v1/images/generations`).
- `Sound`
  <br>Text-to-sound / music generation (`POST /v1/sound-generation`).
- `SpeechToText`
  <br>Speech-to-text / transcription (`/v1/audio/transcriptions`).
- `Text`
  <br>Chat / LLM text generation — incl. vision-capable multimodal (`POST /v1/chat/completions`).
- `TextToSpeech`
  <br>Text-to-speech (`/v1/audio/speech`).
- `Video`
  <br>Text/image-to-video (`POST /video`).
- `value__`

### `OpenWebUiOptions`

`class`

Options for the `WithOpenWebUI(...)` overloads. Defaults reproduce the built-in wiring (LocalAI registered as an OpenAI-compatible connection + image generation, env authoritative). Override to change auth, persistence, the image model/tag, or add arbitrary env.

**Konstruktoren**

- `OpenWebUiOptions()`

**Eigenschaften**

- `ApiKey : string { get; set; }`
  <br>API key sent to LocalAI for the OpenAI/images calls. Default `sk-local`.
- `Auth : bool? { get; set; }`
  <br>`WEBUI_AUTH`. `null` = leave untouched (a reused WebUI keeps its own setting); a newly-created WebUI defaults to `false` (no login, dev-friendly).
- `EnableImageGeneration : bool { get; set; }`
  <br>Wire image generation against LocalAI's OpenAI-compatible images endpoint. Default `true`.
- `Environment : IDictionary<string, string> { get; }`
  <br>Extra environment variables for the Open WebUI container (applied last — overrides the above).
- `Image : string { get; set; }`
  <br>Container image — only used when a NEW Open WebUI is created. Default `ghcr.io/open-webui/open-webui`.
- `ImageGenerationModel : string { get; set; }`
  <br>Image-generation model Open WebUI uses. Default: the LocalAI default image model.
- `PersistentConfig : bool { get; set; }`
  <br>`ENABLE_PERSISTENT_CONFIG`. Default `false` so this env is authoritative on every start — otherwise Open WebUI freezes these values in its DB on first run and ignores later env changes (that's why a reused WebUI wouldn't pick up the config). Set `true` to let the UI persist changes.
- `RegisterOpenAiModels : bool { get; set; }`
  <br>Register LocalAI as an OpenAI-compatible model connection (chat/LLM/vision list). Default `true`.
- `Tag : string { get; set; }`
  <br>Image tag (new WebUI only). Default `main`.

### `RegisteredModel`

`class`

A model queued for install on LocalAI startup, tagged with its `ModelModality`.

**Konstruktoren**

- `RegisteredModel(string Name, string Reference, ModelModality Modality)`
  <br>A model queued for install on LocalAI startup, tagged with its `ModelModality`.

**Eigenschaften**

- `Modality : ModelModality { get; set; }`
  <br>The generation kind this model performs.
- `Name : string { get; set; }`
  <br>The model id consumers pass to the API (and shown in /v1/models).
- `Reference : string { get; set; }`
  <br>What goes into LocalAI's MODELS list: a gallery name, URI or container path to a generated config yaml.

### `SdNextResource`

`class`

A standalone SD.Next image-generation studio attached to the stack.

**Konstruktoren**

- `SdNextResource(string name)`
  <br>A standalone SD.Next image-generation studio attached to the stack.

## Projects

### `Nextended_Aspire_Hosting_LocalAI`

`class`

Metadata for the Aspire AppHost project.

**Eigenschaften**

- `ProjectPath : string { get; }`
  <br>The path to the Aspire Host project.

↩ [Zurück zur Paketseite](/de/projects/aspire-localai)
