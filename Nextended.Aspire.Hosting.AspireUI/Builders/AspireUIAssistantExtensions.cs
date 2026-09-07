using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.AspireUI;

/// <summary>
/// Gives AspireUI's assistant a backend: an OpenAI-compatible endpoint, a model server running in
/// this stack, or an agent CLI installed on the AspireUI host. Same shape as the studio's
/// <c>WithAssistant</c> in <c>Nextended.Aspire.Hosting.WebDataStudio</c>, so the two packages are
/// learned once.
/// <para>
/// Once a backend is configured the assistant is more than a text box: it appears on every page of
/// AspireUI and can operate the instance through the same tools the MCP server exposes, with the
/// permissions of whoever is typing. That needs an HTTP endpoint — an agent CLI takes a prompt and
/// returns text, and there is nowhere in that to put a tool call.
/// </para>
/// </summary>
public static class AspireUIAssistantExtensions
{
    /// <summary>
    /// The path an OpenAI-compatible server serves the chat API under. AspireUI adds
    /// <c>/chat/completions</c> itself, so this is the version segment and not the whole path.
    /// </summary>
    public const string DefaultApiPath = "/v1";

    /// <summary>The model Ollama pulls by default in most examples.</summary>
    public const string DefaultOllamaModel = "llama3.2";

    /// <summary>The agent CLIs AspireUI knows how to drive.</summary>
    public static readonly string[] CliTools = ["claude", "gemini", "ollama", "llm", "codex"];

    // --- A URL you already know ---------------------------------------------------------------

    /// <summary>
    /// Points the assistant at an OpenAI-compatible endpoint.
    /// </summary>
    /// <param name="builder">The AspireUI resource.</param>
    /// <param name="endpoint">
    /// Base url, with or without the version segment: <c>http://localhost:11434</c> and
    /// <c>https://integrate.api.nvidia.com/v1</c> both work.
    /// </param>
    /// <param name="model">
    /// Model name as that provider knows it. Required, unlike the studio's equivalent: AspireUI
    /// treats an assistant without a model as not configured, and a default here would be a guess
    /// at a name that may not exist at the provider.
    /// </param>
    /// <param name="apiKey">Sent as a bearer token. Most local servers ignore it.</param>
    /// <param name="label">What the UI calls the provider, e.g. "Ollama".</param>
    public static IResourceBuilder<AspireUIResource> WithAssistant(
        this IResourceBuilder<AspireUIResource> builder, string endpoint, string model,
        string? apiKey = null, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        builder.WithEnvironment("ASPIREUI_AI_BASE_URL", endpoint.Trim().TrimEnd('/'));
        if (apiKey is { Length: > 0 }) builder.WithEnvironment("ASPIREUI_AI_API_KEY", apiKey);
        return builder.WithModel(model, label);
    }

    /// <summary>
    /// Points the assistant at an endpoint that is only known at start — another resource's url, a
    /// value from configuration. Build it with <see cref="ReferenceExpression.Create"/>.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithAssistant(
        this IResourceBuilder<AspireUIResource> builder, ReferenceExpression endpoint, string model,
        string? apiKey = null, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(endpoint);

