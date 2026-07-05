using System.ComponentModel;
using System.Reflection;

namespace Nextended.Aspire.Hosting.LocalAI;

/// <summary>
/// Well-known text-to-speech / audio models from the LocalAI gallery, installed via
/// <c>AddTextToSpeechModel</c>. The <see cref="DescriptionAttribute"/> holds the exact
/// installable gallery name. Served on <c>/v1/audio/speech</c> (OpenAI-compatible) and <c>/tts</c>.
/// Any other gallery model works via the <c>AddTextToSpeechModel(string)</c> overload.
/// </summary>
public enum KnownTextToSpeechModel
{
    /// <summary>Kokoro — multilingual (incl. German), fast, high quality. Good default.</summary>
    [Description("kokoro")]
    Kokoro,

    /// <summary>Kokoro (Rust "kokoros"), German voices.</summary>
    [Description("kokoros-de")]
    KokoroGerman,

    /// <summary>VibeVoice — expressive multi-speaker TTS.</summary>
    [Description("vibevoice")]
    VibeVoice,

    /// <summary>OmniVoice (cpp) — fast TTS with voice cloning from a reference clip.</summary>
    [Description("omnivoice-cpp")]
    OmniVoice,

    /// <summary>OmniVoice (cpp, high quality) — higher-fidelity OmniVoice variant.</summary>
    [Description("omnivoice-cpp-hq")]
    OmniVoiceHq,

    /// <summary>Pocket-TTS — small, fast general-purpose TTS.</summary>
    [Description("pocket-tts")]
    PocketTts,

    /// <summary>OuteTTS — multilingual TTS.</summary>
    [Description("outetts")]
    OuteTts,

    /// <summary>Kitten-TTS — very small / fast TTS.</summary>
    [Description("kitten-tts")]
    KittenTts,

    /// <summary>Piper — German voice (Thorsten), small &amp; robust offline.</summary>
    [Description("vits-piper-de_DE-thorsten-sherpa")]
    PiperGerman,

    // — Additional verified TTS models & voices from the gallery —

    /// <summary>Chatterbox — expressive TTS with zero-shot voice cloning (Resemble AI).</summary>
    [Description("chatterbox")]
    Chatterbox,

    /// <summary>Dia — 1.6B dialogue TTS (Nari Labs); generates lifelike multi-speaker conversation.</summary>
    [Description("dia")]
    Dia,

    /// <summary>Fish-Speech S2 Pro — high-quality multilingual TTS with voice cloning.</summary>
    [Description("fish-speech-s2-pro")]
    FishSpeechS2Pro,

    /// <summary>NeuTTS Air — lightweight, natural on-device TTS with instant voice cloning.</summary>
    [Description("neutts-air")]
    NeuTtsAir,

    /// <summary>Parler-TTS Mini v0.1 — controllable TTS (steer voice/style via a text description).</summary>
    [Description("parler-tts-mini-v0.1")]
    ParlerTtsMini,

    /// <summary>SuperTonic 3 — fast, expressive neural TTS.</summary>
    [Description("supertonic-3")]
    Supertonic3,

    /// <summary>VoxCPM 1.5 — tokenizer-free, context-aware TTS with voice cloning.</summary>
    [Description("voxcpm-1.5")]
    VoxCpm15,

    /// <summary>VibeVoice (C++/GGML build) — expressive multi-speaker TTS, native backend.</summary>
    [Description("vibevoice-cpp")]
    VibeVoiceCpp,

    /// <summary>LiquidAI LFM2.5-Audio 1.5B — TTS variant of the audio foundation model.</summary>
    [Description("lfm2.5-audio-1.5b-tts")]
    Lfm2AudioTts,

    /// <summary>Qwen3-TTS 0.6B — custom-voice TTS (clone a voice from a reference sample).</summary>
    [Description("qwen3-tts-0.6b-custom-voice")]
    Qwen3Tts0_6b,

    /// <summary>Qwen3-TTS 1.7B — larger custom-voice TTS, higher quality.</summary>
    [Description("qwen3-tts-1.7b-custom-voice")]
    Qwen3Tts1_7b,

