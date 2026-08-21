using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// The hosted model providers, one call each. All of them are <see cref="WebDataStudioAssistantExtensions.WithAssistant(IResourceBuilder{WebDataStudioResource}, string, string?, string?)"/>
/// with the right URL and a sensible default model — the point is that nobody has to look the URL
/// up to get started.
/// </summary>
/// <remarks>
/// Every provider here speaks the OpenAI chat-completions shape, which is what the studio talks.
/// For Anthropic that is their OpenAI-compatible endpoint rather than the native Messages API; it
/// takes the same key.
/// </remarks>
public static class WebDataStudioProviderExtensions
{
    /// <summary>Anthropic's OpenAI-compatible endpoint.</summary>
    public const string ClaudeEndpoint = "https://api.anthropic.com/v1/chat/completions";

    /// <summary>OpenAI.</summary>
    public const string OpenAiEndpoint = "https://api.openai.com/v1/chat/completions";

    /// <summary>OpenRouter, which fronts many models behind one key.</summary>
    public const string OpenRouterEndpoint = "https://openrouter.ai/api/v1/chat/completions";

    /// <summary>Groq.</summary>
    public const string GroqEndpoint = "https://api.groq.com/openai/v1/chat/completions";

    /// <summary>Mistral.</summary>
    public const string MistralEndpoint = "https://api.mistral.ai/v1/chat/completions";

    /// <summary>DeepSeek.</summary>
    public const string DeepSeekEndpoint = "https://api.deepseek.com/v1/chat/completions";

    /// <summary>Google's OpenAI-compatible Gemini endpoint.</summary>
    public const string GeminiEndpoint =
        "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";

    /// <summary>Assistance from Claude, through Anthropic's OpenAI-compatible endpoint.</summary>
    /// <param name="builder">The studio.</param>
    /// <param name="apiKey">An Anthropic API key.</param>
    /// <param name="model">Defaults to a current Sonnet.</param>
    public static IResourceBuilder<WebDataStudioResource> WithClaudeAssistant(
        this IResourceBuilder<WebDataStudioResource> builder, string apiKey,
        string model = "claude-sonnet-4-5") =>
        builder.WithAssistant(ClaudeEndpoint, model, apiKey);

    /// <summary>Assistance from Claude, with the key from an Aspire parameter.</summary>
    public static IResourceBuilder<WebDataStudioResource> WithClaudeAssistant(
        this IResourceBuilder<WebDataStudioResource> builder,
        IResourceBuilder<ParameterResource> apiKey, string model = "claude-sonnet-4-5") =>
        builder.WithAssistant(ClaudeEndpoint, apiKey, model);

    /// <summary>Assistance from OpenAI.</summary>
    public static IResourceBuilder<WebDataStudioResource> WithChatGptAssistant(
        this IResourceBuilder<WebDataStudioResource> builder, string apiKey,
        string model = "gpt-4o-mini") =>
        builder.WithAssistant(OpenAiEndpoint, model, apiKey);

    /// <summary>Assistance from OpenAI, with the key from an Aspire parameter.</summary>
    public static IResourceBuilder<WebDataStudioResource> WithChatGptAssistant(
        this IResourceBuilder<WebDataStudioResource> builder,
        IResourceBuilder<ParameterResource> apiKey, string model = "gpt-4o-mini") =>
        builder.WithAssistant(OpenAiEndpoint, apiKey, model);

    /// <summary>Assistance through OpenRouter, where the model name carries the provider.</summary>
    public static IResourceBuilder<WebDataStudioResource> WithOpenRouterAssistant(
        this IResourceBuilder<WebDataStudioResource> builder, string apiKey,
        string model = "anthropic/claude-sonnet-4.5") =>
        builder.WithAssistant(OpenRouterEndpoint, model, apiKey);

    /// <summary>Assistance through OpenRouter, with the key from an Aspire parameter.</summary>
    public static IResourceBuilder<WebDataStudioResource> WithOpenRouterAssistant(
        this IResourceBuilder<WebDataStudioResource> builder,
        IResourceBuilder<ParameterResource> apiKey,
        string model = "anthropic/claude-sonnet-4.5") =>
        builder.WithAssistant(OpenRouterEndpoint, apiKey, model);

