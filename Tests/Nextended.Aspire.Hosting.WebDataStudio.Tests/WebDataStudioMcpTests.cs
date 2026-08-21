using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
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

    /// A stand-in for Ollama, LocalAI or anything else with an HTTP endpoint.
    private sealed class FakeModelServer(string name) : ContainerResource(name), IResourceWithEndpoints;

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

    /// The app host warns before anything starts: the studio would refuse to serve MCP, and a
    /// warning here can still be acted on. The rule is a pure function, so it is tested as one.
    [Fact]
    public void ALoginWithoutAnMcpKeyIsWarnedAbout()
    {
        var studio = Add().WithMcpEndpoint().WithLogin("ada", "one");

        var warning = WebDataStudioMcpExtensions.MissingKeyWarning(studio.Resource);

        Assert.NotNull(warning);
        Assert.Contains("refuse to serve MCP", warning);
        Assert.False(studio.Resource.McpHasKey);
    }

    /// The order of the calls must not matter: WithLogin can come first.
    [Fact]
    public void TheOrderOfTheCallsDoesNotMatter()
    {
        var studio = Add().WithLogin("ada", "one").WithMcpEndpoint();

        Assert.NotNull(WebDataStudioMcpExtensions.MissingKeyWarning(studio.Resource));
    }

    [Fact]
    public void WithAKeyThereIsNoWarning()
    {
        var studio = Add().WithMcpEndpoint("/mcp", "k").WithLogin("ada", "one");

        Assert.Null(WebDataStudioMcpExtensions.MissingKeyWarning(studio.Resource));
    }

    [Fact]
    public void WithoutALoginThereIsNothingToWarnAbout()
    {
        var studio = Add().WithMcpEndpoint();

        Assert.Null(WebDataStudioMcpExtensions.MissingKeyWarning(studio.Resource));
    }

    [Fact]
    public async Task APathAndAParameterKeyReadInThatOrder()
    {
        var builder = DistributedApplication.CreateBuilder();
        var key = builder.AddParameter("mcp-key", secret: true);
        var studio = builder.AddWebDataStudio().WithMcpEndpoint("/agents", key, allowWrite: true);

        var keys = await EnvKeysOf(studio.Resource);
        var plain = await EnvOf(studio.Resource);

        Assert.Equal("/agents", plain["WDS_MCP_PATH"]);
        Assert.Equal("true", plain["WDS_MCP_ALLOW_WRITE"]);
        Assert.Contains("WDS_MCP_KEY", keys);
        Assert.DoesNotContain("WDS_MCP_KEY", plain.Keys);
        Assert.True(studio.Resource.McpHasKey);
    }

    [Fact]
    public async Task AModelServerCanTakeAParameterKey()
    {
        var builder = DistributedApplication.CreateBuilder();
        var key = builder.AddParameter("gateway-key", secret: true);
        var server = builder.AddResource(new FakeModelServer("vllm")).WithHttpEndpoint(targetPort: 8000);

        var studio = builder.AddWebDataStudio().WithAssistant(server, "mixtral", key);

        var keys = await EnvKeysOf(studio.Resource);
        var plain = await EnvOf(studio.Resource);

        Assert.Contains("WDS_ASSIST_KEY", keys);
        Assert.DoesNotContain("WDS_ASSIST_KEY", plain.Keys);
        Assert.Equal("mixtral", plain["WDS_ASSIST_MODEL"]);
    }

    [Fact]
    public async Task AzureCanTakeAParameterKey()
    {
        var builder = DistributedApplication.CreateBuilder();
        var key = builder.AddParameter("azure-key", secret: true);
        var studio = builder.AddWebDataStudio()
            .WithAzureOpenAiAssistant("my-openai", "gpt4o-deploy", key);

        var keys = await EnvKeysOf(studio.Resource);
        var plain = await EnvOf(studio.Resource);

        Assert.Contains("WDS_ASSIST_KEY", keys);
        Assert.Contains("api-version=", plain["WDS_ASSIST_ENDPOINT"]);
    }

    [Fact]
    public async Task TheAssistantsToolsCanBeTurnedOff()
    {
        var env = await EnvOf(Add().WithMcpEndpoint().WithoutAssistantTools().Resource);

        Assert.Equal("false", env["WDS_ASSIST_TOOLS"]);
    }

    [Fact]
    public async Task TheToolsCanBeNarrowed()
    {
        var studio = Add().WithMcpEndpoint().WithMcpTools(WebDataStudioMcpTools.SchemaOnly);

        var env = await EnvOf(studio.Resource);

        Assert.Contains("describe_object", env["WDS_MCP_TOOLS"]);
        Assert.DoesNotContain("run_query", env["WDS_MCP_TOOLS"]);
        Assert.Contains("list_tables", studio.Resource.McpTools);
    }

    [Fact]
    public async Task NarrowingTwiceAddsUp()
    {
        var env = await EnvOf(Add()
            .WithMcpEndpoint()
            .WithMcpTools(WebDataStudioMcpTools.ListTables)
            .WithMcpTools(WebDataStudioMcpTools.RunQuery)
            .Resource);

        Assert.Equal("list_tables,run_query", env["WDS_MCP_TOOLS"]);
    }

    [Fact]
    public async Task NamingNoToolsLeavesThemAll()
    {
        var keys = await EnvKeysOf(Add().WithMcpEndpoint().WithMcpTools().Resource);

        Assert.DoesNotContain("WDS_MCP_TOOLS", keys);
    }

    // --- alerts -----------------------------------------------------------------------------

    [Fact]
    public async Task TheAlertWebhookAndItsSettings()
    {
        var env = await EnvOf(Add()
            .WithAlertWebhook("https://hooks.example.com/x", TimeSpan.FromHours(2), "info", "SHOP")
            .Resource);

        Assert.Equal("https://hooks.example.com/x", env["WDS_ALERT_WEBHOOK"]);
        Assert.Equal("120", env["WDS_ALERT_INTERVAL_MINUTES"]);
        Assert.Equal("info", env["WDS_ALERT_MIN_SEVERITY"]);
        Assert.Equal("SHOP", env["WDS_ALERT_CONNECTIONS"]);
    }

    [Fact]
    public async Task WithoutTheCallNothingIsWatched()
    {
        var keys = await EnvKeysOf(Add().Resource);

        Assert.DoesNotContain("WDS_ALERT_WEBHOOK", keys);
    }

    [Fact]
    public void ANonsenseSeverityOrIntervalThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Add().WithAlertWebhook("https://x", minSeverity: "shouting"));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Add().WithAlertWebhook("https://x", interval: TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task TheWebhookCanComeFromAParameter()
    {
        var builder = DistributedApplication.CreateBuilder();
        var hook = builder.AddParameter("slack-webhook", secret: true);
        var studio = builder.AddWebDataStudio().WithAlertWebhook(hook);

        var keys = await EnvKeysOf(studio.Resource);
        var plain = await EnvOf(studio.Resource);

        Assert.Contains("WDS_ALERT_WEBHOOK", keys);
        Assert.DoesNotContain("WDS_ALERT_WEBHOOK", plain.Keys);
    }

    [Fact]
    public async Task SchemaSnapshotsGoToTheDataVolumeByDefault()
    {
        var studio = Add().WithSchemaSnapshots();

        var env = await EnvOf(studio.Resource);

        Assert.Equal("/data/snapshots", env["WDS_SCHEMA_SNAPSHOT_DIR"]);
        Assert.Equal("/data/snapshots", studio.Resource.SchemaSnapshotPath);
    }

    [Fact]
    public async Task WithoutTheCallNoSnapshotsAreWritten()
    {
        var keys = await EnvKeysOf(Add().Resource);

        Assert.DoesNotContain("WDS_SCHEMA_SNAPSHOT_DIR", keys);
        Assert.Null(Add().Resource.SchemaSnapshotPath);
    }

    // --- files that ship with the stack -----------------------------------------------------

    [Fact]
    public async Task SavedQueriesAreMountedReadOnlyAndPointedAt()
    {
        var studio = Add().WithSavedQueriesFromDirectory("./queries");

        var env = await EnvOf(studio.Resource);
        var mount = Assert.Single(studio.Resource.Annotations.OfType<ContainerMountAnnotation>(),
            m => m.Target == "/data/queries");

        Assert.Equal("/data/queries", env["WDS_SAVED_QUERIES_DIR"]);
        Assert.True(mount.IsReadOnly);
        Assert.Equal("./queries", studio.Resource.SavedQueriesPath);
    }

    [Fact]
    public async Task ASeedFolderAndASeedFileMountDifferently()
    {
        var folder = await EnvOf(Add().WithSeedScript("./seed").Resource);
        var file = await EnvOf(Add().WithSeedScript("./seed/shop.sql").Resource);

        Assert.Equal("/data/seed", folder["WDS_SEED_SQL"]);
        Assert.Equal("/data/seed/seed.sql", file["WDS_SEED_SQL"]);
    }

    [Fact]
    public async Task WithoutTheCallsNeitherIsSet()
    {
        var keys = await EnvKeysOf(Add().Resource);

        Assert.DoesNotContain("WDS_SAVED_QUERIES_DIR", keys);
        Assert.DoesNotContain("WDS_SEED_SQL", keys);
    }

    // --- scheduled queries ------------------------------------------------------------------

    [Fact]
    public async Task ScheduledQueriesBecomeAMountedFile()
    {
        var studio = Add().WithScheduledQueries(
            new ScheduledStudioQuery("nightly", "SHOP", "SELECT 1", DailyAtUtc: "03:00"),
            new ScheduledStudioQuery("often", "SHOP", "SELECT 2", EveryMinutes: 15, Format: "json"));

        var env = await EnvOf(studio.Resource);
        var mount = Assert.Single(studio.Resource.Annotations.OfType<ContainerMountAnnotation>(),
            m => m.Target == "/data/schedule/schedule.json");

        Assert.Equal("/data/schedule/schedule.json", env["WDS_SCHEDULE_FILE"]);
        Assert.Equal("/data/exports", env["WDS_SCHEDULE_OUTPUT_DIR"]);
        Assert.True(mount.IsReadOnly);

        // The file the studio will read really holds both jobs.
        var written = await File.ReadAllTextAsync(mount.Source!);
        Assert.Contains("nightly", written);
        Assert.Contains("\"everyMinutes\": 15", written);
        Assert.Equal(2, studio.Resource.Schedule.Count);
    }

    [Fact]
    public void AJobThatWouldNeverRunThrows()
    {
        Assert.Throws<ArgumentException>(() =>
            Add().WithScheduledQueries(new ScheduledStudioQuery("nowhen", "SHOP", "SELECT 1")));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Add().WithScheduledQueries(
                new ScheduledStudioQuery("too-often", "SHOP", "SELECT 1", EveryMinutes: 0)));
    }

    [Fact]
    public async Task WithoutJobsNothingIsScheduled()
    {
        var keys = await EnvKeysOf(Add().WithScheduledQueries().Resource);

        Assert.DoesNotContain("WDS_SCHEDULE_FILE", keys);
    }

    // --- masking ----------------------------------------------------------------------------

    [Fact]
    public async Task MaskedAndUnmaskedColumnsBecomeTheirLists()
    {
        var studio = Add()
            .WithMaskedColumns("ssn", "iban")
            .WithUnmaskedColumns("token_type");

        var env = await EnvOf(studio.Resource);

        Assert.Equal("iban,ssn", env["WDS_MASK_EXTRA"]);
        Assert.Equal("token_type", env["WDS_MASK_NEVER"]);
        Assert.Equal(["iban", "ssn"], studio.Resource.MaskedColumns);
    }

    /// Chaining adds up: two calls are two lists joined, not the second one winning.
    [Fact]
    public async Task ChainingAddsToTheList()
    {
        var env = await EnvOf(Add()
            .WithMaskedColumns("ssn")
            .WithMaskedColumns("iban", "  ")
            .Resource);

        Assert.Equal("iban,ssn", env["WDS_MASK_EXTRA"]);
    }

    [Fact]
    public async Task TheHeuristicCanBeTurnedOff()
    {
        var env = await EnvOf(Add().WithoutColumnMasking().WithMaskedColumns("ssn").Resource);

        Assert.Equal("false", env["WDS_MASK_DEFAULT"]);
        Assert.Equal("ssn", env["WDS_MASK_EXTRA"]);
    }

    [Fact]
    public async Task WithoutTheCallsNothingIsSet()
    {
        var keys = await EnvKeysOf(Add().Resource);

        Assert.DoesNotContain("WDS_MASK_EXTRA", keys);
        Assert.DoesNotContain("WDS_MASK_DEFAULT", keys);
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