    /// <summary>Kokoros (Rust) — the default multilingual Kokoro voice set.</summary>
    [Description("kokoros")]
    Kokoros,

    /// <summary>Kokoros (Rust) — Chinese (Mandarin) voices.</summary>
    [Description("kokoros-cmn")]
    KokorosChinese,

    /// <summary>Kokoros (Rust) — Japanese voices.</summary>
    [Description("kokoros-ja")]
    KokorosJapanese,

    /// <summary>Kokoro multi-language v1.0 (sherpa-onnx) — offline multilingual Kokoro.</summary>
    [Description("kokoro-multi-lang-v1.0-sherpa")]
    KokoroMultiLang,

    // Piper/VITS single-speaker voices (sherpa-onnx, small & fully offline):

    /// <summary>Piper VITS — English (US) female voice "Amy".</summary>
    [Description("vits-piper-en_US-amy-sherpa")]
    PiperEnglishUsAmy,

    /// <summary>Piper VITS — English (GB) male voice "Alan".</summary>
    [Description("vits-piper-en_GB-alan-medium-sherpa")]
    PiperEnglishGbAlan,

    /// <summary>Piper VITS — English (GB) multi-speaker "VCTK".</summary>
    [Description("vits-piper-en_GB-vctk-medium-sherpa")]
    PiperEnglishGbVctk,

    /// <summary>Piper VITS — French voice "Siwis".</summary>
    [Description("vits-piper-fr_FR-siwis-sherpa")]
    PiperFrenchSiwis,

    /// <summary>Piper VITS — Spanish voice "DaveFX".</summary>
    [Description("vits-piper-es_ES-davefx-sherpa")]
    PiperSpanishDavefx,

    /// <summary>Piper VITS — Italian voice "Paola".</summary>
    [Description("vits-piper-it_IT-paola-sherpa")]
    PiperItalianPaola,

    /// <summary>VITS — English "LJSpeech" (classic LJS single-speaker voice).</summary>
    [Description("vits-ljs-sherpa")]
    VitsLjs,

    // Piper voices via the all-in-one TTS backend ("voice-*" gallery entries):

    /// <summary>Piper voice — German (Thorsten), medium quality.</summary>
    [Description("voice-de_DE-thorsten-medium")]
    VoiceGermanThorsten,

    /// <summary>Piper voice — German (Thorsten), emotional variant.</summary>
    [Description("voice-de_DE-thorsten_emotional-medium")]
    VoiceGermanThorstenEmotional,

    /// <summary>Piper voice — English (US) female "Amy", medium quality.</summary>
    [Description("voice-en_US-amy-medium")]
    VoiceEnglishAmy,

    /// <summary>Piper voice — English (US) male "Ryan", high quality.</summary>
    [Description("voice-en_US-ryan-high")]
    VoiceEnglishRyan,

    /// <summary>Piper voice — French male "Tom", medium quality.</summary>
    [Description("voice-fr_FR-tom-medium")]
    VoiceFrenchTom,

    /// <summary>Piper voice — Spanish "DaveFX", medium quality.</summary>
    [Description("voice-es_ES-davefx-medium")]
    VoiceSpanishDavefx,

    /// <summary>Piper voice — Italian "Paola", medium quality.</summary>
    [Description("voice-it-paola-medium")]
    VoiceItalianPaola,

    /// <summary>Piper voice — Russian male "Denis", medium quality.</summary>
    [Description("voice-ru_RU-denis-medium")]
    VoiceRussianDenis,

    /// <summary>Piper voice — Chinese (Mandarin) "Huayan", medium quality.</summary>
    [Description("voice-zh_CN-huayan-medium")]
    VoiceChineseHuayan,
}

