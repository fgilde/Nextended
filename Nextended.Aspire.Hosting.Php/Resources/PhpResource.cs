using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.Php;

/// <summary>
/// A PHP app served by PHP's built-in web server (<c>php -S</c>) inside the official
/// <c>php:cli</c> container. The source is a bind-mounted host folder (docroot) or a single
/// <c>.php</c> file (router script — every request is handed to it). Exposes one HTTP endpoint
/// and supports service discovery, so .NET services can call the PHP endpoints via
/// <c>WithReference</c>.
/// </summary>
public sealed class PhpResource(string name) : ContainerResource(name), IResourceWithServiceDiscovery
{
    /// <summary>Container image: the official PHP image.</summary>
    public const string DefaultImage = "php";

    /// <summary>Default image tag (CLI variant — ships the built-in web server).</summary>
    public const string DefaultTag = "8.4-cli";

    /// <summary>Port the built-in server listens on inside the container.</summary>
    public const int DefaultTargetPort = 80;

    /// <summary>Mount point of the app source inside the container.</summary>
    public const string AppDirectory = "/app";

    /// <summary>Name of the HTTP endpoint.</summary>
    public const string HttpEndpointName = "http";

    /// <summary>The HTTP endpoint serving the PHP app.</summary>
    public EndpointReference HttpEndpoint => new(this, HttpEndpointName);

    /// <summary>
    /// File name of the router script when a single <c>.php</c> file was added; <c>null</c> in
    /// folder (docroot) mode.
    /// </summary>
    public string? RouterScript { get; internal set; }

    /// <summary>php.ini directives applied as <c>-d key=value</c> (see <c>WithPhpIni</c>).</summary>
    public IDictionary<string, string> IniSettings { get; } = new Dictionary<string, string>();
}
