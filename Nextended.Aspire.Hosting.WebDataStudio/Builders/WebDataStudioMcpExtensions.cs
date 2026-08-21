using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// Turns the studio into an MCP server, so an agent — Claude Code, Claude Desktop, VS Code, Cursor,
/// anything that speaks MCP — can reach the databases of this stack through it.
/// </summary>
/// <remarks>
/// The agent gets the same deal a person gets: a read-only connection stays read-only, a masked
/// column stays masked, and a write is previewed before it runs. Off unless one of these calls is
/// made, because an endpoint that hands an agent every database in the stack is not something to
/// switch on by accident.
/// </remarks>
public static class WebDataStudioMcpExtensions
{
    /// <summary>Path the studio serves MCP on when nothing else is asked for.</summary>
    public const string DefaultPath = "/mcp";

    /// <summary>
    /// Serves MCP from the studio, at <paramref name="path"/>, guarded by
    /// <paramref name="apiKey"/> when one is given.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="path">Path for the endpoint. Defaults to <see cref="DefaultPath"/>.</param>
    /// <param name="apiKey">
    /// Sent by the client as <c>Authorization: Bearer</c>. A studio that has accounts
    /// (<c>WithLogin</c>/<c>WithUser</c>) <b>requires</b> one: the MCP endpoint sits outside the
    /// login screen, and without a key of its own it would be a way past it.
    /// </param>
    /// <param name="allowWrite">
    /// Lets the agent change data, through a preview and its hash — never in one step. Off by
    /// default.
    /// </param>
    public static IResourceBuilder<WebDataStudioResource> WithMcpEndpoint(
        this IResourceBuilder<WebDataStudioResource> builder, string? path = null,
        string? apiKey = null, bool allowWrite = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Resource.McpPath = Normalise(path);
        builder.Resource.McpAllowsWrite = allowWrite;
        builder.Resource.McpHasKey = apiKey is { Length: > 0 };

        builder
            .WithEnvironment("WDS_MCP_ENABLED", "true")
            .WithEnvironment("WDS_MCP_PATH", builder.Resource.McpPath);

        if (allowWrite) builder.WithEnvironment("WDS_MCP_ALLOW_WRITE", "true");
        if (builder.Resource.McpHasKey) builder.WithEnvironment("WDS_MCP_KEY", apiKey!);

        return builder.WarnAboutTheMissingKey();
    }

    /// <summary>
    /// The same, with the key from an Aspire parameter so it stays out of the manifest and out of
    /// source control.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithMcpEndpoint(
        this IResourceBuilder<WebDataStudioResource> builder,
        IResourceBuilder<ParameterResource> apiKey, string? path = null, bool allowWrite = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(apiKey);

        builder.Resource.McpPath = Normalise(path);
        builder.Resource.McpAllowsWrite = allowWrite;
        builder.Resource.McpHasKey = true;

        builder
            .WithEnvironment("WDS_MCP_ENABLED", "true")
            .WithEnvironment("WDS_MCP_PATH", builder.Resource.McpPath)
            .WithEnvironment("WDS_MCP_KEY", apiKey);

        if (allowWrite) builder.WithEnvironment("WDS_MCP_ALLOW_WRITE", "true");

        return builder;
    }

    /// <summary>
    /// The same, written the way it reads in an app host: the path first, then the parameter that
    /// holds the key.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithMcpEndpoint(
        this IResourceBuilder<WebDataStudioResource> builder, string? path,
        IResourceBuilder<ParameterResource> apiKey, bool allowWrite = false) =>
        builder.WithMcpEndpoint(apiKey, path, allowWrite);

    /// <summary>
    /// Narrows the endpoint to these tools. A whitelist: a tool added in a later version of the
    /// studio does not appear on an endpoint somebody deliberately narrowed.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="tools">
    /// Tool names — see <see cref="WebDataStudioMcpTools"/> for the ones that exist. Nothing given
    /// means every tool the endpoint has.
    /// </param>
    public static IResourceBuilder<WebDataStudioResource> WithMcpTools(
        this IResourceBuilder<WebDataStudioResource> builder, params string[] tools)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var named = (tools ?? [])
            .Where(tool => !string.IsNullOrWhiteSpace(tool))
            .Select(tool => tool.Trim())
            .ToList();

        if (named.Count == 0) return builder;

        foreach (var tool in named) builder.Resource.McpToolList.Add(tool);

        return builder.WithEnvironment(context =>
            context.EnvironmentVariables["WDS_MCP_TOOLS"] =
                string.Join(",", builder.Resource.McpToolList));
    }

    /// <summary>
    /// Keeps the studio's own assistant from using the MCP tools. Without this it uses them
    /// whenever both the assistant and the MCP endpoint are configured — the same registry and the
    /// same rules, so the answer comes from the database rather than from a guess.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithoutAssistantTools(
        this IResourceBuilder<WebDataStudioResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithEnvironment("WDS_ASSIST_TOOLS", "false");
    }

    /// <summary>
    /// A studio with accounts refuses to serve MCP without a key of its own — the endpoint sits
    /// outside the login screen, so without one it would be a way past it. The studio says so in its
    /// log; this says it in the app host, where it can still be fixed before anything starts.
    /// </summary>
    private static IResourceBuilder<WebDataStudioResource> WarnAboutTheMissingKey(
        this IResourceBuilder<WebDataStudioResource> builder)
    {
        // Registered once, and it reads the resource at start — so the order of the calls in the
        // app host does not matter: WithLogin after WithMcpEndpoint is still caught.
        if (!builder.Resource.WarnedAboutMcpKey)
        {
            builder.Resource.WarnedAboutMcpKey = true;

            builder.ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>((_, _) =>
            {
                if (MissingKeyWarning(builder.Resource) is { } warning)
                    Console.Error.WriteLine(warning);

                return Task.CompletedTask;
            });
        }

        return builder;
    }

    /// <summary>
    /// What is wrong with this studio's MCP configuration, or null when nothing is. The app host
    /// prints it before anything starts; it is public so a test or a health check can ask the same
    /// question without starting an application.
    /// </summary>
    public static string? MissingKeyWarning(WebDataStudioResource resource) =>
        resource.McpPath is not null && !resource.McpHasKey && resource.Username is not null
            ? $"WebDataStudio '{resource.Name}' has a login and an MCP endpoint without a key, so " +
              "the studio will refuse to serve MCP: an agent has no cookie, and without a key of " +
              "its own the endpoint would be a way past the login. Pass one to WithMcpEndpoint " +
              "(a parameter keeps it out of source control)."
            : null;

    private static string Normalise(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return DefaultPath;

        var trimmed = path.Trim().TrimEnd('/');
        if (trimmed.Length == 0) return DefaultPath;

        return trimmed.StartsWith('/') ? trimmed : "/" + trimmed;
    }
}
