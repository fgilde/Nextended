using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

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

        builder
            .WithEnvironment("WDS_MCP_ENABLED", "true")
            .WithEnvironment("WDS_MCP_PATH", builder.Resource.McpPath);

        if (allowWrite) builder.WithEnvironment("WDS_MCP_ALLOW_WRITE", "true");
        if (apiKey is { Length: > 0 }) builder.WithEnvironment("WDS_MCP_KEY", apiKey);

        return builder;
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

        builder
            .WithEnvironment("WDS_MCP_ENABLED", "true")
            .WithEnvironment("WDS_MCP_PATH", builder.Resource.McpPath)
            .WithEnvironment("WDS_MCP_KEY", apiKey);

        if (allowWrite) builder.WithEnvironment("WDS_MCP_ALLOW_WRITE", "true");

        return builder;
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

    private static string Normalise(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return DefaultPath;

        var trimmed = path.Trim().TrimEnd('/');
        if (trimmed.Length == 0) return DefaultPath;

        return trimmed.StartsWith('/') ? trimmed : "/" + trimmed;
    }
}
