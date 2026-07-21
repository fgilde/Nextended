using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.AspireUI;

/// <summary>
/// AspireUI — the visual .NET Aspire AppHost builder — running as a container resource. Exposes an
/// HTTP endpoint for the web UI, and can be pre-seeded (admin user + a starter stack) so it comes up
/// ready without the manual first-run wizard.
/// </summary>
public sealed class AspireUIResource(string name) : ContainerResource(name)
{
    /// <summary>Container image (without registry-less shorthand): the published AspireUI image.</summary>
    public const string DefaultImage = "ghcr.io/fgilde/aspireui";

    /// <summary>Default image tag.</summary>
    public const string DefaultTag = "latest";

    /// <summary>Internal port the AspireUI server listens on inside the container.</summary>
    public const int DefaultTargetPort = 8080;

    /// <summary>Name of the primary HTTP endpoint (the web UI).</summary>
    public const string HttpEndpointName = "http";

    /// <summary>The HTTP endpoint serving the AspireUI web app.</summary>
    public EndpointReference HttpEndpoint => new(this, HttpEndpointName);

    /// <summary>Admin username seeded on first run, if configured via <c>WithAdminUser</c>.</summary>
    public string? AdminUsername { get; internal set; }

    /// <summary>Name of the starter stack seeded on first run, if configured via <c>WithSeedStack</c>.</summary>
    public string? SeedStackName { get; internal set; }

    /// <summary>Project paths seeded into the starter stack (one <c>AddProject</c> node each).</summary>
    public IList<string> SeedProjects { get; } = new List<string>();
}