/// <summary>
/// Well-known speech-to-text (whisper) models from the LocalAI gallery, installed via
/// <c>AddSpeechToTextModel</c>. Served OpenAI-compatibly on <c>/v1/audio/transcriptions</c>.
/// Names follow the standard whisper.cpp gallery size variants; any other gallery model works
/// via the <c>AddSpeechToTextModel(string)</c> overload.
/// </summary>
public enum KnownSpeechToTextModel
{
    /// <summary>Whisper base (74M) — good default, fast, low memory.</summary>
    [Description("whisper-base")]
    WhisperBase,

    /// <summary>Whisper small (244M) — better accuracy, still light.</summary>
    [Description("whisper-small")]
    WhisperSmall,

    /// <summary>Whisper medium (769M) — high accuracy.</summary>
    [Description("whisper-medium")]
    WhisperMedium,

    /// <summary>Whisper large (Q5_0) — quantized large; best-accuracy multilingual at a smaller size.</summary>
    [Description("whisper-large-q5_0")]
    WhisperLargeQ5,

    // — Additional verified whisper variants & modern ASR models —

    /// <summary>Whisper tiny (39M) — fastest, tiniest whisper; lowest accuracy.</summary>
    [Description("whisper-tiny")]
    WhisperTiny,

    /// <summary>Whisper large (v2, 1.55B) — original large multilingual model.</summary>
    [Description("whisper-large")]
    WhisperLarge,

    /// <summary>Whisper large turbo — distilled large-v3, much faster with near-large accuracy.</summary>
    [Description("whisper-large-turbo")]
    WhisperLargeTurbo,

    /// <summary>Whisper large turbo (Q8_0) — quantized turbo; smaller/faster, minimal quality loss.</summary>
    [Description("whisper-large-turbo-q8_0")]
    WhisperLargeTurboQ8,

    /// <summary>Whisper small (Q5_1) — quantized small; light memory footprint.</summary>
    [Description("whisper-small-q5_1")]
    WhisperSmallQ5,

    /// <summary>Whisper base (Q5_1) — quantized base; very light default.</summary>
    [Description("whisper-base-q5_1")]
    WhisperBaseQ5,

    /// <summary>Whisper medium (Q5_0) — quantized medium; good accuracy/size balance.</summary>
    [Description("whisper-medium-q5_0")]
    WhisperMediumQ5,

    /// <summary>Qwen3-ASR 0.6B — small modern multilingual ASR model.</summary>
    [Description("qwen3-asr-0.6b")]
    Qwen3Asr0_6b,

    /// <summary>Qwen3-ASR 1.7B — larger Qwen3 ASR, higher accuracy.</summary>
    [Description("qwen3-asr-1.7b")]
    Qwen3Asr1_7b,

    /// <summary>NVIDIA Parakeet TDT 1.1B (crispASR) — fast, accurate English transcription.</summary>
    [Description("parakeet-tdt-1.1b-crispasr")]
    ParakeetTdt1_1b,

    /// <summary>Moonshine (crispASR) — fast, low-latency English speech recognition.</summary>
    [Description("moonshine-crispasr")]
    Moonshine,

    /// <summary>Voxtral (crispASR) — Mistral's speech-understanding/transcription model.</summary>
    [Description("voxtral-crispasr")]
    Voxtral,
}

/// <summary>
/// Well-known text/image-to-video models from the LocalAI gallery, installed via
/// <c>AddVideoModel</c>. Served on <c>POST /video</c>. Video weights are large (many GB) and
/// generation is slow / GPU-bound. The gallery is the source of truth — browse
/// <see href="https://localai.io/gallery.html"/> and pass any exact id via the
/// <c>AddVideoModel(string)</c> overload.
/// </summary>
public enum KnownVideoModel
{
    /// <summary>Wan 2.2 <b>text-to-video</b> (14B) via the vllm-omni backend. Needs a strong GPU.</summary>
    [Description("vllm-omni-wan2.2-t2v")]
    Wan22TextToVideo,

    /// <summary>Wan 2.2 <b>image-to-video</b> (14B) via the vllm-omni backend — animates a still image.</summary>
    [Description("vllm-omni-wan2.2-i2v")]
    Wan22ImageToVideo,

