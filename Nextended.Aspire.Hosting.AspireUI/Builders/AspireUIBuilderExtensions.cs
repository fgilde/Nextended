using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.AspireUI;

/// <summary>
/// Fluent API for running AspireUI inside your Aspire stack. Start with <see cref="AddAspireUI"/>,
/// then optionally <see cref="WithAdminUser(IResourceBuilder{AspireUIResource}, string, string)"/> and
/// <see cref="WithSeedStack"/> to have it come up pre-configured.
/// </summary>
public static class AspireUIBuilderExtensions
{
    /// <summary>
    /// Adds AspireUI as a container. Exposes the web UI over HTTP, mounts the host Docker socket so
    /// AspireUI can run the stacks you build in it, and persists its data (stacks, settings, users)
    /// on a named volume.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">Resource name (default <c>aspireui</c>).</param>
    /// <param name="port">Optional fixed host port for the UI (default: auto-assigned).</param>
    /// <param name="image">Override the container image (default <c>ghcr.io/fgilde/aspireui</c>).</param>
    /// <param name="tag">Override the image tag (default <c>latest</c>).</param>
    public static IResourceBuilder<AspireUIResource> AddAspireUI(
        this IDistributedApplicationBuilder builder,
        string name = "aspireui",
        int? port = null,
        string? image = null,
        string? tag = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Precomputed string: passing an interpolated string directly would bind to the
        // ReferenceExpression WithEnvironment overload (which can't format an int).
        var urls = "http://+:" + AspireUIResource.DefaultTargetPort;
        return builder.AddResource(new AspireUIResource(name))
            .WithImage(image ?? AspireUIResource.DefaultImage, tag ?? AspireUIResource.DefaultTag)
            // Default tag is a rolling "latest" — always re-pull so a stale local image doesn't pin an old build.
            .WithImagePullPolicy(ImagePullPolicy.Always)
            .WithHttpEndpoint(port: port, targetPort: AspireUIResource.DefaultTargetPort, name: AspireUIResource.HttpEndpointName)
            // The container listens on 8080; keep its data on /data (matches the env below).
            .WithEnvironment("ASPNETCORE_URLS", urls)
            .WithEnvironment("DB_PATH", "/data/aspireui.db")
            .WithEnvironment("WORKSPACE_DIR", "/data/workspace")
            // Cookies ignore the port, so multiple AspireUI instances on localhost would share a session
            // cookie and log each other out. A per-instance cookie name keeps them independent.
            .WithEnvironment("ASPIREUI_COOKIE_NAME", "aspireui-" + name)
            // Per-instance named volume so different AspireUI resources don't share stacks/users/settings.
            .WithVolume($"aspireui-data-{name}", "/data")
            // AspireUI shells `dotnet run` on generated AppHosts, which start their own containers —
            // it needs the host Docker daemon. (Linux hosts; on Docker Desktop the socket path holds.)
            .WithBindMount("/var/run/docker.sock", "/var/run/docker.sock");
    }

    /// <summary>
    /// Pre-seeds the AspireUI admin user on first run (skipped once any user exists; the password is
    /// stored hashed). Without this, AspireUI shows its first-run setup wizard.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithAdminUser(
        this IResourceBuilder<AspireUIResource> builder, string username, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        builder.Resource.AdminUsername = username;
        return builder
            .WithEnvironment("ASPIREUI_ADMIN_USERNAME", username)
            .WithEnvironment("ASPIREUI_ADMIN_PASSWORD", password);
    }

    /// <summary>
    /// Pre-seeds the AspireUI admin user with the password coming from an Aspire parameter
    /// (secret-friendly — keeps it out of the manifest/source).
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithAdminUser(
        this IResourceBuilder<AspireUIResource> builder, string username, IResourceBuilder<ParameterResource> password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        builder.Resource.AdminUsername = username;
        return builder
            .WithEnvironment("ASPIREUI_ADMIN_USERNAME", username)
            .WithEnvironment("ASPIREUI_ADMIN_PASSWORD", password);
    }

    /// <summary>
    /// Pre-seeds a starter stack of the given name on first run, with one <c>AddProject</c> node per
    /// project path. Handy to point AspireUI straight at the project(s) you're working on, e.g.
    /// <c>WithSeedStack("dev", builder.AppHostDirectory)</c>. The paths are recorded as-is; for the
    /// seeded stack to also <em>run</em> inside the container, mount the sources there too
    /// (see <see cref="WithSourceMount"/>).
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithSeedStack(
        this IResourceBuilder<AspireUIResource> builder, string stackName, params string[] projectPaths)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stackName);
        ArgumentNullException.ThrowIfNull(projectPaths);
        builder.Resource.SeedStackName = stackName;
        foreach (var p in projectPaths)
            if (!string.IsNullOrWhiteSpace(p)) builder.Resource.SeedProjects.Add(p);
        return builder
            .WithEnvironment("ASPIREUI_SEED_STACK_NAME", stackName)
            .WithEnvironment("ASPIREUI_SEED_STACK_PROJECTS", string.Join(";", builder.Resource.SeedProjects));
    }

    /// <summary>
    /// Bind-mounts a host source folder into the AspireUI container at the same (or a given) path, so
    /// stacks seeded via <see cref="WithSeedStack"/> can actually build/run there.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithSourceMount(
        this IResourceBuilder<AspireUIResource> builder, string hostPath, string? containerPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostPath);
        return builder.WithBindMount(hostPath, containerPath ?? hostPath);
    }

    /// <summary>
    /// Configures AspireUI's built-in AI assistant to use an OpenAI-compatible endpoint (base URL +
    /// model, optional key). Seeded into AspireUI's settings on first run (via the ASPIREUI_AI_* env).
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithAi(
        this IResourceBuilder<AspireUIResource> builder, string baseUrl, string model, string? apiKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        builder.WithEnvironment("ASPIREUI_AI_BASE_URL", baseUrl)
               .WithEnvironment("ASPIREUI_AI_MODEL", model);
        if (!string.IsNullOrEmpty(apiKey)) builder.WithEnvironment("ASPIREUI_AI_API_KEY", apiKey);
        return builder;
    }

    /// <summary>
    /// Points AspireUI's assistant at an OpenAI-compatible backend resource in the same stack
    /// (e.g. Ollama or LocalAI). The base URL is derived from the backend's HTTP endpoint +
    /// <paramref name="apiPath"/> (default <c>/v1</c>). AspireUI waits for the backend to be ready.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithAi<T>(
        this IResourceBuilder<AspireUIResource> builder, IResourceBuilder<T> backend, string model,
        string endpointName = "http", string? apiKey = null)
        where T : IResourceWithEndpoints
    {
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        var ep = backend.GetEndpoint(endpointName);
        // "/v1" is a literal part of the interpolated ReferenceExpression (the handler only accepts
        // value-providers like the endpoint for interpolated holes, not arbitrary strings).
        builder.WithEnvironment("ASPIREUI_AI_BASE_URL", ReferenceExpression.Create($"{ep}/v1"))
               .WithEnvironment("ASPIREUI_AI_MODEL", model);
        if (!string.IsNullOrEmpty(apiKey)) builder.WithEnvironment("ASPIREUI_AI_API_KEY", apiKey);
        return builder;
    }
}
