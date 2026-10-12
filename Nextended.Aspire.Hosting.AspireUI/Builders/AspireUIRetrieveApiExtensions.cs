using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.AspireUI;

/// <summary>
/// Turns on AspireUI's retrieve API: <c>GET /api/retrieve/apps</c> lists the hosted apps and the running
/// stacks as <c>[{ name, kind, healthy, urls }]</c>, for another app to pick up. Each app or stack can be
/// hidden from it in AspireUI's menus.
/// </summary>
public static class AspireUIRetrieveApiExtensions
{
    /// <summary>The header a caller sends the key in, when one is required.</summary>
    public const string KeyHeader = "X-Api-Key";

    /// <summary>The path of the list.</summary>
    public const string Path = "/api/retrieve/apps";

    /// <summary>Turns the retrieve API on, answering anyone who asks.</summary>
    public static IResourceBuilder<AspireUIResource> WithRetrieveApi(this IResourceBuilder<AspireUIResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithEnvironment("ASPIREUI_RETRIEVE_API", "true");
    }

    /// <summary>Turns the retrieve API on for callers that send <paramref name="apiKey"/> in the <c>X-Api-Key</c> header.</summary>
    public static IResourceBuilder<AspireUIResource> WithRetrieveApi(this IResourceBuilder<AspireUIResource> builder, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        return builder.WithRetrieveApi().WithEnvironment("ASPIREUI_RETRIEVE_API_KEY", apiKey);
    }

    /// <inheritdoc cref="WithRetrieveApi(IResourceBuilder{AspireUIResource}, string)"/>
    /// <param name="builder">The AspireUI resource.</param>
    /// <param name="apiKey">The key from an Aspire parameter, which keeps it out of the source.</param>
    public static IResourceBuilder<AspireUIResource> WithRetrieveApi(this IResourceBuilder<AspireUIResource> builder, IResourceBuilder<ParameterResource> apiKey)
    {
        ArgumentNullException.ThrowIfNull(apiKey);
        return builder.WithRetrieveApi().WithEnvironment("ASPIREUI_RETRIEVE_API_KEY", apiKey);
    }

    /// <summary>
    /// Leaves the apps and stacks of these names out of the retrieve API until someone shows them again
    /// from their menu in AspireUI. Calls add up.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithRetrieveApiHidden(this IResourceBuilder<AspireUIResource> builder, params string[] names)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(names);
        foreach (var n in names)
            if (!string.IsNullOrWhiteSpace(n) && !builder.Resource.RetrieveApiHidden.Contains(n.Trim()))
                builder.Resource.RetrieveApiHidden.Add(n.Trim());
        return builder.WithEnvironment("ASPIREUI_RETRIEVE_API_HIDDEN", string.Join(";", builder.Resource.RetrieveApiHidden));
    }
}