    /// <summary>Assistance from Groq.</summary>
    public static IResourceBuilder<WebDataStudioResource> WithGroqAssistant(
        this IResourceBuilder<WebDataStudioResource> builder, string apiKey,
        string model = "llama-3.3-70b-versatile") =>
        builder.WithAssistant(GroqEndpoint, model, apiKey);

    /// <summary>Assistance from Groq, with the key from an Aspire parameter.</summary>
    public static IResourceBuilder<WebDataStudioResource> WithGroqAssistant(
        this IResourceBuilder<WebDataStudioResource> builder,
        IResourceBuilder<ParameterResource> apiKey, string model = "llama-3.3-70b-versatile") =>
        builder.WithAssistant(GroqEndpoint, apiKey, model);

    /// <summary>Assistance from Mistral.</summary>
    public static IResourceBuilder<WebDataStudioResource> WithMistralAssistant(
        this IResourceBuilder<WebDataStudioResource> builder, string apiKey,
        string model = "mistral-large-latest") =>
        builder.WithAssistant(MistralEndpoint, model, apiKey);

    /// <summary>Assistance from Mistral, with the key from an Aspire parameter.</summary>
    public static IResourceBuilder<WebDataStudioResource> WithMistralAssistant(
        this IResourceBuilder<WebDataStudioResource> builder,
        IResourceBuilder<ParameterResource> apiKey, string model = "mistral-large-latest") =>
        builder.WithAssistant(MistralEndpoint, apiKey, model);

    /// <summary>Assistance from DeepSeek.</summary>
    public static IResourceBuilder<WebDataStudioResource> WithDeepSeekAssistant(
        this IResourceBuilder<WebDataStudioResource> builder, string apiKey,
        string model = "deepseek-chat") =>
        builder.WithAssistant(DeepSeekEndpoint, model, apiKey);

    /// <summary>Assistance from DeepSeek, with the key from an Aspire parameter.</summary>
    public static IResourceBuilder<WebDataStudioResource> WithDeepSeekAssistant(
        this IResourceBuilder<WebDataStudioResource> builder,
        IResourceBuilder<ParameterResource> apiKey, string model = "deepseek-chat") =>
        builder.WithAssistant(DeepSeekEndpoint, apiKey, model);

    /// <summary>Assistance from Gemini, through Google's OpenAI-compatible endpoint.</summary>
    public static IResourceBuilder<WebDataStudioResource> WithGeminiAssistant(
        this IResourceBuilder<WebDataStudioResource> builder, string apiKey,
        string model = "gemini-2.5-flash") =>
        builder.WithAssistant(GeminiEndpoint, model, apiKey);

    /// <summary>Assistance from Gemini, with the key from an Aspire parameter.</summary>
    public static IResourceBuilder<WebDataStudioResource> WithGeminiAssistant(
        this IResourceBuilder<WebDataStudioResource> builder,
        IResourceBuilder<ParameterResource> apiKey, string model = "gemini-2.5-flash") =>
        builder.WithAssistant(GeminiEndpoint, apiKey, model);

    /// <summary>
    /// Assistance from an Azure OpenAI deployment. Azure puts the deployment in the path and the
    /// api-version in the query, so the URL is built here rather than guessed.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="resourceName">The Azure OpenAI resource name, i.e. <c>{name}.openai.azure.com</c>.</param>
    /// <param name="deployment">The deployment name.</param>
    /// <param name="apiKey">The key. Azure also accepts it as a bearer token.</param>
    /// <param name="apiVersion">API version, defaults to a current one.</param>
    public static IResourceBuilder<WebDataStudioResource> WithAzureOpenAiAssistant(
        this IResourceBuilder<WebDataStudioResource> builder, string resourceName,
        string deployment, string apiKey, string apiVersion = "2024-10-21")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(deployment);

        var endpoint = $"https://{resourceName}.openai.azure.com/openai/deployments/{deployment}" +
                       $"/chat/completions?api-version={apiVersion}";

        return builder.WithAssistant(endpoint, deployment, apiKey);
    }
}