    /// <summary>Wan 2.1 text-to-video 1.3B, GGUF-quantized (stable-diffusion.cpp). Cheapest Wan; ~10 GB RAM, CPU-offloadable.</summary>
    [Description("wan-2.1-t2v-1.3b-ggml")]
    Wan21TextToVideoGgml,

    /// <summary>Wan 2.1 image-to-video 14B 480p, GGUF Q4 — animates a reference image into a short clip.</summary>
    [Description("wan-2.1-i2v-14b-480p-ggml")]
    Wan21ImageToVideo480pGgml,

    /// <summary>Wan 2.1 image-to-video 14B 720p, GGUF Q4_K_M — native 720p single-image animation.</summary>
    [Description("wan-2.1-i2v-14b-720p-ggml")]
    Wan21ImageToVideo720pGgml,

    /// <summary>Wan 2.1 first-last-frame-to-video 14B 720p, GGUF Q4_K_M — interpolates between a start and end image (great for seamless loops).</summary>
    [Description("wan-2.1-flf2v-14b-720p-ggml")]
    Wan21FirstLastFrameToVideo720pGgml,

    /// <summary>Lightricks LTX-2 — DiT audio-video foundation model; generates synchronized video <i>and</i> audio (image-to-video, diffusers). GPU.</summary>
    [Description("ltx-2")]
    Ltx2,

    /// <summary>Lightricks LTX-2.3 — improved LTX-2 with better motion/quality; synchronized audio-video (diffusers). GPU.</summary>
    [Description("ltx-2.3")]
    Ltx23,
}

/// <summary>
/// Well-known text-to-sound / MUSIC generation models from the LocalAI gallery, installed via
/// <c>AddSoundModel</c>. Served on the ElevenLabs-compatible <c>POST /v1/sound-generation</c>
/// (fields <c>model_id</c> + <c>text</c>, plus optional music metadata like <c>lyrics</c>,
/// <c>bpm</c>, <c>duration_seconds</c>). Returns binary audio (wav/flac/mp3). Weights are multi-GB
/// and generation is GPU-bound — combine with <c>WithDataVolume</c>. Any other gallery model with a
/// <c>sound_generation</c> usecase works via the <c>AddSoundModel(string)</c> overload.
/// </summary>
public enum KnownSoundModel
{
    /// <summary>ACE-Step 1.5 Turbo — music generation from text/lyrics with BPM/key/time-signature control (ace-step backend). Good default.</summary>
    [Description("ace-step-turbo")]
    AceStepTurbo,

    /// <summary>ACE-Step 1.5 Turbo, native C++/GGML build (acestep-cpp backend). Stereo 48kHz; Q8_0 quant for a good speed/quality balance.</summary>
    [Description("acestep-cpp-turbo")]
    AceStepCppTurbo,

    /// <summary>ACE-Step 1.5 Turbo (C++/GGML) with the larger 4B LM — higher-quality metadata/code generation.</summary>
    [Description("acestep-cpp-turbo-4b")]
    AceStepCppTurbo4b,
}

/// <summary>
/// Well-known chat / LLM text models from the LocalAI gallery, installed via <c>AddTextModel</c>
/// and served on <c>/v1/chat/completions</c>. Includes vision-capable multimodal (Qwen3-VL,
/// Gemma 3) and an omni model. The gallery holds 1000+ LLMs — this is a curated pick; ANY other
/// works via the <c>AddTextModel(string)</c> overload. MoE sizes like "30b-a3b" = 30B total / 3B active.
/// </summary>
public enum KnownTextModel
{
    // — Qwen3 (starke, aktuelle Allrounder) —
    /// <summary>Qwen3 0.6B — winzig, für schnelle/lokale Tests.</summary>
    [Description("qwen3-0.6b")] Qwen3_0_6b,
    /// <summary>Qwen3 4B.</summary>
    [Description("qwen3-4b")] Qwen3_4b,
    /// <summary>Qwen3 8B — guter Allrounder-Default für eine einzelne GPU.</summary>
    [Description("qwen3-8b")] Qwen3_8b,
    /// <summary>Qwen3 14B.</summary>
    [Description("qwen3-14b")] Qwen3_14b,
    /// <summary>Qwen3 32B.</summary>
    [Description("qwen3-32b")] Qwen3_32b,
    /// <summary>Qwen3 30B-A3B (MoE, 3B aktiv) — stark bei geringem Speed-Kosten.</summary>
    [Description("qwen3-30b-a3b")] Qwen3_30bA3b,
    /// <summary>Qwen3-Coder 480B-A35B — Spitzen-Coding (sehr groß).</summary>
    [Description("qwen3-coder-480b-a35b-instruct")] Qwen3Coder480b,

