using System.ComponentModel;
using System.Reflection;

namespace Nextended.Aspire.Hosting.LocalAI;

/// <summary>
/// Well-known text-to-image models from the LocalAI model gallery.
/// The <see cref="DescriptionAttribute"/> holds the exact installable gallery name.
/// </summary>
public enum KnownImageModel
{
    /// <summary>Bundled default of the LocalAI AIO images (no extra download).</summary>
    [Description("stablediffusion")]
    StableDiffusionAio,

    /// <summary>Classic Stable Diffusion 1.5 (GGML, small and fast).</summary>
    [Description("sd-1.5-ggml")]
    StableDiffusion15,

    /// <summary>Stable Diffusion 3 Medium.</summary>
    [Description("stable-diffusion-3-medium")]
    StableDiffusion3Medium,

    /// <summary>Stable Diffusion 3.5 Medium (GGML).</summary>
    [Description("sd-3.5-medium-ggml")]
    StableDiffusion35Medium,

    /// <summary>Stable Diffusion 3.5 Large (GGML) — high quality, needs more VRAM.</summary>
    [Description("sd-3.5-large-ggml")]
    StableDiffusion35Large,

    /// <summary>DreamShaper — popular general-purpose SD fine-tune.</summary>
    [Description("dreamshaper")]
    DreamShaper,

    /// <summary>FLUX.1 schnell — fast, high-quality (Apache-2.0).</summary>
    [Description("flux.1-schnell")]
    Flux1Schnell,

    /// <summary>FLUX.1 dev (GGML quantized).</summary>
    [Description("flux.1-dev-ggml")]
    Flux1Dev,

    /// <summary>
    /// FLUX.1 dev abliterated v2 (GGML Q8) — uncensored variant without content filtering.
    /// Runs on the lightweight stablediffusion-ggml backend (~13 GB download).
    /// </summary>
    [Description("flux.1-dev-ggml-abliterated-v2-q8_0")]
    Flux1DevUncensored,

    /// <summary>
    /// FLUX.1 dev abliterated v2 (full fp16, diffusers backend) — highest quality, but a
    /// ~24 GB download and heavy VRAM usage. Prefer <see cref="Flux1DevUncensored"/> locally.
    /// </summary>
    [Description("flux.1dev-abliteratedv2")]
    Flux1DevUncensoredDiffusers,

    /// <summary>FLUX.1 Kontext dev — image editing / in-context generation.</summary>
    [Description("flux.1-kontext-dev")]
    Flux1KontextDev,

    /// <summary>FLUX.1 Krea dev (GGML) — photographic aesthetic without the "flux look".</summary>
    [Description("flux.1-krea-dev-ggml")]
    Flux1KreaDev,

    /// <summary>FLUX.2 dev — newest FLUX generation.</summary>
    [Description("flux.2-dev")]
    Flux2Dev,

    /// <summary>FLUX.2 klein 4B — small and fast FLUX.2 (low VRAM).</summary>
    [Description("flux.2-klein-4b")]
    Flux2Klein4b,

    /// <summary>FLUX.2 klein 9B — mid-size FLUX.2.</summary>
    [Description("flux.2-klein-9b")]
    Flux2Klein9b,

    /// <summary>Ideogram 4 (GGML Q8) — very strong text rendering.</summary>
    [Description("ideogram-4-q8_0-ggml")]
    Ideogram4,

    /// <summary>Z-Image Turbo (GGML) — very fast generations on the ggml backend.</summary>
    [Description("Z-Image-Turbo")]
    ZImageTurboGgml,

    /// <summary>Qwen-Image — strong text rendering inside images.</summary>
    [Description("qwen-image")]
    QwenImage,

    /// <summary>Chroma1-HD — 8.9B text-to-image derived from FLUX.1-schnell.</summary>
    [Description("chroma1-hd")]
    Chroma1Hd,

    /// <summary>Z-Image Turbo (diffusers) — very fast generations.</summary>
    [Description("z-image-turbo-diffusers")]
    ZImageTurbo,

    // — Additional verified LocalAI-gallery image models —

