using System.ComponentModel;
using System.Reflection;

namespace Nextended.Aspire.Hosting.LocalAI;

/// <summary>
/// Curated, well-known text-to-image <b>LoRA adapters</b> hosted on HuggingFace. A LoRA is NOT a
/// standalone model — it is applied on top of a base checkpoint. <c>AddModel</c>/<c>AddImageLora</c>
/// generate a diffusers config that (a) downloads the LoRA <c>.safetensors</c> into the model dir and
/// (b) applies it on the matching base (here all on SDXL base 1.0). The <see cref="DescriptionAttribute"/>
/// holds the LoRA repo id; base + weight file are resolved in <see cref="ImageLora"/>. These are
/// general-purpose <b>style</b> LoRAs — put your own domain-specific LoRAs directly in your AppHost via
/// <c>AddImageLora(name, base, "owner/repo")</c>. Most need a trigger phrase in the prompt (noted below).
/// </summary>
public enum KnownHuggingFaceImageLora
{
    /// <summary>Pixel Art XL (nerijs) — retro pixel-art style. Trigger: "pixel art".</summary>
    [Description("nerijs/pixel-art-xl")]
    PixelArt,

    /// <summary>IKEA Instructions (ostris) — flat IKEA-manual illustration style. Trigger: "ikea instructions".</summary>
    [Description("ostris/ikea-instructions-lora-sdxl")]
    IkeaInstructions,

    /// <summary>Papercut (TheLastBen) — layered paper-cutout look. Trigger: "papercut".</summary>
    [Description("TheLastBen/Papercut_SDXL")]
    Papercut,

    /// <summary>Toy Face (CiroN2022) — cute 3D toy/figurine faces. Trigger: "toy_face".</summary>
    [Description("CiroN2022/toy-face")]
    ToyFace,

    /// <summary>3D Render Style (goofyai) — clean 3D-render aesthetic. Trigger: "3d style" / "3d render".</summary>
    [Description("goofyai/3d_render_style_xl")]
    ThreeDRender,

    /// <summary>Graphic Novel Illustration (blink7630) — inked comic/graphic-novel look. Trigger: "graphic novel illustration".</summary>
    [Description("blink7630/graphic-novel-illustration")]
    GraphicNovel,

    /// <summary>Line-Art / Manga (artificialguybr, LineAniRedmond V2) — clean line art. Trigger: "LineAniAF" / "lineart".</summary>
    [Description("artificialguybr/LineAniRedmond-LinearMangaSDXL-V2")]
    LineArtManga,
}

/// <summary>Resolver for <see cref="KnownHuggingFaceImageLora"/>: LoRA repo id, weight file and the base model to apply it on.</summary>
internal static class ImageLora
{
    /// <summary>The LoRA repo id carried in the enum member's <see cref="DescriptionAttribute"/>.</summary>
    public static string RepoOf(KnownHuggingFaceImageLora lora)
    {
        var member = typeof(KnownHuggingFaceImageLora).GetField(lora.ToString());
        return member?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? lora.ToString();
    }

    /// <summary>The LoRA weight file inside the repo (verified per repo — file names are NOT uniform across authors).</summary>
    public static string FileOf(KnownHuggingFaceImageLora lora) => lora switch
    {
        KnownHuggingFaceImageLora.PixelArt => "pixel-art-xl.safetensors",
        KnownHuggingFaceImageLora.IkeaInstructions => "ikea_instructions_xl_v1_5.safetensors",
        KnownHuggingFaceImageLora.Papercut => "papercut.safetensors",
        KnownHuggingFaceImageLora.ToyFace => "toy_face_sdxl.safetensors",
        KnownHuggingFaceImageLora.ThreeDRender => "3d_render_style_xl.safetensors",
        KnownHuggingFaceImageLora.GraphicNovel => "Graphic_Novel_Illustration-000007.safetensors",
        KnownHuggingFaceImageLora.LineArtManga => "LineAniRedmondV2-Lineart-LineAniAF.safetensors",
        _ => throw new ArgumentOutOfRangeException(nameof(lora)),
    };

    /// <summary>The base checkpoint the LoRA is applied on (all curated entries target SDXL base 1.0).</summary>
    public static KnownHuggingFaceImageModel BaseOf(KnownHuggingFaceImageLora lora) => KnownHuggingFaceImageModel.SdxlBase;
}
