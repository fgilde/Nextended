using System.IO.Compression;
using Nextended.Core.Extensions;
using Nextended.Core.Types;

namespace Nextended.Blazor.Models;

public class ArchiveStructureBase<T> : Hierarchical<T> where T : ArchiveStructureBase<T>, new()
{
    public ArchiveStructureBase()
    {
        IsExpanded = true;
    }

    public ArchiveStructureBase(IArchivedBrowserFile browserFile)
        : this(browserFile.Name)
    {
        BrowserFile = browserFile;
    }

    public ArchiveStructureBase(string name): this()
    {
        Name = name;
    }

    public string Name { get; set; }

    public bool IsDirectory => BrowserFile == null || BrowserFile.IsDirectory;

    public bool IsDownloading { get; set; }

    public long Size => IsDirectory ? ContainingFiles.Sum(f => f.Size) : BrowserFile.Size;

    public IEnumerable<IArchivedBrowserFile> ContainingFiles
        => Children?.Recursive(s => s.Children ?? Enumerable.Empty<T>()).Where(s => s is { IsDirectory: false }).Select(s => s.BrowserFile);
    
    public IArchivedBrowserFile BrowserFile { get; set; }

    public async Task<byte[]> ToArchiveBytesAsync()
    {
        var archive = await ToArchiveAsync();
        await using (archive.Stream)
        {
            using var zipArchive = archive.Archive;
            return archive.Stream.ToArray();
        }
    }

    private async Task<(MemoryStream Stream, ZipArchive Archive)> ToArchiveAsync()
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

    protected static void EnsurePartExists(T archiveContent, List<string> parts, string p, IList<IArchivedBrowserFile> archiveEntries)
    {
        if (parts.Any() && !string.IsNullOrEmpty(parts.FirstOrDefault()))
        {
            var title = parts.First();

            var child = archiveContent.Children.SingleOrDefault(x => x.Name == title);

            if (child == null)
            {
                child = new T()
                {
                    Name=title,
                    Parent = archiveContent,
                    Children = FindByPath(archiveEntries, p).ToHashSet()
                };

                archiveContent.Children.Add(child);
            }

            EnsurePartExists(child, parts.Skip(1).ToList(), p, archiveEntries);
        }
    }

    protected static IEnumerable<T> FindByPath(IList<IArchivedBrowserFile> archiveEntries, string path = default)
    {
        return archiveEntries.Where(f => !f.IsDirectory && f.Path == path).Select(file => new T() { BrowserFile = file});
    }

    public static T CreateStructure(IList<IArchivedBrowserFile> archiveEntries, string rootFolderName)
    {
        var paths = archiveEntries.Select(file => file.Path).Distinct().ToArray();
        var root = new T {Name = rootFolderName, Children = FindByPath(archiveEntries, "").ToHashSet() };
        foreach (var p in paths)
        {
            var parts = p.Split('/');
            EnsurePartExists(root, parts.ToList(), p, archiveEntries);
        }
        return root;
    }
}
