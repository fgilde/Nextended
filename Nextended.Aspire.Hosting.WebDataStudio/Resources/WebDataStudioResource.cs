using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// WebDataStudio — a browser-based database studio — running as a container resource. Exposes an
/// HTTP endpoint for the studio, and carries the connections that were attached to it so several
/// databases can share one instance.
/// </summary>
public sealed class WebDataStudioResource(string name) : ContainerResource(name)
{
    /// <summary>The published WebDataStudio image.</summary>
    public const string DefaultImage = "ghcr.io/fgilde/webdatastudio";

    /// <summary>Default image tag.</summary>
    public const string DefaultTag = "latest";

    /// <summary>Port the studio listens on inside the container.</summary>
    public const int DefaultTargetPort = 8080;

    /// <summary>Name of the HTTP endpoint serving the studio.</summary>
    public const string HttpEndpointName = "http";

    /// <summary>Resource name used when nothing else is asked for, and the key for sharing one studio.</summary>
    public const string DefaultResourceName = "webdatastudio";

    /// <summary>The HTTP endpoint serving the studio.</summary>
    public EndpointReference HttpEndpoint => new(this, HttpEndpointName);

    /// <summary>Login name, when one was configured with <c>WithLogin</c>. Null means anonymous access.</summary>
    public string? Username { get; internal set; }

    /// <summary>
    /// The name the studio shows in its header and browser tab. Defaults to the resource name, so
    /// three studios in one stack are told apart at a glance; <c>WithTitle</c> overrides it and
    /// <c>WithTitle(null)</c> leaves the studio unnamed.
    /// </summary>
    public string? Title { get; internal set; }

    /// <summary>
    /// Names of the connections attached to this studio, in the order they were added. These are
    /// the labels the studio shows in its explorer, and the suffixes of its <c>WDS_CONN_*</c>
    /// variables.
    /// </summary>
    public IReadOnlyList<string> ConnectionNames => _connectionNames;

    private readonly List<string> _connectionNames = [];

    internal void TrackConnection(string connectionName) => _connectionNames.Add(connectionName);

    internal bool HasConnection(string connectionName) =>
        _connectionNames.Contains(connectionName, StringComparer.OrdinalIgnoreCase);
}
