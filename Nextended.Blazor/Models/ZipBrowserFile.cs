using System.IO.Compression;
using HeyRed.Mime;
using Microsoft.AspNetCore.Components.Forms;

namespace Nextended.Blazor.Models;

/**
 * Represents a Zip file entry compatiable as BrowserFile
 */
public record ZipBrowserFile : IBrowserFile
{
    private readonly ZipArchiveEntry _entry;
    private Stream _stream;
    private byte[] fileBytes;
    public ZipBrowserFile(ZipArchiveEntry entry)
    {
        using var fileStream = entry.Open();
        fileBytes = GetBytes(fileStream);

        _entry = entry;
        Name = _entry.Name;
        Size = _entry.Length;
        LastModified = _entry.LastWriteTime;
        FullName = entry.FullName;
        ContentType = MimeTypesMap.GetMimeType(entry.FullName);

        if (string.IsNullOrWhiteSpace(FullName))
            FullName = Name;
        if (string.IsNullOrWhiteSpace(Name) && IsDirectory)
            Name = PathArray.Last(s => !string.IsNullOrEmpty(s));
    }

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        return new MemoryStream(fileBytes);
    }

    public static byte[] GetBytes(Stream input)
    {
        using var ms = new MemoryStream();
        input.CopyToAsync(ms);
        return ms.ToArray();
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