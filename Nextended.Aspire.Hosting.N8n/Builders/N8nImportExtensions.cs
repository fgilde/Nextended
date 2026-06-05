using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.N8n.Resources;
using static Nextended.Aspire.Hosting.N8n.Helpers.N8nLogger;

namespace Nextended.Aspire.Hosting.N8n.Builders;

/// <summary>
/// Provides extension methods to import workflows and credentials into n8n on startup.
/// A one-shot init container runs the n8n CLI import commands before the main instance starts.
/// Intended for local development and integration tests (uses bind mounts); skipped in publish mode.
/// </summary>
public static class N8nImportExtensions
{
    internal const string WorkflowsMountTarget = "/import/workflows";
    internal const string CredentialsMountTarget = "/import/credentials";

    /// <summary>
    /// Imports all workflow JSON files from the given directory on startup
    /// (<c>n8n import:workflow --separate</c>).
    /// </summary>
    /// <param name="builder">The n8n resource builder.</param>
    /// <param name="workflowsPath">Absolute path to the directory containing workflow JSON files.</param>
    public static IResourceBuilder<N8nResource> WithImportWorkflows(
        this IResourceBuilder<N8nResource> builder, string workflowsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowsPath);

        if (!Directory.Exists(workflowsPath))
        {
            LogWarning($"Workflow import directory not found: {workflowsPath}");
            return builder;
        }

        builder.Resource.ImportWorkflowsPath = workflowsPath;
        EnsureImportContainer(builder)?.WithBindMount(workflowsPath, WorkflowsMountTarget, isReadOnly: true);
        return builder;
    }

    /// <summary>
    /// Imports all credential JSON files from the given directory on startup
    /// (<c>n8n import:credentials --separate</c>). Requires the matching encryption key.
    /// </summary>
    /// <param name="builder">The n8n resource builder.</param>
    /// <param name="credentialsPath">Absolute path to the directory containing credential JSON files.</param>
    public static IResourceBuilder<N8nResource> WithImportCredentials(
        this IResourceBuilder<N8nResource> builder, string credentialsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialsPath);

        if (!Directory.Exists(credentialsPath))
        {
            LogWarning($"Credential import directory not found: {credentialsPath}");
            return builder;
        }

        builder.Resource.ImportCredentialsPath = credentialsPath;
        EnsureImportContainer(builder)?.WithBindMount(credentialsPath, CredentialsMountTarget, isReadOnly: true);
        return builder;
    }

    /// <summary>
    /// Creates the one-shot import init container on first use and wires the main instance to
    /// wait for its completion. Returns <c>null</c> in publish mode (bind mounts are unavailable there).
    /// </summary>
    private static IResourceBuilder<ContainerResource>? EnsureImportContainer(IResourceBuilder<N8nResource> builder)
    {
        var resource = builder.Resource;
        var app = resource.AppBuilder!;

        if (app.ExecutionContext.IsPublishMode)
        {
            LogWarning("Workflow/credential import uses local bind mounts and is skipped in publish mode.");
            return null;
        }

        if (resource.ImportContainer is not null)
            return resource.ImportContainer;

        var importContainer = app.AddContainer($"{resource.Name}-import", resource.Image, resource.ImageTag)
            .WithEnvironment(ctx => N8nBuilderExtensions.ApplyEnvironment(ctx, resource, isPublishMode: false))
            .WithEntrypoint("/bin/sh")
            .WithArgs(ctx =>
            {
                var commands = new List<string> { "set -e" };
                if (resource.ImportWorkflowsPath is not null)
                    commands.Add($"echo '[n8n-import] importing workflows...' && n8n import:workflow --separate --input={WorkflowsMountTarget}");
                if (resource.ImportCredentialsPath is not null)
                    commands.Add($"echo '[n8n-import] importing credentials...' && n8n import:credentials --separate --input={CredentialsMountTarget}");
                commands.Add("echo '[n8n-import] done'");

                ctx.Args.Add("-c");
                ctx.Args.Add(string.Join(" && ", commands));
            });

        if (resource.Database is { } database)
            importContainer.WaitFor(database);

        importContainer.WithParentRelationship(resource);
        resource.ImportContainer = importContainer;

        // Main instance (and transitively the workers) must wait for the import to finish.
        builder.WaitForCompletion(importContainer);

        return importContainer;
    }
}
