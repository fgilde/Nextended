using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// Wires the studio's optional assistance — explain a statement, draft one from a question — to an
/// OpenAI-compatible endpoint. Without one of these calls the feature does not exist: no button in
/// the UI, no calls anywhere, and <c>/api/health</c> reports <c>assist: false</c>.
/// </summary>
/// <remarks>
/// What leaves the studio is the statement or the question, and — only when the user turns the
/// switch on — the table and column names of the connection. Never a row of data. Nothing the model
/// answers is executed: a suggested statement lands in the editor, where it goes through the same
/// run and the same preview as anything typed by hand.
/// </remarks>
public static class WebDataStudioAssistantExtensions
{
    /// <summary>The path an OpenAI-compatible server serves chat completions on.</summary>
    public const string ChatCompletionsPath = "/v1/chat/completions";

    /// <summary>Model used when a call does not name one.</summary>
    public const string DefaultModel = "gpt-4o-mini";

    /// <summary>
    /// Points the assistance at an OpenAI-compatible chat-completions URL.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="endpoint">
    /// The full chat-completions URL, e.g. <c>https://api.openai.com/v1/chat/completions</c>.
    /// </param>
    /// <param name="model">Model name. Defaults to <see cref="DefaultModel"/>.</param>
    /// <param name="apiKey">Sent as <c>Authorization: Bearer</c>. Omit for an endpoint that needs none.</param>
    public static IResourceBuilder<WebDataStudioResource> WithAssistant(
        this IResourceBuilder<WebDataStudioResource> builder, string endpoint,
        string? model = null, string? apiKey = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        builder.WithEnvironment("WDS_ASSIST_ENDPOINT", endpoint);
        if (apiKey is { Length: > 0 }) builder.WithEnvironment("WDS_ASSIST_KEY", apiKey);

        return builder.WithModel(model);
    }

    /// <summary>
    /// Points the assistance at an endpoint that is only known at start — another resource's URL,
    /// a value from configuration. Build it with <see cref="ReferenceExpression.Create"/>.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithAssistant(
        this IResourceBuilder<WebDataStudioResource> builder, ReferenceExpression endpoint,
        string? model = null, string? apiKey = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(endpoint);

        builder.WithEnvironment("WDS_ASSIST_ENDPOINT", endpoint);
        if (apiKey is { Length: > 0 }) builder.WithEnvironment("WDS_ASSIST_KEY", apiKey);

