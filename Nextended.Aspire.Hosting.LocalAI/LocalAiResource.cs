using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.LocalAI;

/// <summary>
/// A self-hosted, OpenAI-compatible multimodal AI service (backend: LocalAI). One container
/// serves image generation (<c>/v1/images/generations</c>), text-to-speech (<c>/v1/audio/speech</c>),
/// speech-to-text (<c>/v1/audio/transcriptions</c>), video generation (<c>/video</c>),
/// sound / music generation (<c>/v1/sound-generation</c>), chat and embeddings — plus LocalAI's
/// built-in WebUI, all on the same endpoint.
/// </summary>
public sealed class LocalAiResource(string name) : ContainerResource(name)
{
    /// <summary>Default internal container port LocalAI listens on.</summary>
    public const int DefaultTargetPort = 8080;

    /// <summary>Name of the primary HTTP endpoint.</summary>
    public const string HttpEndpointName = "http";

    /// <summary>The HTTP endpoint that serves the OpenAI-compatible API (and LocalAI WebUI).</summary>
    public EndpointReference HttpEndpoint => new(this, HttpEndpointName);

    /// <summary>
    /// Models registered via <c>AddModel</c>/<c>AddTextToSpeechModel</c>/<c>AddSpeechToTextModel</c>/
    /// <c>AddVideoModel</c>/<c>AddSoundModel</c> (installed from the LocalAI gallery on startup),
    /// each tagged with its modality.
    /// </summary>
    public IList<RegisteredModel> Models { get; } = [];

    /// <summary>Whether the container was configured with GPU acceleration (used for generated configs).</summary>
    internal bool GpuEnabled { get; set; }

    /// <summary>Host directory with generated HuggingFace model configs (bind-mounted once).</summary>
    internal string? HfConfigDir { get; set; }

    /// <summary>
    /// The default IMAGE model consumers use (first image model added wins; falls back to the
    /// AIO-bundled <c>stablediffusion</c> when no image model was added explicitly).
    /// </summary>
    public string DefaultModel => DefaultModelFor(ModelModality.Image) ?? ImageModel.NameOf(KnownImageModel.StableDiffusionAio);

    /// <summary>The default model id for a modality (first added of that kind), or <c>null</c> if none was added.</summary>
    public string? DefaultModelFor(ModelModality modality)
        => Models.FirstOrDefault(m => m.Modality == modality)?.Name;
}

/// <summary>The kind of generation a model performs — determines which default-model env var it drives.</summary>
public enum ModelModality
{
    /// <summary>Text-to-image (<c>/v1/images/generations</c>).</summary>
    Image,
    /// <summary>Text-to-speech (<c>/v1/audio/speech</c>).</summary>
    TextToSpeech,
    /// <summary>Speech-to-text / transcription (<c>/v1/audio/transcriptions</c>).</summary>
    SpeechToText,
    /// <summary>Text/image-to-video (<c>POST /video</c>).</summary>
    Video,
    /// <summary>Text-to-sound / music generation (<c>POST /v1/sound-generation</c>).</summary>
    Sound,
    /// <summary>Chat / LLM text generation — incl. vision-capable multimodal (<c>POST /v1/chat/completions</c>).</summary>
    Text,
    /// <summary>Text embeddings for semantic search / RAG (<c>POST /v1/embeddings</c>).</summary>
    Embedding,
}

/// <summary>A model queued for install on LocalAI startup, tagged with its <see cref="ModelModality"/>.</summary>
/// <param name="Name">The model id consumers pass to the API (and shown in /v1/models).</param>
/// <param name="Reference">What goes into LocalAI's MODELS list: a gallery name, URI or container path to a generated config yaml.</param>
/// <param name="Modality">The generation kind this model performs.</param>
public sealed record RegisteredModel(string Name, string Reference, ModelModality Modality);

/// <summary>An Open WebUI container wired to a <see cref="LocalAiResource"/>.</summary>
public sealed class LocalAiOpenWebUIResource(string name) : ContainerResource(name);

/// <summary>A standalone SD.Next image-generation studio attached to the stack.</summary>
public sealed class SdNextResource(string name) : ContainerResource(name);

/// <summary>The ACE-Step 1.5 server (Gradio + REST API) backing an <see cref="AceStepUiResource"/>.</summary>
public sealed class AceStepApiResource(string name) : ContainerResource(name);

/// <summary>The ace-step-ui music studio (Suno-style frontend for ACE-Step), built from source.</summary>
public sealed class AceStepUiResource(string name) : ContainerResource(name);