    /// <summary>Z-Image (diffusers) — full (non-turbo) Z-Image, higher quality than the turbo variant.</summary>
    [Description("z-image-diffusers")]
    ZImageDiffusers,

    /// <summary>Z-Image Turbo via the vllm-omni backend — fast generations served through vLLM.</summary>
    [Description("vllm-omni-z-image-turbo")]
    ZImageTurboVllm,

    /// <summary>FLUX.1 dev (diffusers, full fp16) — reference FLUX.1-dev; large download, heavy VRAM.</summary>
    [Description("flux.1-dev")]
    Flux1DevDiffusers,

    /// <summary>FLUX.1 dev (GGML Q8_0) — higher-quality quant than the default GGML build; more VRAM.</summary>
    [Description("flux.1-dev-ggml-q8_0")]
    Flux1DevGgmlQ8,

    /// <summary>FLUX.1 Krea dev (GGML Q8_0) — higher-quality quant of the photographic Krea model.</summary>
    [Description("flux.1-krea-dev-ggml-q8_0")]
    Flux1KreaDevGgmlQ8,

    /// <summary>Ideogram 4 (GGML IQ4_NL) — smaller/faster Ideogram 4 quant with strong text rendering.</summary>
    [Description("ideogram-4-iq4nl-ggml")]
    Ideogram4Iq4nl,

    /// <summary>Qwen-Image-Edit — instruction-driven image editing (edit an input image from a prompt).</summary>
    [Description("qwen-image-edit")]
    QwenImageEdit,

    /// <summary>Qwen-Image-Edit 2509 — updated (Sept 2025) Qwen image-editing model.</summary>
    [Description("qwen-image-edit-2509")]
    QwenImageEdit2509,
}

/// <summary>
/// Curated text-to-image models hosted on HuggingFace (diffusers format) that are NOT in the
/// LocalAI gallery. Loaded via <c>AddHuggingFaceModel</c>, which generates a diffusers model
/// config on the fly. The <see cref="DescriptionAttribute"/> holds the HF repo id.
/// All repos verified public/ungated.
/// </summary>
public enum KnownHuggingFaceImageModel
{
    /// <summary>RealVisXL V4.0 — photorealistic SDXL, renders NSFW without filters.</summary>
    [Description("SG161222/RealVisXL_V4.0")]
    RealVisXL4,

    /// <summary>RealVisXL V5.0 — newest photorealistic RealVis SDXL.</summary>
    [Description("SG161222/RealVisXL_V5.0")]
    RealVisXL5,

    /// <summary>UnfilteredAI NSFW-gen v2 — SDXL tuned for explicit content, unfiltered.</summary>
    [Description("UnfilteredAI/NSFW-gen-v2")]
    NsfwGenV2,

    /// <summary>UnfilteredAI NSFW-GEN-ANIME — anime-style NSFW SDXL.</summary>
    [Description("UnfilteredAI/NSFW-GEN-ANIME")]
    NsfwGenAnime,

    /// <summary>NSFW v1.0 (SDXL) — the "nsfw-v1" model from LocalAIHub.</summary>
    [Description("stablediffusionapi/nsfw")]
    NsfwV1,

    /// <summary>OmnigenXL — NSFW/SFW all-rounder SDXL.</summary>
    [Description("stablediffusionapi/omnigen-xl")]
    OmnigenXL,

    /// <summary>OmnigenXL NSFW/SFW v1.0 — the "omnigen-nsfw-v10" model from LocalAIHub.</summary>
    [Description("stablediffusionapi/omnigenxlnsfwsfw-v10")]
    OmnigenXLNsfw,

    /// <summary>waiIllustrious SDXL v15 — anime/illustrious NSFW SDXL.</summary>
    [Description("votepurchase/waiIllustriousSDXL_v150")]
    WaiIllustriousSDXL,

    /// <summary>Pony Diffusion V6 XL (SPO, diffusers) — hugely popular anime/furry SDXL, uncensored.</summary>
    [Description("John6666/pony-diffusion-v6-xl-sdxl-spo")]
    PonyDiffusionV6XL,

    /// <summary>DreamShaper XL v2 Turbo — fast SDXL turbo (few steps).</summary>
    [Description("Lykon/dreamshaper-xl-v2-turbo")]
    DreamShaperXLTurbo,