    // — Meta Llama 3.x —
    /// <summary>Llama 3.1 8B Instruct.</summary>
    [Description("meta-llama-3.1-8b-instruct")] Llama31_8b,
    /// <summary>Llama 3.1 70B Instruct.</summary>
    [Description("meta-llama-3.1-70b-instruct")] Llama31_70b,
    /// <summary>Llama 3.3 70B Instruct.</summary>
    [Description("llama-3.3-70b-instruct")] Llama33_70b,
    /// <summary>Llama 3.2 3B Instruct (Q4).</summary>
    [Description("llama-3.2-3b-instruct:q4_k_m")] Llama32_3b,
    /// <summary>Llama 3.2 1B Instruct (Q4).</summary>
    [Description("llama-3.2-1b-instruct:q4_k_m")] Llama32_1b,

    // — Google Gemma 3 (multimodal-fähig) —
    /// <summary>Gemma 3 1B.</summary>
    [Description("gemma-3-1b-it")] Gemma3_1b,
    /// <summary>Gemma 3 4B (Vision).</summary>
    [Description("gemma-3-4b-it")] Gemma3_4b,
    /// <summary>Gemma 3 12B (Vision).</summary>
    [Description("gemma-3-12b-it")] Gemma3_12b,
    /// <summary>Gemma 3 27B (Vision).</summary>
    [Description("gemma-3-27b-it")] Gemma3_27b,

    // — DeepSeek / GLM / weitere —
    /// <summary>DeepSeek V3.2 — Top-Reasoning (sehr groß).</summary>
    [Description("deepseek-ai.deepseek-v3.2")] DeepSeekV3_2,
    /// <summary>DeepSeek-OCR — Dokument-/Bild-zu-Text.</summary>
    [Description("deepseek-ocr")] DeepSeekOcr,
    /// <summary>GLM-4.7 Flash — schnelles, starkes Chat-Modell.</summary>
    [Description("glm-4.7-flash")] Glm47Flash,
    /// <summary>Kimi K2.7 Code — sehr starkes Coding-Modell.</summary>
    [Description("kimi-k2.7-code")] KimiK27Code,
    /// <summary>Kimi K2.6.</summary>
    [Description("kimi-k2.6")] KimiK26,
    /// <summary>NousResearch Hermes 4 14B.</summary>
    [Description("nousresearch_hermes-4-14b")] Hermes4_14b,
    /// <summary>SmolLM2 1.7B — sehr leichtgewichtig.</summary>
    [Description("smollm2-1.7b-instruct")] SmolLm2_1_7b,
    /// <summary>IBM Granite 3.0 1B (MoE).</summary>
    [Description("granite-3.0-1b-a400m-instruct")] Granite3_1b,

    // — Vision / multimodal (Bildverständnis via /v1/chat/completions) —
    /// <summary>Qwen3-VL 4B Instruct — Vision-LLM.</summary>
    [Description("qwen3-vl-4b-instruct")] Qwen3Vl4b,
    /// <summary>Qwen3-VL 8B Instruct — Vision-LLM.</summary>
    [Description("qwen3-vl-8b-instruct")] Qwen3Vl8b,
    /// <summary>Qwen3-VL 30B-A3B Instruct — starkes Vision-LLM (MoE).</summary>
    [Description("qwen3-vl-30b-a3b-instruct")] Qwen3Vl30b,