        builder.WithEnvironment("ASPIREUI_AI_BASE_URL", endpoint);
        if (apiKey is { Length: > 0 }) builder.WithEnvironment("ASPIREUI_AI_API_KEY", apiKey);
        return builder.WithModel(model, label);
    }

    /// <summary>Points the assistant at a url, with the key from an Aspire parameter.</summary>
    public static IResourceBuilder<AspireUIResource> WithAssistant(
        this IResourceBuilder<AspireUIResource> builder, string endpoint,
        IResourceBuilder<ParameterResource> apiKey, string model, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        return builder
            .WithEnvironment("ASPIREUI_AI_BASE_URL", endpoint.Trim().TrimEnd('/'))
            .WithEnvironment("ASPIREUI_AI_API_KEY", apiKey)
            .WithModel(model, label);
    }

    /// <inheritdoc cref="WithAssistant(IResourceBuilder{AspireUIResource}, string, IResourceBuilder{ParameterResource}, string, string?)"/>
    public static IResourceBuilder<AspireUIResource> WithAssistant(
        this IResourceBuilder<AspireUIResource> builder, ReferenceExpression endpoint,
        IResourceBuilder<ParameterResource> apiKey, string model, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(apiKey);

        return builder
            .WithEnvironment("ASPIREUI_AI_BASE_URL", endpoint)
            .WithEnvironment("ASPIREUI_AI_API_KEY", apiKey)
            .WithModel(model, label);
    }

    // --- A model server in this stack -----------------------------------------------------------

    /// <summary>
    /// Points the assistant at a model server running in this stack — Ollama, LocalAI, vLLM,
    /// llama.cpp, anything that speaks the OpenAI chat-completions shape.
    /// </summary>
    /// <remarks>
    /// AspireUI talks to it over the container network, so nothing about the conversation leaves the
    /// machine. It also waits for the server: an assistant that answers "connection refused" for the
    /// first minute is worse than one that arrives a moment later.
    /// </remarks>
    /// <param name="builder">The AspireUI resource.</param>
    /// <param name="server">The model server. Any resource with an HTTP endpoint.</param>
    /// <param name="model">
    /// Model name as that server knows it — <c>llama3.2</c> for Ollama, <c>qwen3-8b</c> for LocalAI.
    /// </param>
    /// <param name="apiKey">Sent as a bearer token. Most local servers ignore it.</param>
    /// <param name="apiPath">
    /// The version segment, if the server does not serve the API under <see cref="DefaultApiPath"/>.
    /// </param>
    /// <param name="endpointName">Endpoint of the server to use, when it has several.</param>
    /// <param name="label">What the UI calls the provider. Defaults to the server's resource name.</param>
    public static IResourceBuilder<AspireUIResource> WithAssistant<TServer>(
        this IResourceBuilder<AspireUIResource> builder, IResourceBuilder<TServer> server,
        string model, string? apiKey = null, string? apiPath = null, string? endpointName = null,
        string? label = null)
        where TServer : IResource, IResourceWithEndpoints
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(server);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        var endpoint = endpointName is { Length: > 0 }
            ? server.GetEndpoint(endpointName)
            : PrimaryEndpointOf(server);
        var suffix = Normalise(apiPath ?? DefaultApiPath);

        builder.WithEnvironment("ASPIREUI_AI_BASE_URL", ReferenceExpression.Create($"{endpoint}{suffix}"));
        if (apiKey is { Length: > 0 }) builder.WithEnvironment("ASPIREUI_AI_API_KEY", apiKey);

        // A model server that is still pulling its weights is not an error, only slow to start.
        // Re-created as an untyped builder because WaitFor takes IResourceBuilder<IResource>, and
        // IResourceBuilder<T> is invariant.
        var dependency = builder.ApplicationBuilder.CreateResourceBuilder<IResource>(server.Resource);

        return builder.WaitFor(dependency).WithModel(model, label ?? server.Resource.Name);
    }

    /// <summary>
    /// A model server in this stack whose key comes from an Aspire parameter — a hosted gateway
    /// somebody put in front of it, say.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithAssistant<TServer>(
        this IResourceBuilder<AspireUIResource> builder, IResourceBuilder<TServer> server,
        string model, IResourceBuilder<ParameterResource> apiKey, string? apiPath = null,
        string? endpointName = null, string? label = null)
        where TServer : IResource, IResourceWithEndpoints
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        builder.WithAssistant(server, model, apiKey: (string?)null, apiPath, endpointName, label);

        return builder.WithEnvironment("ASPIREUI_AI_API_KEY", apiKey);
    }

    /// <summary>
    /// The same as <see cref="WithAssistant{TServer}(IResourceBuilder{AspireUIResource}, IResourceBuilder{TServer}, string, string?, string?, string?, string?)"/>,
    /// named for the server most people reach for first. Use it with <c>builder.AddOllama(...)</c>
    /// from the CommunityToolkit, and give it a model Ollama has been told to pull.
    /// </summary>
    /// <example>
    /// <code>
    /// var ollama = builder.AddOllama("ollama").WithDataVolume();
    /// ollama.AddModel("llama3.2");
    ///
    /// builder.AddAspireUI()
    ///     .WithAdminUser("admin", "change-me")
    ///     .WithOllamaAssistant(ollama, "llama3.2");
    /// </code>
    /// </example>
    public static IResourceBuilder<AspireUIResource> WithOllamaAssistant<TServer>(
        this IResourceBuilder<AspireUIResource> builder, IResourceBuilder<TServer> ollama,
        string model = DefaultOllamaModel, string? endpointName = null)
        where TServer : IResource, IResourceWithEndpoints =>
        builder.WithAssistant(ollama, model, apiKey: (string?)null, apiPath: null,
            endpointName: endpointName, label: "Ollama");

    /// <summary>
    /// The same for a LocalAI instance, which serves the OpenAI API at the same path. Pass the model
    /// as LocalAI names it.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithLocalAiAssistant<TServer>(
        this IResourceBuilder<AspireUIResource> builder, IResourceBuilder<TServer> localAi,
        string model, string? apiKey = null, string? endpointName = null)
        where TServer : IResource, IResourceWithEndpoints =>
        builder.WithAssistant(localAi, model, apiKey, apiPath: null, endpointName: endpointName,
            label: "LocalAI");

    // --- An agent CLI on the AspireUI host ------------------------------------------------------

    /// <summary>
    /// Uses an agent CLI installed on the machine AspireUI runs on instead of an endpoint:
    /// <c>claude</c>, <c>gemini</c>, <c>ollama</c>, <c>llm</c> or <c>codex</c>.
    /// <para>
    /// Cheap to set up and it keeps everything local, but a CLI takes a prompt and returns text, so
    /// the assistant can answer questions and not operate the instance. The container image does not
    /// ship these tools: mount or install the one you name.
    /// </para>
    /// </summary>
    /// <param name="builder">The AspireUI resource.</param>
    /// <param name="tool">One of <see cref="CliTools"/>.</param>
    /// <param name="model">Needed by <c>ollama</c> and <c>llm</c>; ignored by the others.</param>
    public static IResourceBuilder<AspireUIResource> WithCliAssistant(
        this IResourceBuilder<AspireUIResource> builder, string tool, string? model = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(tool);
        var chosen = tool.Trim().ToLowerInvariant();
        if (!CliTools.Contains(chosen))
            throw new ArgumentException($"'{tool}' is not one of {string.Join(", ", CliTools)}", nameof(tool));

        builder.Resource.AssistantModel = string.IsNullOrWhiteSpace(model) ? null : model!.Trim();
        builder.WithSetting("AiKind", "cli").WithSetting("AiCliTool", chosen);
        if (builder.Resource.AssistantModel is { } m) builder.WithSetting("AiModel", m);
        return builder.WithSetting("AiProviderLabel", chosen + " (cli)");
    }

    private static IResourceBuilder<AspireUIResource> WithModel(
        this IResourceBuilder<AspireUIResource> builder, string model, string? label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        builder.Resource.AssistantModel = model.Trim();
        builder.WithEnvironment("ASPIREUI_AI_MODEL", builder.Resource.AssistantModel);
        // An assistant reached over http is the one that can use tools; saying so here means a stack
        // that used to point at a CLI does not keep that setting by accident.
        builder.WithSetting("AiKind", "http");
        if (label is { Length: > 0 }) builder.WithSetting("AiProviderLabel", label.Trim());
        return builder;
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
                $"'{server.Resource.Name}' has no endpoint to talk to; give AspireUI the url with " +
                "WithAssistant(endpoint, model) instead.");

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
