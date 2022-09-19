using System.IO.Compression;
using Nextended.Core.Extensions;
using Nextended.Core.Types;

namespace Nextended.Blazor.Models;

public class ZipStructure : Hierarchical<ZipStructure>
{

    public ZipStructure(ZipBrowserFile browserFile)
        : this(browserFile.Name)
    {
        BrowserFile = browserFile;
    }

    public ZipStructure(string name)
    {
        Name = name;
        IsExpanded = true;
    }

    public string Name { get; set; }

    public bool IsDirectory => BrowserFile == null || BrowserFile.IsDirectory;

    public bool IsDownloading { get; set; }

    public long Size => IsDirectory ? ContainingFiles.Sum(f => f.Size) : BrowserFile.Size;

    public IEnumerable<ZipBrowserFile> ContainingFiles
        => Children?.Recursive(s => s.Children ?? Enumerable.Empty<ZipStructure>()).Where(s => s is { IsDirectory: false }).Select(s => s.BrowserFile);

    public ZipBrowserFile BrowserFile { get; set; }

    public async Task<(MemoryStream Stream, ZipArchive Archive)> ToArchiveAsync()
    {
        var ms = new MemoryStream();
        using ZipArchive archive = new ZipArchive(ms, ZipArchiveMode.Create, true);

        var path = IsDirectory ? string.Join('/', Path.Skip(1).Select(s => s.Name)) : BrowserFile?.Path ?? "";
        path = !string.IsNullOrWhiteSpace(path) ? path.EnsureEndsWith('/') : path;

        foreach (var file in IsDirectory ? ContainingFiles : new[] { BrowserFile })
        {
            var entry = archive.CreateEntry(file.FullName.Substring(path.Length), CompressionLevel.Optimal);
            await using var stream = entry.Open();
            await stream.WriteAsync(file.FileBytes);
        }

        return (ms, archive);
    }

    public async Task<byte[]> ToArchiveBytesAsync()
    {
        var archive = await ToArchiveAsync();
        await using (archive.Stream)
        {
            using var zipArchive = archive.Archive;
            return archive.Stream.ToArray();
        }
    }

    private static void EnsurePartExists(ZipStructure zipContent, List<string> parts, string p, IList<ZipBrowserFile> zipEntries)
    {
        if (parts.Any() && !string.IsNullOrEmpty(parts.FirstOrDefault()))
        {
            var title = parts.First();

            var child = zipContent.Children.SingleOrDefault(x => x.Name == title);

            if (child == null)
            {
                child = new ZipStructure(title)
                {
                    Parent = zipContent,
                    Children = FindByPath(zipEntries, p).ToHashSet()
                };

                zipContent.Children.Add(child);
            }

            EnsurePartExists(child, parts.Skip(1).ToList(), p, zipEntries);
        }
    }

    private static IEnumerable<ZipStructure> FindByPath(IList<ZipBrowserFile> zipEntries, string path = default)
    {
        return zipEntries.Where(f => !f.IsDirectory && f.Path == path).Select(file => new ZipStructure(file));
    }

    public static HashSet<ZipStructure> CreateStructure(IList<ZipBrowserFile> zipEntries, string rootFolderName)
    {
        var paths = zipEntries.Select(file => file.Path).Distinct().ToArray();
        var root = new ZipStructure(rootFolderName) { Children = FindByPath(zipEntries, "").ToHashSet() };
        foreach (var p in paths)
        {
            var parts = p.Split('/');
            EnsurePartExists(root, parts.ToList(), p, zipEntries);
        }

        return new[] { root }.ToHashSet();
    }
}