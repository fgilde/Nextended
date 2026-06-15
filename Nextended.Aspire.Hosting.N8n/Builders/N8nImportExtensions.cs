using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.N8n.Resources;
using static Nextended.Aspire.Hosting.N8n.Helpers.N8nLogger;

namespace Nextended.Aspire.Hosting.N8n.Builders;

/// <summary>
/// Provides extension methods to seed workflows and credentials into n8n on startup.
/// A one-shot init container runs the n8n CLI import commands before the main instance starts.
/// Intended for local development and integration tests (uses bind mounts); skipped in publish mode.
/// </summary>
public static class N8nImportExtensions
{
    internal const string WorkflowsMountTarget = "/import/workflows";
    internal const string CredentialsMountTarget = "/import/credentials";

    #region Workflows

    /// <summary>
    /// Seeds n8n with the given workflow JSON <b>files</b>. Each file is imported as a workflow on startup
    /// (<c>n8n import:workflow --separate</c>).
    /// </summary>
    /// <param name="builder">The n8n resource builder.</param>
    /// <param name="workflowFiles">Absolute or relative paths to workflow JSON files.</param>
    public static IResourceBuilder<N8nResource> WithWorkflows(
        this IResourceBuilder<N8nResource> builder, params string[] workflowFiles)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (workflowFiles is null)
            return builder;

        foreach (var file in workflowFiles)
        {
            if (string.IsNullOrWhiteSpace(file))
                continue;
            if (!File.Exists(file))
            {
                LogWarning($"Workflow file not found: {file}");
                continue;
            }

            SeedWorkflow(builder, File.ReadAllText(file), Path.GetFileNameWithoutExtension(file));
        }

        return builder;
    }

    /// <summary>
    /// Seeds n8n with workflows provided as raw JSON <b>content</b> strings (e.g. embedded resources).
    /// </summary>
    /// <param name="builder">The n8n resource builder.</param>
    /// <param name="workflowContents">Workflow definitions as JSON strings.</param>
    public static IResourceBuilder<N8nResource> WithWorkflowContents(
        this IResourceBuilder<N8nResource> builder, params string[] workflowContents)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (workflowContents is null)
            return builder;

        foreach (var content in workflowContents)
        {
            if (string.IsNullOrWhiteSpace(content))
                continue;

            SeedWorkflow(builder, content, originalName: null);
        }

        return builder;
    }

    /// <summary>
    /// Seeds n8n with every <c>*.json</c> file found in the given directory.
    /// </summary>
    /// <param name="builder">The n8n resource builder.</param>
    /// <param name="directory">Absolute or relative path to a directory containing workflow JSON files.</param>
    public static IResourceBuilder<N8nResource> WithWorkflowsFromDirectory(
        this IResourceBuilder<N8nResource> builder, string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Directory.Exists(directory))
        {
            LogWarning($"Workflow directory not found: {directory}");
            return builder;
        }

        var files = Directory.GetFiles(directory, "*.json");
        if (files.Length == 0)
            LogWarning($"No *.json workflow files found in: {directory}");

        return builder.WithWorkflows(files);
    }

    /// <summary>
    /// Alias for <see cref="WithWorkflowsFromDirectory"/>: seeds all <c>*.json</c> workflows from a directory.
    /// </summary>
    public static IResourceBuilder<N8nResource> WithImportWorkflows(
        this IResourceBuilder<N8nResource> builder, string workflowsPath)
        => builder.WithWorkflowsFromDirectory(workflowsPath);

    private static void SeedWorkflow(IResourceBuilder<N8nResource> builder, string json, string? originalName)
    {
        var resource = builder.Resource;
        var app = resource.AppBuilder!;

        if (app.ExecutionContext.IsPublishMode)
        {
            LogWarning("Workflow seeding uses local bind mounts and is skipped in publish mode.");
            return;
        }

        // Managed staging directory that aggregates all seeded workflows for a single import mount.
        var stagingDir = resource.ImportWorkflowsPath ??=
            Path.Combine(app.AppHostDirectory, "..", "infra", "n8n", resource.Name, "import", "workflows");
        Directory.CreateDirectory(stagingDir);

        if (!resource.WorkflowStagingCleared)
        {
            foreach (var stale in Directory.GetFiles(stagingDir, "*.json"))
                File.Delete(stale);
            resource.WorkflowStagingCleared = true;
        }

        var index = ++resource.SeededWorkflowCount;
        var safeName = Sanitize(originalName) ?? "workflow";
        var fileName = $"{index:D3}_{safeName}.json";
        File.WriteAllText(Path.Combine(stagingDir, fileName), json);

        var container = EnsureImportContainer(builder);
        if (container is not null && !resource.WorkflowsMountAdded)
        {
            container.WithBindMount(stagingDir, WorkflowsMountTarget, isReadOnly: true);
            resource.WorkflowsMountAdded = true;
        }
    }

    private static string? Sanitize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;
        var chars = name.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '-').ToArray();
        var result = new string(chars).Trim('-');
        return string.IsNullOrEmpty(result) ? null : result;
    }

    #endregion

    #region Credentials

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

        var container = EnsureImportContainer(builder);
        if (container is not null && !builder.Resource.CredentialsMountAdded)
        {
            container.WithBindMount(credentialsPath, CredentialsMountTarget, isReadOnly: true);
            builder.Resource.CredentialsMountAdded = true;
        }

        return builder;
    }

    #endregion

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
