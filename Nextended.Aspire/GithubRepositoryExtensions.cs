using System.Diagnostics;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire;

/// <summary>
/// Options for <see cref="GithubRepositoryExtensions.AddGithubRepository"/> /
/// <see cref="GithubRepositoryExtensions.WithGithubSource{T}"/>.
/// </summary>
public sealed class GithubRepositoryOptions
{
    /// <summary>Git branch/tag/commit-ish to check out. Default <c>main</c>. Pin a tag for reproducible builds.</summary>
    public string GitRef { get; set; } = "main";

    /// <summary>
    /// Content of a Dockerfile to <b>generate</b> next to the checkout (for repos that ship none).
    /// Line endings are normalized to LF. When <c>null</c>, the repo's own Dockerfile is used
    /// (see <see cref="DockerfilePath"/>).
    /// </summary>
    public string? DockerfileContent { get; set; }

    /// <summary>
    /// Path of the Dockerfile <b>inside the checkout</b>, relative to the repo root — only used when
    /// <see cref="DockerfileContent"/> is <c>null</c>. Default: the builder's own default (<c>Dockerfile</c> in the context).
    /// </summary>
    public string? DockerfilePath { get; set; }

    /// <summary>Subdirectory of the checkout to use as the docker build context. Default: repo root.</summary>
    public string? ContextSubPath { get; set; }

    /// <summary>
    /// Directory the repository is cloned into. Default <c>{AppHostDirectory}/obj/github/{resourceName}</c>.
    /// The checkout itself lives in a <c>src</c> subfolder; a generated Dockerfile is written next to it.
    /// </summary>
    public string? CheckoutDirectory { get; set; }
}

/// <summary>
/// Runs any GitHub (or other git) repository as an Aspire container resource: the repo is cloned/updated
/// on the host at build time and built via its own — or a generated — Dockerfile.
/// </summary>
public static class GithubRepositoryExtensions
{
    /// <summary>
    /// Adds a container resource built from a git repository.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.AddGithubRepository("myui", "https://github.com/acme/my-ui", o => o.GitRef = "v1.2.0")
    ///        .WithHttpEndpoint(targetPort: 3000);
    /// </code>
    /// </example>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">Resource name (also used as the local image name).</param>
    /// <param name="repository">Git repository URL (https or ssh).</param>
    /// <param name="configure">Git ref, Dockerfile generation/location, context …</param>
    public static IResourceBuilder<ContainerResource> AddGithubRepository(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        string repository,
        Action<GithubRepositoryOptions>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repository);
        return builder.AddContainer(name, name).WithGithubSource(repository, configure);
    }

    /// <summary>
    /// Builds an existing container resource from a git repository instead of a registry image:
    /// clones/updates the repo on the host and wires <c>WithDockerfile</c> to the checkout
    /// (generating the Dockerfile first when <see cref="GithubRepositoryOptions.DockerfileContent"/> is set).
    /// </summary>
    public static IResourceBuilder<T> WithGithubSource<T>(
        this IResourceBuilder<T> builder,
        string repository,
        Action<GithubRepositoryOptions>? configure = null)
        where T : ContainerResource
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repository);
        var options = new GithubRepositoryOptions();
        configure?.Invoke(options);

        var app = builder.ApplicationBuilder;
        var name = builder.Resource.Name;
        var rootDir = options.CheckoutDirectory ?? Path.Combine(app.AppHostDirectory, "obj", "github", name);
        var srcDir = Path.Combine(rootDir, "src");

        EnsureGitCheckout(repository, options.GitRef, srcDir);

        // Keep .git out of the build context (smaller + stable layer cache); only when the checkout
        // ships no .dockerignore itself (untracked files survive the refresh reset).
        var dockerIgnore = Path.Combine(srcDir, ".dockerignore");
        if (!File.Exists(dockerIgnore))
            File.WriteAllText(dockerIgnore, ".git\n");

        string? dockerfilePath = null;
        if (options.DockerfileContent is { } content)
        {
            // Generated NEXT to the checkout (outside the context) so it never collides with repo files.
            // LF endings: heredoc/entrypoint lines break with CRLF ("bash\r: not found").
            dockerfilePath = Path.Combine(rootDir, "Dockerfile");
            File.WriteAllText(dockerfilePath, content.ReplaceLineEndings("\n"));
        }
        else if (options.DockerfilePath is { } relative)
        {
            dockerfilePath = Path.Combine(srcDir, relative);
        }

        var contextDir = options.ContextSubPath is { } sub ? Path.Combine(srcDir, sub) : srcDir;

        // WithDockerfile only REPLACES an existing image annotation (AddContainer normally creates it) —
        // a custom resource added via AddResource needs the placeholder annotation up front.
        if (!builder.Resource.Annotations.OfType<ContainerImageAnnotation>().Any())
            builder.Resource.Annotations.Add(new ContainerImageAnnotation { Image = name.ToLowerInvariant(), Tag = "latest" });

        return dockerfilePath is null
            ? builder.WithDockerfile(contextDir)
            : builder.WithDockerfile(contextDir, dockerfilePath);
    }

    /// <summary>
    /// Clones <paramref name="repository"/>@<paramref name="gitRef"/> into <paramref name="dir"/> (shallow),
    /// or refreshes an existing checkout to that ref. A failed refresh (offline) keeps the existing
    /// checkout; the initial clone is required and throws with a clear message.
    /// </summary>
    public static void EnsureGitCheckout(string repository, string gitRef, string dir)
    {
        if (!Directory.Exists(Path.Combine(dir, ".git")))
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            Directory.CreateDirectory(Path.GetDirectoryName(dir)!);
            // autocrlf=false: a Windows checkout would otherwise stamp CRLF into the repo's Dockerfile,
            // whose heredoc-generated entrypoint script then fails in the container ("bash\r: not found").
            if (!RunGit($"clone -c core.autocrlf=false --depth 1 --branch {gitRef} {repository} \"{dir}\"", workDir: null))
                throw new InvalidOperationException(
                    $"Could not clone '{repository}' ({gitRef}) into '{dir}' — git must be installed and the repository reachable.");
        }
        else if (RunGit($"fetch --depth 1 origin {gitRef}", dir))
        {
            RunGit("reset --hard FETCH_HEAD", dir);
        }
    }

    /// <summary>Runs git with output passing through to the AppHost console; <c>false</c> on failure (incl. git missing).</summary>
    private static bool RunGit(string args, string? workDir)
    {
        try
        {
            var psi = new ProcessStartInfo("git", args) { UseShellExecute = false };
            if (workDir is not null) psi.WorkingDirectory = workDir;
            using var process = Process.Start(psi);
            if (process is null) return false;
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
