using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// Files an app host writes, put into the studio without a folder on anybody's machine.
///
/// Everything the studio can read from a repository — saved queries, export templates, quality
/// rules, seed scripts — it can now also be handed directly, as C# in the app host. Both at once is
/// fine: each of those settings takes a list of paths, so what the repository ships and what the app
/// host wrote live side by side rather than one silently replacing the other.
///
/// The files are created inside the container (`WithContainerFiles`) rather than mounted from the
/// host, so a published stack carries them the same way a local one does.
internal static class WebDataStudioInlineFiles
{
    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// Adds one file to the resource's inline folder for `setting`, and points the setting at that
    /// folder — appended to whatever is already there.
    ///
    /// The whole folder is written on every call: `WithContainerFiles` replaces what it is given,
    /// and the resource remembers every file so the last call still carries the earlier ones.
    public static IResourceBuilder<WebDataStudioResource> Add(
        IResourceBuilder<WebDataStudioResource> builder,
        string setting, string folder, string fileName, string content)
    {
        var files = builder.Resource.InlineFiles.TryGetValue(folder, out var existing)
            ? existing
            : builder.Resource.InlineFiles[folder] = new Dictionary<string, string>(StringComparer.Ordinal);

        files[fileName] = content;

        // One annotation per folder: every call writes the whole folder, so the previous one is
        // replaced rather than left behind for Aspire to apply on top of it.
        foreach (var stale in builder.Resource.Annotations
            .OfType<ContainerFileSystemCallbackAnnotation>()
            .Where(annotation => annotation.DestinationPath == folder)
            .ToList())
            builder.Resource.Annotations.Remove(stale);

        builder.WithContainerFiles(folder,
            files.OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => new ContainerFile { Name = entry.Key, Contents = entry.Value })
                .ToList<ContainerFileSystemItem>());

        return Point(builder, setting, folder);
    }

    /// One object written as one JSON file — an export template is read that way.
    public static IResourceBuilder<WebDataStudioResource> AddJsonObject(
        IResourceBuilder<WebDataStudioResource> builder,
        string setting, string folder, string fileName, object item) =>
        Add(builder, setting, folder, fileName, JsonSerializer.Serialize(item, Json));

    /// A list of things written as one JSON file — which is how the quality rules are read.
    public static IResourceBuilder<WebDataStudioResource> AddJson<T>(
        IResourceBuilder<WebDataStudioResource> builder,
        string setting, string folder, string fileName, IEnumerable<T> items) =>
        Add(builder, setting, folder, fileName, JsonSerializer.Serialize(items, Json));

    /// Points a setting at a path, keeping whatever it already named. The studio reads these as a
    /// list separated by `;`, so a repository folder and this one both count.
    private static IResourceBuilder<WebDataStudioResource> Point(
        IResourceBuilder<WebDataStudioResource> builder, string setting, string path)
    {
        var current = builder.Resource.PathSettings.TryGetValue(setting, out var existing) ? existing : [];

        if (!current.Contains(path, StringComparer.Ordinal))
        {
            current = [.. current, path];
            builder.Resource.PathSettings[setting] = current;
        }

        return builder.WithEnvironment(setting, string.Join(';', current));
    }

    /// Records a path a folder-taking builder mounted, so a later inline call keeps it rather than
    /// writing over the setting.
    public static IResourceBuilder<WebDataStudioResource> Mounted(
        IResourceBuilder<WebDataStudioResource> builder, string setting, string path) =>
        Point(builder, setting, path);

    /// A file name from something a person typed. A saved query called "Orders / last week" is a
    /// file name that has to survive both a Linux container and a Windows host.
    public static string FileName(string name, string extension)
    {
        var cleaned = new string(name.Trim()
            .Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' or ' '
                ? character
                : '-')
            .ToArray())
            .Trim();

        return (cleaned.Length == 0 ? "query" : cleaned) + extension;
    }
}