    // — Omni (Text + Audio + Vision) —
    /// <summary>Qwen3-Omni 30B-A3B Instruct — multimodal (Text/Audio/Bild).</summary>
    [Description("qwen3-omni-30b-a3b-instruct")] Qwen3Omni30b,

    // ===== Weitere verifizierte Gallery-LLMs (append-only) =====

    // — Mistral / Mixtral / Ministral / Magistral / Devstral / Codestral —
    /// <summary>Mistral 7B Instruct v0.3 — kompakter, robuster Klassiker.</summary>
    [Description("mistral-7b-instruct-v0.3")] Mistral7bV03,
    /// <summary>Mistral NeMo Instruct 2407 (12B) — 128k-Kontext, mehrsprachig.</summary>
    [Description("mistral-nemo-instruct-2407")] MistralNemo2407,
    /// <summary>Mistral Small 24B Instruct 2501.</summary>
    [Description("mistral-small-24b-instruct-2501")] MistralSmall24b2501,
    /// <summary>Mistral Small 3.2 24B Instruct 2506.</summary>
    [Description("mistralai_mistral-small-3.2-24b-instruct-2506")] MistralSmall32_24b,
    /// <summary>Mistral Small 3.1 24B Instruct 2503.</summary>
    [Description("mistralai_mistral-small-3.1-24b-instruct-2503")] MistralSmall31_24b,
    /// <summary>Magistral Small 2509 — Mistrals Reasoning-Modell.</summary>
    [Description("mistralai_magistral-small-2509")] MagistralSmall2509,
    /// <summary>Devstral Small 2507 — Coding-/Agent-optimiert.</summary>
    [Description("mistralai_devstral-small-2507")] DevstralSmall2507,
    /// <summary>Codestral 22B v0.1 — starkes Code-Modell.</summary>
    [Description("codestral-22b-v0.1")] Codestral22b,
    /// <summary>Pixtral 12B — Mistrals multimodales (Vision) Modell.</summary>
    [Description("mistral-community_pixtral-12b")] Pixtral12b,

    // — DeepSeek R1 Distills (starkes Reasoning, kleine Größen) —
    /// <summary>DeepSeek-R1 Distill Qwen 7B.</summary>
    [Description("deepseek-r1-distill-qwen-7b")] DeepSeekR1DistillQwen7b,
    /// <summary>DeepSeek-R1 Distill Qwen 14B.</summary>
    [Description("deepseek-r1-distill-qwen-14b")] DeepSeekR1DistillQwen14b,
    /// <summary>DeepSeek-R1 Distill Qwen 32B.</summary>
    [Description("deepseek-r1-distill-qwen-32b")] DeepSeekR1DistillQwen32b,
    /// <summary>DeepSeek-R1 Distill Llama 8B.</summary>
    [Description("deepseek-r1-distill-llama-8b")] DeepSeekR1DistillLlama8b,
    /// <summary>DeepSeek-R1 Distill Llama 70B.</summary>
    [Description("deepseek-r1-distill-llama-70b")] DeepSeekR1DistillLlama70b,
    /// <summary>DeepSeek-Coder V2 Lite Instruct (MoE) — starkes Coding bei geringem Aktiv-Speicher.</summary>
    [Description("deepseek-coder-v2-lite-instruct")] DeepSeekCoderV2Lite,

    // — OpenAI gpt-oss (offene Gewichte) —
    /// <summary>gpt-oss 20B — OpenAIs offenes Modell (mittelgroß).</summary>
    [Description("gpt-oss-20b")] GptOss20b,
    /// <summary>gpt-oss 120B — großes offenes OpenAI-Modell.</summary>
    [Description("gpt-oss-120b")] GptOss120b,

    // — Qwen3 (weitere Varianten) —
    /// <summary>Qwen3-VL 2B Instruct — kompaktes Vision-LLM.</summary>
    [Description("qwen3-vl-2b-instruct")] Qwen3Vl2b,
    /// <summary>Qwen3 235B-A22B Instruct 2507 (MoE) — sehr großes Flaggschiff.</summary>
    [Description("qwen3-235b-a22b-instruct-2507")] Qwen3_235bA22b,
    /// <summary>Qwen3-VL 32B Instruct — großes Vision-LLM.</summary>
    [Description("qwen3-vl-32b-instruct")] Qwen3Vl32b,

