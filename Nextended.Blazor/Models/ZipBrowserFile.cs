using System.IO.Compression;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace Nextended.Blazor.Models;

/**
 * Represents a Zip file entry compatiable as BrowserFile
 */
public record ZipBrowserFile : IArchivedBrowserFile
{
    public ZipArchiveEntry Entry { get; }
    public byte[] FileBytes { get; private set; }

    public ZipBrowserFile(ZipArchiveEntry entry, bool load = true)
    {        
        if (load)
            FileBytes = entry.Open().ToByteArray();

        Entry = entry;
        Name = Entry.Name;
        Size = Entry.Length;
        LastModified = Entry.LastWriteTime;
        FullName = entry.FullName;
        ContentType = MimeType.GetMimeType(entry.FullName);

        if (string.IsNullOrWhiteSpace(FullName))
            FullName = Name;
        if (string.IsNullOrWhiteSpace(Name) && IsDirectory)
            Name = PathArray.Last(s => !string.IsNullOrEmpty(s));
    }

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        if (!FileBytes?.Any() == true)
            FileBytes ??= Entry.Open().ToByteArray();
        return new MemoryStream(FileBytes);
    }
    
    public string Name { get; init; }
    public DateTimeOffset LastModified { get; }
    public long Size { get; }
    public string ContentType { get; }
    public string FullName { get; }
    public string Path => FullName.TrimEnd(Name.ToCharArray());
    public bool IsDirectory => Path == FullName;
    public string[] PathArray => Path.Split('/').Where(s => !string.IsNullOrEmpty(s)).ToArray();
    public string ParentDirectoryName => PathArray?.Any() == true ? !IsDirectory ? PathArray.Last(s => !string.IsNullOrEmpty(s)) : PathArray[^2] : string.Empty;
}