    /// <summary>Animagine XL 4.0 — high-quality anime SDXL.</summary>
    [Description("cagliostrolab/animagine-xl-4.0")]
    AnimagineXL4,

    /// <summary>Playground v2.5 — aesthetic-focused SDXL-class model.</summary>
    [Description("playgroundai/playground-v2.5-1024px-aesthetic")]
    PlaygroundV25,

    /// <summary>Stable Diffusion XL base 1.0 — the reference SDXL model.</summary>
    [Description("stabilityai/stable-diffusion-xl-base-1.0")]
    SdxlBase,

    /// <summary>DreamShaper 8 — extremely popular SD1.5 all-rounder.</summary>
    [Description("Lykon/dreamshaper-8")]
    DreamShaper8,

    /// <summary>majicMIX realistic v7 — photoreal SD1.5 (portraits, Asian aesthetics).</summary>
    [Description("digiplay/majicMIX_realistic_v7")]
    MajicMixRealistic,

    /// <summary>Juggernaut XL v9 — top-tier photorealistic SDXL.</summary>
    [Description("RunDiffusion/Juggernaut-XL-v9")]
    JuggernautXLv9,

    /// <summary>epiCRealism XL v7 (Final Destination) — photorealistic SDXL.</summary>
    [Description("misri/epicrealismXL_v7FinalDestination")]
    EpicRealismXL,

    /// <summary>epiCRealism (SD1.5) — beloved photoreal SD1.5 model.</summary>
    [Description("emilianJR/epiCRealism")]
    EpicRealism15,

    /// <summary>Realistic Vision v5.1 — classic photoreal SD1.5.</summary>
    [Description("stablediffusionapi/realistic-vision-v51")]
    RealisticVision51,

    /// <summary>Animagine XL 3.1 — high-quality anime SDXL (predecessor of 4.0).</summary>
    [Description("cagliostrolab/animagine-xl-3.1")]
    AnimagineXL31,

    /// <summary>RealVisXL V5.0 Lightning — photoreal SDXL, ~6 steps (very fast).</summary>
    [Description("SG161222/RealVisXL_V5.0_Lightning")]
    RealVisXL5Lightning,

    // — Additional curated HuggingFace diffusers repos (photoreal / anime / NSFW) —

    /// <summary>Juggernaut XL v8 (RunDiffusion) — earlier, very popular photorealistic SDXL.</summary>
    [Description("RunDiffusion/Juggernaut-XL-v8")]
    JuggernautXLv8,

    /// <summary>SDXL Turbo — single-/few-step SDXL from Stability AI (very fast).</summary>
    [Description("stabilityai/sdxl-turbo")]
    SdxlTurbo,

    /// <summary>Stable Diffusion v1.5 (reference SD1.5 checkpoint, diffusers).</summary>
    [Description("stable-diffusion-v1-5/stable-diffusion-v1-5")]
    StableDiffusion15Diffusers,

    /// <summary>DreamShaper 7 (SD1.5) — popular all-rounder, predecessor of DreamShaper 8.</summary>
    [Description("Lykon/dreamshaper-7")]
    DreamShaper7,

    /// <summary>AbsoluteReality v1.8.1 (SD1.5) — photoreal SD1.5 all-rounder.</summary>
    [Description("digiplay/AbsoluteReality_v1.8.1")]
    AbsoluteReality,

    /// <summary>CyberRealistic v3.3 (SD1.5) — photoreal SD1.5, renders NSFW.</summary>
    [Description("digiplay/CyberRealistic_V3.3")]
    CyberRealistic,

    /// <summary>Deliberate v2 (SD1.5) — versatile, widely used SD1.5 model.</summary>
    [Description("XpucT/Deliberate")]
    DeliberateV2,

    /// <summary>NeverEnding Dream (SD1.5) — anime/semi-real SD1.5.</summary>
    [Description("Lykon/NeverEnding-Dream")]
    NeverEndingDream,

    /// <summary>Counterfeit V3.0 (SD1.5) — high-quality anime SD1.5.</summary>
    [Description("gsdf/Counterfeit-V3.0")]
    CounterfeitV3,