    // — IBM Granite 3.x / 4.0 —
    /// <summary>IBM Granite 3.3 8B Instruct.</summary>
    [Description("ibm-granite_granite-3.3-8b-instruct")] Granite33_8b,
    /// <summary>IBM Granite 3.3 2B Instruct.</summary>
    [Description("ibm-granite_granite-3.3-2b-instruct")] Granite33_2b,
    /// <summary>IBM Granite 3.2 8B Instruct.</summary>
    [Description("ibm-granite_granite-3.2-8b-instruct")] Granite32_8b,
    /// <summary>IBM Granite 4.0 Micro — sehr kompakt.</summary>
    [Description("ibm-granite_granite-4.0-micro")] Granite40Micro,
    /// <summary>IBM Granite 4.0 H Small (Hybrid).</summary>
    [Description("ibm-granite_granite-4.0-h-small")] Granite40HSmall,
    /// <summary>IBM Granite 4.0 H Tiny (Hybrid).</summary>
    [Description("ibm-granite_granite-4.0-h-tiny")] Granite40HTiny,

    // — NousResearch Hermes / DeepHermes —
    /// <summary>NousResearch Hermes 4 70B.</summary>
    [Description("nousresearch_hermes-4-70b")] Hermes4_70b,
    /// <summary>Hermes 3 Llama 3.1 8B (lorablated, unzensiert).</summary>
    [Description("hermes-3-llama-3.1-8b-lorablated")] Hermes3Llama8bLorablated,
    /// <summary>DeepHermes 3 Llama 3 8B (Preview) — Reasoning-fokussiert.</summary>
    [Description("nousresearch_deephermes-3-llama-3-8b-preview")] DeepHermes3Llama8b,
    /// <summary>DeepHermes 3 Mistral 24B (Preview).</summary>
    [Description("nousresearch_deephermes-3-mistral-24b-preview")] DeepHermes3Mistral24b,

    // — Cohere Aya (mehrsprachig) —
    /// <summary>Aya 23 8B — mehrsprachiges Modell von Cohere.</summary>
    [Description("aya-23-8b")] Aya23_8b,
    /// <summary>Aya 23 35B — größeres mehrsprachiges Cohere-Modell.</summary>
    [Description("aya-23-35b")] Aya23_35b,

    // — Dolphin (unzensierte Fine-tunes) —
    /// <summary>Dolphin 3.0 Llama 3.1 8B — hilfsbereiter, unzensierter Allrounder.</summary>
    [Description("dolphin3.0-llama3.1-8b")] Dolphin30Llama31_8b,
    /// <summary>Dolphin 3.0 Llama 3.2 3B — kompakt, unzensiert.</summary>
    [Description("dolphin3.0-llama3.2-3b")] Dolphin30Llama32_3b,
    /// <summary>Dolphin 3.0 Mistral 24B (CognitiveComputations).</summary>
    [Description("cognitivecomputations_dolphin3.0-mistral-24b")] Dolphin30Mistral24b,

    // — Falcon 3 —
    /// <summary>Falcon3 10B Instruct.</summary>
    [Description("falcon3-10b-instruct")] Falcon3_10b,
    /// <summary>Falcon3 3B Instruct.</summary>
    [Description("falcon3-3b-instruct")] Falcon3_3b,
    /// <summary>Falcon3 1B Instruct.</summary>
    [Description("falcon3-1b-instruct")] Falcon3_1b,

    // — Google Gemma 3n (mobil-effizient, multimodal) —
    /// <summary>Gemma 3n E4B (effiziente MatFormer-Architektur).</summary>
    [Description("gemma-3n-e4b-it")] Gemma3n_E4b,
    /// <summary>Gemma 3n E2B — kleinere Gemma-3n-Variante.</summary>
    [Description("gemma-3n-e2b-it")] Gemma3n_E2b,