        return builder.WithModel(model);
    }

    /// <summary>Points the assistance at a URL, with the key from an Aspire parameter.</summary>
    public static IResourceBuilder<WebDataStudioResource> WithAssistant(
        this IResourceBuilder<WebDataStudioResource> builder, string endpoint,
        IResourceBuilder<ParameterResource> apiKey, string? model = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        return builder
            .WithEnvironment("WDS_ASSIST_ENDPOINT", endpoint)
            .WithEnvironment("WDS_ASSIST_KEY", apiKey)
            .WithModel(model);
    }

    /// <summary>
    /// Points the assistance at a model server running in this stack — Ollama, LocalAI, vLLM,
    /// llama.cpp, anything that speaks the OpenAI chat-completions shape.
    /// </summary>
    /// <remarks>
    /// The studio talks to it over the container network, so nothing about the conversation leaves
    /// the machine. The studio also waits for the server: an assistant button that answers
    /// "connection refused" for the first minute is worse than one that arrives a moment later.
    /// </remarks>
    /// <param name="builder">The studio.</param>
    /// <param name="server">The model server. Any resource with an HTTP endpoint.</param>
    /// <param name="model">
    /// Model name as that server knows it — <c>llama3.2</c> for Ollama, <c>qwen3-8b</c> for LocalAI.
    /// </param>
    /// <param name="apiKey">Sent as a bearer token. Most local servers ignore it.</param>
    /// <param name="path">
    /// Path to the chat-completions API, if the server does not serve it at
    /// <see cref="ChatCompletionsPath"/>.
    /// </param>
    /// <param name="endpointName">Endpoint of the server to use, when it has several.</param>
    public static IResourceBuilder<WebDataStudioResource> WithAssistant<TServer>(
        this IResourceBuilder<WebDataStudioResource> builder, IResourceBuilder<TServer> server,
        string model, string? apiKey = null, string? path = null, string? endpointName = null)
        where TServer : IResource, IResourceWithEndpoints
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(server);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        var endpoint = endpointName is { Length: > 0 }
            ? server.GetEndpoint(endpointName)
            : PrimaryEndpointOf(server);

        var suffix = Normalise(path ?? ChatCompletionsPath);

        builder.WithEnvironment("WDS_ASSIST_ENDPOINT",
            ReferenceExpression.Create($"{endpoint}{suffix}"));

        if (apiKey is { Length: > 0 }) builder.WithEnvironment("WDS_ASSIST_KEY", apiKey);

        // A model server that is still pulling its weights is not an error, only slow to start.
        // Re-created as an untyped builder because WaitFor takes IResourceBuilder<IResource>, and
        // IResourceBuilder<T> is invariant.
        var dependency = builder.ApplicationBuilder.CreateResourceBuilder<IResource>(server.Resource);

        return builder.WaitFor(dependency).WithModel(model);
    }

    /// <summary>
    /// The same as <see cref="WithAssistant{TServer}"/>, named for the server most people reach for
    /// first. Use it with <c>builder.AddOllama(...)</c> from the CommunityToolkit, and give it a
    /// model that Ollama has been told to pull.
    /// </summary>
    /// <example>
    /// <code>
    /// var ollama = builder.AddOllama("ollama").WithDataVolume();
    /// ollama.AddModel("llama3.2");
    ///
    /// builder.AddWebDataStudio()
    ///     .WithReference(shop)
    ///     .WithOllamaAssistant(ollama, "llama3.2");
    /// </code>
    /// </example>
    public static IResourceBuilder<WebDataStudioResource> WithOllamaAssistant<TServer>(
        this IResourceBuilder<WebDataStudioResource> builder, IResourceBuilder<TServer> ollama,
        string model = "llama3.2", string? endpointName = null)
        where TServer : IResource, IResourceWithEndpoints =>
        builder.WithAssistant(ollama, model, apiKey: null, path: null, endpointName: endpointName);

    /// <summary>
    /// The same for a LocalAI instance, which serves the OpenAI API at the same path. Pass the
    /// model as LocalAI names it.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithLocalAiAssistant<TServer>(
        this IResourceBuilder<WebDataStudioResource> builder, IResourceBuilder<TServer> localAi,
        string model, string? apiKey = null, string? endpointName = null)
        where TServer : IResource, IResourceWithEndpoints =>
        builder.WithAssistant(localAi, model, apiKey, path: null, endpointName: endpointName);

    private static IResourceBuilder<WebDataStudioResource> WithModel(
        this IResourceBuilder<WebDataStudioResource> builder, string? model)
    {
        var chosen = string.IsNullOrWhiteSpace(model) ? DefaultModel : model.Trim();
        builder.Resource.AssistantModel = chosen;

        return builder.WithEnvironment("WDS_ASSIST_MODEL", chosen);
    }

    /// The endpoint named "http", if there is one, and otherwise the resource's first.
    private static EndpointReference PrimaryEndpointOf<TServer>(IResourceBuilder<TServer> server)
        where TServer : IResource, IResourceWithEndpoints
    {
        var names = server.Resource.Annotations.OfType<EndpointAnnotation>()
            .Select(annotation => annotation.Name)
            .ToList();

        if (names.Count == 0)
            throw new InvalidOperationException(
                $"'{server.Resource.Name}' has no endpoint to talk to; give the studio the URL " +
                "with WithAssistant(endpoint) instead.");

        var name = names.FirstOrDefault(n => n.Equals("http", StringComparison.OrdinalIgnoreCase))
                   ?? names[0];

        return server.GetEndpoint(name);
    }

    /// A path that starts with '/' and does not end with one.
    private static string Normalise(string path)
    {
        var trimmed = path.Trim().TrimEnd('/');
        if (trimmed.Length == 0) return "";

        return trimmed.StartsWith('/') ? trimmed : "/" + trimmed;
    }
}