    /// <summary>MeinaMix v11 (SD1.5) — extremely popular anime SD1.5.</summary>
    [Description("Meina/MeinaMix_V11")]
    MeinaMix,

    /// <summary>NoobAI-XL (NAI-XL) EPS v1.1 — Illustrious-based anime SDXL, uncensored.</summary>
    [Description("John6666/noobai-xl-nai-xl-epsilonpred11version-sdxl")]
    NoobAiXL,

    /// <summary>Illustrious XL v0.1 — the base Illustrious anime SDXL model.</summary>
    [Description("OnomaAIResearch/Illustrious-xl-early-release-v0")]
    IllustriousXL,
}

/// <summary>
/// A text-to-image model reference. Implicitly convertible from <see cref="string"/>
/// (any LocalAI gallery name, OCI/huggingface URI or config URL) and from
/// <see cref="KnownImageModel"/> for a friendly, discoverable API:
/// <code>
/// imagegen.AddModel(KnownImageModel.Flux1Schnell);
/// imagegen.AddModel("dreamshaper");
/// </code>
/// </summary>
public sealed class ImageModel
{
    /// <summary>The model id consumers pass to the API (and shown in /v1/models).</summary>
    public string Name { get; }

    /// <summary>
    /// What goes into LocalAI's MODELS list to install/load this model:
    /// a gallery name, URI — or a container path to a generated config yaml.
    /// Defaults to <see cref="Name"/>.
    /// </summary>
    internal string Reference { get; init; }

    public ImageModel(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Model name must not be empty.", nameof(name));
        Name = name.Trim();
        Reference = Name;
    }

    public ImageModel(KnownImageModel model) : this(NameOf(model)) { }

    /// <summary>Resolves the HF repo id of a <see cref="KnownHuggingFaceImageModel"/>.</summary>
    public static string RepoOf(KnownHuggingFaceImageModel model)
    {
        var member = typeof(KnownHuggingFaceImageModel).GetField(model.ToString());
        return member?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? model.ToString();
    }

    /// <summary>Recommended sampler steps for a known HF model (turbo models need few).</summary>
    public static int StepsOf(KnownHuggingFaceImageModel model) => model switch
    {
        KnownHuggingFaceImageModel.DreamShaperXLTurbo => 8,
        KnownHuggingFaceImageModel.RealVisXL5Lightning => 6,
        _ => 25,
    };

    /// <summary>
    /// Whether to load the fp16 file variant. This is per-repo: some repos ship ONLY
    /// <c>*.fp16.safetensors</c> (need f16=true), others ONLY default-named weights (need
    /// f16=false); the wrong setting fails with "variant=fp16, no such files" or
    /// "necessary safetensors weights ... (variant=None)". Values verified against each
    /// repo's unet folder. "BOTH"-repos use fp16 to save VRAM.
    /// </summary>
    public static bool F16Of(KnownHuggingFaceImageModel model) => model switch
    {
        KnownHuggingFaceImageModel.OmnigenXL => false,           // DEFAULT-only
        KnownHuggingFaceImageModel.PonyDiffusionV6XL => false,   // DEFAULT-only
        KnownHuggingFaceImageModel.AnimagineXL4 => false,        // DEFAULT-only
        KnownHuggingFaceImageModel.AnimagineXL31 => false,       // DEFAULT-only
        KnownHuggingFaceImageModel.EpicRealism15 => false,       // DEFAULT-only
        KnownHuggingFaceImageModel.RealisticVision51 => false,   // DEFAULT-only
        _ => true,                                               // FP16-only or BOTH
    };

    /// <summary>Resolves the gallery name of a <see cref="KnownImageModel"/> via its <see cref="DescriptionAttribute"/>.</summary>
    public static string NameOf(KnownImageModel model)
    {
        var member = typeof(KnownImageModel).GetField(model.ToString());
        return member?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? model.ToString().ToLowerInvariant();
    }

    public static implicit operator ImageModel(string name) => new(name);
    public static implicit operator ImageModel(KnownImageModel model) => new(model);
    public static implicit operator string(ImageModel model) => model.Name;

    public override string ToString() => Name;
}
