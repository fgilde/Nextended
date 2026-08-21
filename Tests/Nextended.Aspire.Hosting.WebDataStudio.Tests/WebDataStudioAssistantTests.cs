using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Nextended.Aspire.Hosting.WebDataStudio.Tests;

/// The optional assistance, as the app host configures it. Two things matter more than the wiring:
/// nothing is set unless it was asked for, and a model server in the stack is reached over the
/// container network rather than over the internet.
public class WebDataStudioAssistantTests
{
    /// A stand-in for Ollama, LocalAI or anything else that speaks the OpenAI shape.
    private sealed class FakeModelServer(string name) : ContainerResource(name), IResourceWithEndpoints;

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

    [Fact]
    public async Task WithoutAssistant_TheFeatureDoesNotExist()
    {
        var studio = DistributedApplication.CreateBuilder().AddWebDataStudio();

        var keys = await EnvKeysOf(studio.Resource);

        Assert.DoesNotContain("WDS_ASSIST_ENDPOINT", keys);
        Assert.DoesNotContain("WDS_ASSIST_MODEL", keys);
        Assert.Null(studio.Resource.AssistantModel);
    }

    [Fact]
    public async Task AnEndpointAndAKeyAreWrittenAsGiven()
    {
        var studio = DistributedApplication.CreateBuilder().AddWebDataStudio()
            .WithAssistant("https://api.openai.com/v1/chat/completions", "gpt-4o", "sk-test");

        var env = await EnvOf(studio.Resource);

        Assert.Equal("https://api.openai.com/v1/chat/completions", env["WDS_ASSIST_ENDPOINT"]);
        Assert.Equal("gpt-4o", env["WDS_ASSIST_MODEL"]);
        Assert.Equal("sk-test", env["WDS_ASSIST_KEY"]);
        Assert.Equal("gpt-4o", studio.Resource.AssistantModel);
    }

    /// An endpoint that needs no key must not get an empty one: the studio would send
    /// `Authorization: Bearer ` and some servers reject that outright.
    [Fact]
    public async Task NoKeyMeansNoKeyVariable()
    {
        var studio = DistributedApplication.CreateBuilder().AddWebDataStudio()
            .WithAssistant("http://localhost:11434/v1/chat/completions");

        var keys = await EnvKeysOf(studio.Resource);

        Assert.DoesNotContain("WDS_ASSIST_KEY", keys);
        // A call without a model still names one, because the studio needs one.
        Assert.Equal(WebDataStudioAssistantExtensions.DefaultModel,
            (await EnvOf(studio.Resource))["WDS_ASSIST_MODEL"]);
    }

    [Fact]
    public async Task AKeyFromAParameterStaysAReference()
    {
        var builder = DistributedApplication.CreateBuilder();
        var key = builder.AddParameter("assist-key", secret: true);
        var studio = builder.AddWebDataStudio()
            .WithAssistant("https://api.openai.com/v1/chat/completions", key, "gpt-4o-mini");

        var keys = await EnvKeysOf(studio.Resource);
        var plain = await EnvOf(studio.Resource);

        Assert.Contains("WDS_ASSIST_KEY", keys);
        Assert.DoesNotContain("WDS_ASSIST_KEY", plain.Keys);
    }

    /// The point of the resource overload: the URL is the server's own endpoint, so the traffic
    /// stays inside the stack.
    [Fact]
    public async Task AModelServerInTheStackBecomesTheEndpoint()
    {
        var builder = DistributedApplication.CreateBuilder();
        var ollama = builder.AddResource(new FakeModelServer("ollama"))
            .WithHttpEndpoint(targetPort: 11434);

        var studio = builder.AddWebDataStudio().WithOllamaAssistant(ollama, "llama3.2");

        var keys = await EnvKeysOf(studio.Resource);
        var env = await EnvOf(studio.Resource);

        // The endpoint resolves at start, so it is a reference rather than a string…
        Assert.Contains("WDS_ASSIST_ENDPOINT", keys);
        Assert.DoesNotContain("WDS_ASSIST_ENDPOINT", env.Keys);
        // …and the model is plain.
        Assert.Equal("llama3.2", env["WDS_ASSIST_MODEL"]);
        Assert.Equal("llama3.2", studio.Resource.AssistantModel);
    }

    [Fact]
    public void TheStudioWaitsForTheModelServer()
    {
        var builder = DistributedApplication.CreateBuilder();
        var localai = builder.AddResource(new FakeModelServer("localai"))
            .WithHttpEndpoint(targetPort: 8080);

        var studio = builder.AddWebDataStudio().WithLocalAiAssistant(localai, "qwen3-8b");

        // A studio that starts first would answer "connection refused" for the first minute.
        var waits = studio.Resource.Annotations.OfType<WaitAnnotation>().ToList();
        Assert.Contains(waits, wait => wait.Resource.Name == "localai");
    }

    [Fact]
    public void AServerWithNoEndpointSaysWhatToDoInstead()
    {
        var builder = DistributedApplication.CreateBuilder();
        var server = builder.AddResource(new FakeModelServer("nowhere"));

        var error = Assert.Throws<InvalidOperationException>(() =>
            builder.AddWebDataStudio().WithAssistant(server, "llama3.2"));

        Assert.Contains("WithAssistant(endpoint)", error.Message);
    }

    [Fact]
    public async Task AnExpressionEndpointIsTakenAsItIs()
    {
        var builder = DistributedApplication.CreateBuilder();
        var studio = builder.AddWebDataStudio()
            .WithAssistant(ReferenceExpression.Create($"http://gateway/v1/chat/completions"),
                "mistral");

        var keys = await EnvKeysOf(studio.Resource);

        Assert.Contains("WDS_ASSIST_ENDPOINT", keys);
        Assert.Equal("mistral", (await EnvOf(studio.Resource))["WDS_ASSIST_MODEL"]);
    }
}
