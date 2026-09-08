using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// An OData service as a connection: the URL of the service root, and the headers the service wants.
/// </summary>
/// <remarks>
/// The studio reads a service over HTTP, V2 to V4, and only ever reads. What it needs is a URL and
/// possibly a way in — a Basic pair, a Bearer token, an API key header. That is a connection string
/// with a shape, which is what these methods write: <c>WithConnection</c> would take a URL that is
/// not one, and the studio would then fail on the first click instead of the app host saying so
/// while the stack is being described.
/// </remarks>
public static class WebDataStudioODataExtensions
{
    /// <summary>
    /// Attaches an OData service.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="connectionName">Label in the studio, e.g. <c>NORTHWIND</c>.</param>
    /// <param name="url">
    /// The URL of the service root. <c>user:pw@</c> in it travels as Basic authentication and
    /// <c>bearer:&lt;token&gt;@</c> as a Bearer token, both as headers rather than in the URL.
    /// </param>
    /// <param name="headers">
    /// Request headers, one per entry — how a service behind an API key or a session cookie is
    /// reached. The studio's own server makes the request, so a browser's cookies never reach the
    /// service.
    /// </param>
    /// <param name="group">Groups the connection in the explorer.</param>
    /// <param name="color">Tints the connection, e.g. <c>#e03131</c> for production.</param>
    /// <param name="readOnly">
    /// Read-only, which is what the driver is: no POST, no PATCH, no DELETE. Passing
    /// <c>false</c> only makes the UI offer edits the service would refuse.
    /// </param>
    /// <example>
    /// <code>
    /// studio.WithODataService("NORTHWIND", "https://services.odata.org/V4/Northwind/Northwind.svc/")
    ///       .WithODataService("SAP", "https://sap.example/odata/", new()
    ///       {
    ///           ["X-Api-Key"] = "…",
    ///           ["Accept-Language"] = "de-DE",
    ///       });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">
    /// The URL is not an absolute <c>http</c> or <c>https</c> URL, or a header has no name or would
    /// smuggle a second one.
    /// </exception>
    public static IResourceBuilder<WebDataStudioResource> WithODataService(
        this IResourceBuilder<WebDataStudioResource> builder,
        string connectionName,
        string url,
        Dictionary<string, string>? headers = null,
        string? group = null,
        string? color = null,
        bool readOnly = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        var root = ServiceRoot(url);
        var lines = new List<string> { root };

        foreach (var (name, value) in headers ?? [])
            lines.Add(Header(name, value));

        return builder.WithConnection(connectionName, string.Join('\n', lines),
            WebDataStudioEngine.OData, readOnly, group, color);
    }

    /// <summary>
    /// Attaches an OData service whose one header carries a secret — an API key, a token — so that
    /// the value comes from a parameter rather than from the app host's source.
    /// </summary>
    /// <param name="builder">The studio.</param>
    /// <param name="connectionName">Label in the studio.</param>
    /// <param name="url">The URL of the service root.</param>
    /// <param name="headerName">The header's name, e.g. <c>X-Api-Key</c>.</param>
    /// <param name="headerValue">
    /// Where the value comes from — a parameter, another resource's connection string, anything the
    /// stack resolves when it runs.
    /// </param>
    /// <param name="group">Groups the connection in the explorer.</param>
    /// <param name="color">Tints the connection.</param>
    /// <param name="readOnly">Read-only, which is what the driver is.</param>
    /// <example>
    /// <code>
    /// var key = builder.AddParameter("sap-key", secret: true);
    ///
    /// studio.WithODataService("SAP", "https://sap.example/odata/", "X-Api-Key", key);
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">
    /// The URL is not an absolute <c>http</c> or <c>https</c> URL, or the header has no usable name.
    /// </exception>
    public static IResourceBuilder<WebDataStudioResource> WithODataService(
        this IResourceBuilder<WebDataStudioResource> builder,
        string connectionName,
        string url,
        string headerName,
        IResourceBuilder<ParameterResource> headerValue,
        string? group = null,
        string? color = null,
        bool readOnly = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(headerValue);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        var root = ServiceRoot(url);
        var name = HeaderName(headerName);

        // The value is resolved when the stack runs, so the connection string is a reference rather
        // than a literal — and the secret never sits in the app host's source or in a manifest.
        return builder.WithConnection(connectionName,
            ReferenceExpression.Create($"{root}\n{name}: {headerValue.Resource}"),
            WebDataStudioEngine.OData, readOnly, group, color);
    }

    /// <summary>
    /// The URL, checked. A service root that is not an absolute http(s) URL is the mistake worth
    /// catching here: the studio would take it and fail on the first click.
    /// </summary>
    private static string ServiceRoot(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var trimmed = url.Trim();

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var parsed)
            || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException(
                "an OData connection is the absolute http or https URL of the service root — "
                + $"'{url}' is not one", nameof(url));

        return trimmed;
    }

    /// <summary>
    /// One header as the studio reads it, <c>Name: value</c>. A newline anywhere would write a
    /// second header into the connection string, so it is refused rather than passed on.
    /// </summary>
    private static string Header(string name, string value)
    {
        var clean = HeaderName(name);

        if (value is null || value.Contains('\n') || value.Contains('\r'))
            throw new ArgumentException(
                $"the value of the header '{clean}' must be one line: a newline in it would write a "
                + "second header into the connection string", nameof(name));

        return $"{clean}: {value.Trim()}";
    }

    private static string HeaderName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Contains('\n') || name.Contains('\r')
            || name.Contains(':'))
            throw new ArgumentException(
                $"'{name}' is not a header name: it has to be one line and carry no colon",
                nameof(name));

        return name.Trim();
    }
}
