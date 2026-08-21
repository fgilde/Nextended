using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Nextended.Aspire.Hosting.WebDataStudio.Tests;

/// The MCP endpoint and the hosted providers, as the app host configures them.
public class WebDataStudioMcpTests
{
    private static async Task<Dictionary<string, string>> EnvOf(IResource resource)
    {
        var context = new EnvironmentCallbackContext(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));

        foreach (var annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
            await annotation.Callback(context);

        return context.EnvironmentVariables
            .Where(pair => pair.Value is string)
            .ToDictionary(pair => pair.Key, pair => (string)pair.Value);
    }

    private static async Task<IReadOnlyCollection<string>> EnvKeysOf(IResource resource)
    {
        var context = new EnvironmentCallbackContext(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));

        foreach (var annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
            await annotation.Callback(context);

        return context.EnvironmentVariables.Keys;
    }

    private static IResourceBuilder<WebDataStudioResource> Add() =>
        DistributedApplication.CreateBuilder().AddWebDataStudio();

    [Fact]
    public async Task WithoutTheCall_TheStudioIsNotAnMcpServer()
    {
        var studio = Add();

        var keys = await EnvKeysOf(studio.Resource);

        Assert.DoesNotContain("WDS_MCP_ENABLED", keys);
        Assert.Null(studio.Resource.McpPath);
    }

    [Fact]
    public async Task TheEndpointIsOnAndReadOnlyByDefault()
    {
        var studio = Add().WithMcpEndpoint();

        var env = await EnvOf(studio.Resource);

        Assert.Equal("true", env["WDS_MCP_ENABLED"]);
        Assert.Equal("/mcp", env["WDS_MCP_PATH"]);
        Assert.DoesNotContain("WDS_MCP_ALLOW_WRITE", env.Keys);
        Assert.DoesNotContain("WDS_MCP_KEY", env.Keys);
        Assert.Equal("/mcp", studio.Resource.McpPath);
        Assert.False(studio.Resource.McpAllowsWrite);
    }

    [Fact]
    public async Task APathWithoutASlashStillWorks()
    {
        var env = await EnvOf(Add().WithMcpEndpoint("agents/db").Resource);

        Assert.Equal("/agents/db", env["WDS_MCP_PATH"]);
    }

    [Fact]
    public async Task AKeyAndWritingAreWrittenWhenAskedFor()
    {
        var studio = Add().WithMcpEndpoint("/mcp", "s3cret", allowWrite: true);

        var env = await EnvOf(studio.Resource);

        Assert.Equal("s3cret", env["WDS_MCP_KEY"]);
        Assert.Equal("true", env["WDS_MCP_ALLOW_WRITE"]);
        Assert.True(studio.Resource.McpAllowsWrite);
    }

    [Fact]
    public async Task AKeyFromAParameterStaysAReference()
    {
        var builder = DistributedApplication.CreateBuilder();
        var key = builder.AddParameter("mcp-key", secret: true);
        var studio = builder.AddWebDataStudio().WithMcpEndpoint(key);

        var keys = await EnvKeysOf(studio.Resource);
        var plain = await EnvOf(studio.Resource);

        Assert.Contains("WDS_MCP_KEY", keys);
        Assert.DoesNotContain("WDS_MCP_KEY", plain.Keys);
    }

    [Fact]
    public async Task TheAssistantsToolsCanBeTurnedOff()
    {
        var env = await EnvOf(Add().WithMcpEndpoint().WithoutAssistantTools().Resource);

        Assert.Equal("false", env["WDS_ASSIST_TOOLS"]);
    }

    // --- the hosted providers ---------------------------------------------------------------

    [Fact]
    public async Task ClaudeGetsAnthropicsOpenAiCompatibleEndpoint()
    {
        var env = await EnvOf(Add().WithClaudeAssistant("sk-ant-test").Resource);

        Assert.Equal(WebDataStudioProviderExtensions.ClaudeEndpoint, env["WDS_ASSIST_ENDPOINT"]);
        Assert.Equal("sk-ant-test", env["WDS_ASSIST_KEY"]);
        Assert.StartsWith("claude-", env["WDS_ASSIST_MODEL"]);
    }

    [Fact]
    public async Task ChatGptAndOpenRouterCarryTheirOwnUrls()
    {
        var chatgpt = await EnvOf(Add().WithChatGptAssistant("sk-test", "gpt-4o").Resource);
        var router = await EnvOf(Add().WithOpenRouterAssistant("sk-or-test").Resource);

        Assert.Equal(WebDataStudioProviderExtensions.OpenAiEndpoint, chatgpt["WDS_ASSIST_ENDPOINT"]);
        Assert.Equal("gpt-4o", chatgpt["WDS_ASSIST_MODEL"]);
        Assert.Equal(WebDataStudioProviderExtensions.OpenRouterEndpoint, router["WDS_ASSIST_ENDPOINT"]);
        // OpenRouter names the provider inside the model.
        Assert.Contains("/", router["WDS_ASSIST_MODEL"]);
    }

    [Theory]
    [InlineData("groq")]
    [InlineData("mistral")]
    [InlineData("deepseek")]
    [InlineData("gemini")]
    public async Task EveryProviderWritesAnEndpointAKeyAndAModel(string provider)
    {
        var studio = Add();
        studio = provider switch
        {
            "groq" => studio.WithGroqAssistant("key"),
            "mistral" => studio.WithMistralAssistant("key"),
            "deepseek" => studio.WithDeepSeekAssistant("key"),
            _ => studio.WithGeminiAssistant("key"),
        };

        var env = await EnvOf(studio.Resource);

        Assert.StartsWith("https://", env["WDS_ASSIST_ENDPOINT"]);
        Assert.EndsWith("chat/completions", env["WDS_ASSIST_ENDPOINT"]);
        Assert.Equal("key", env["WDS_ASSIST_KEY"]);
        Assert.NotEmpty(env["WDS_ASSIST_MODEL"]);
    }

    /// Azure puts the deployment in the path and the api-version in the query, which is exactly the
    /// URL people get wrong by hand.
    [Fact]
    public async Task AzureBuildsItsDeploymentUrl()
    {
        var env = await EnvOf(Add()
            .WithAzureOpenAiAssistant("my-openai", "gpt4o-deploy", "key").Resource);

        Assert.Equal(
            "https://my-openai.openai.azure.com/openai/deployments/gpt4o-deploy/chat/completions" +
            "?api-version=2024-10-21",
            env["WDS_ASSIST_ENDPOINT"]);
        Assert.Equal("gpt4o-deploy", env["WDS_ASSIST_MODEL"]);
    }

    [Fact]
    public async Task AProviderKeyCanComeFromAParameter()
    {
        var builder = DistributedApplication.CreateBuilder();
        var key = builder.AddParameter("anthropic-key", secret: true);
        var studio = builder.AddWebDataStudio().WithClaudeAssistant(key);

        var keys = await EnvKeysOf(studio.Resource);
        var plain = await EnvOf(studio.Resource);

        Assert.Contains("WDS_ASSIST_KEY", keys);
        Assert.DoesNotContain("WDS_ASSIST_KEY", plain.Keys);
        Assert.Equal(WebDataStudioProviderExtensions.ClaudeEndpoint, plain["WDS_ASSIST_ENDPOINT"]);
    }
}