    // — GLM (weitere) —
    /// <summary>GLM 5.2 — starkes, aktuelles Chat-/Reasoning-Modell.</summary>
    [Description("glm-5.2")] Glm52,

    // — MiniCPM (kompaktes Vision-LLM) —
    /// <summary>MiniCPM-V 4.5 — kompaktes, leistungsstarkes Vision-LLM.</summary>
    [Description("minicpm-v-4_5")] MiniCpmV45,
}

/// <summary>
/// Well-known text-embedding models from the LocalAI gallery, installed via <c>AddEmbeddingModel</c>
/// and served on <c>/v1/embeddings</c> (semantic search / RAG). Any other works via the string overload.
/// </summary>
public enum KnownEmbeddingModel
{
    /// <summary>BERT embeddings — klein, universell, guter Default.</summary>
    [Description("bert-embeddings")] BertEmbeddings,
    /// <summary>Nomic Embed Text v1.5 — starke, offene Embeddings.</summary>
    [Description("nomic-embed-text-v1.5")] NomicEmbedText,
    /// <summary>BGE-M3 (ColBERT) — multilingual, Multi-Vektor.</summary>
    [Description("bge-m3-colbert")] BgeM3,
    /// <summary>IBM Granite Embedding 125M (Englisch).</summary>
    [Description("granite-embedding-125m-english")] GraniteEmbeddingEn,
    /// <summary>IBM Granite Embedding 107M (multilingual).</summary>
    [Description("granite-embedding-107m-multilingual")] GraniteEmbeddingMulti,
    /// <summary>Google EmbeddingGemma 300M.</summary>
    [Description("embeddinggemma-300m")] EmbeddingGemma300m,
    /// <summary>Qwen3 Embedding 0.6B.</summary>
    [Description("qwen3-embedding-0.6b")] Qwen3Embedding0_6b,
    /// <summary>Qwen3 Embedding 4B.</summary>
    [Description("qwen3-embedding-4b")] Qwen3Embedding4b,
    /// <summary>Qwen3 Embedding 8B.</summary>
    [Description("qwen3-embedding-8b")] Qwen3Embedding8b,

    // — Weitere verifizierte Embedding-/Reranker-Modelle —
    /// <summary>all-MiniLM-L6-v2 — sehr kleiner, schneller Sentence-Transformer-Klassiker.</summary>
    [Description("all-MiniLM-L6-v2")] AllMiniLmL6v2,
    /// <summary>all-MiniLM-L6-v2 (OpenVINO) — CPU-optimierte Variante.</summary>
    [Description("openvino-all-MiniLM-L6-v2")] AllMiniLmL6v2OpenVino,
    /// <summary>Multilingual E5 Base (OpenVINO) — mehrsprachige Embeddings, CPU-optimiert.</summary>
    [Description("openvino-multilingual-e5-base")] MultilingualE5Base,
    /// <summary>Qwen3-VL Embedding 2B — multimodale (Text+Bild) Embeddings.</summary>
    [Description("qwen3-vl-embedding-2b")] Qwen3VlEmbedding2b,
    /// <summary>Qwen3-VL Embedding 8B — größere multimodale Embeddings.</summary>
    [Description("qwen3-vl-embedding-8b")] Qwen3VlEmbedding8b,
    /// <summary>Qwen3-VL Reranker 2B — multimodaler Reranker für RAG.</summary>
    [Description("qwen3-vl-reranker-2b-i1")] Qwen3VlReranker2b,
    /// <summary>Qwen3-VL Reranker 8B — größerer multimodaler Reranker.</summary>
    [Description("qwen3-vl-reranker-8b")] Qwen3VlReranker8b,
}

/// <summary>Resolves the LocalAI gallery name carried in an enum member's <see cref="DescriptionAttribute"/>.</summary>
internal static class GalleryNames
{
    public static string Of(Enum value)
    {
        var member = value.GetType().GetField(value.ToString());
        return member?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? value.ToString().ToLowerInvariant();
    }
}
