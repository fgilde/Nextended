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

    /// <summary>Whisper large v3 (1.55B) — best accuracy, multilingual; wants a GPU for speed.</summary>
    [Description("whisper-large-v3")]
    WhisperLargeV3,
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
    /// <summary>Wan 2.2 text-to-video via the vllm-omni backend (gallery id <c>vllm-omni-wan2.2-t2v</c>).</summary>
    [Description("vllm-omni-wan2.2-t2v")]
    Wan22TextToVideo,
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
