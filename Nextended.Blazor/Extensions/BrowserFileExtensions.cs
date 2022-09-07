using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using Nextended.Blazor.Helper;
using Nextended.Blazor.Models;

namespace Nextended.Blazor.Extensions;

public static class BrowserFileExtensions
{

    public static async Task<string> GetDataUrlAsync(this IBrowserFile file)
    {
        return await DataUrl.GetDataUrlAsync(await file.GetBytesAsync(), file.ContentType);
    }

    public static async Task<byte[]> GetBytesAsync(this IBrowserFile file, CancellationToken cancellationToken = default)
    {
        if (file is ZipBrowserFile { FileBytes: { } } zipEntry)
            return zipEntry.FileBytes;
        var buffer = new byte[file.Size];
        await file.OpenReadStream(file.Size).ReadAsync(buffer, cancellationToken);
        return buffer;
    }

    public static byte[] GetBytes(this IBrowserFile file)
    {
        if (file is ZipBrowserFile { FileBytes: { } } zipEntry)
            return zipEntry.FileBytes;
        using var ms = new MemoryStream();
        using var openReadStream = file.OpenReadStream(file.Size);
        openReadStream.CopyToAsync(ms);
        return ms.ToArray();
    }

    public static string GetReadableFileSize(this IBrowserFile file, IStringLocalizer localizer, bool fullName = false)
    {
        return GetReadableFileSize(file.Size, localizer, fullName);
    }

    public static string GetReadableFileSize(long size, IStringLocalizer localizer = null, bool fullName = false)
    {
        var source = new Dictionary<string, string>
        {
            {"B", "Bytes"},
            {"KB", "Kilobytes"},
            {"MB", "Megabytes"},
            {"GB", "Gigabytes"},
            {"TB", "Terabytes"},
            {"PB", "Petabytes"},
            {"EB", "Exabytes"},
            {"ZB", "Zettabytes"},
            {"YB", "Yottabytes"},
            {"BB", "Brontobytes"}
        };
        double length = size;
        int index;
        for (index = 0; length >= 1024.0 && index + 1 < source.Count; length /= 1024.0)
            ++index;
        KeyValuePair<string, string> keyValuePair = source.ElementAt(index);
        return localizer != null ? $"{length:0.##} {localizer[fullName ? keyValuePair.Value : keyValuePair.Key]}" : $"{length:0.##} {(fullName ? keyValuePair.Value : keyValuePair.Key)}";
    }
    
    public static bool IsZipFile(this IBrowserFile file)
    {
        return MimeTypeHelper.IsZip(file.ContentType);
    }


}