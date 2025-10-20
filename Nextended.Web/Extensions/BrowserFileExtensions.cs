using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using Nextended.Core.Helper;

namespace Nextended.Web.Extensions;

public static class BrowserFileExtensions
{
    public static async Task<byte[]> GetBytesAsync(this IBrowserFile file, CancellationToken cancellationToken = default)
    {
        var buffer = new byte[file.Size];
        await file.OpenReadStream().ReadAsync(buffer, cancellationToken);
        return buffer;
    }

    public static byte[] GetBytes(this IBrowserFile file)
    {
        var buffer = new byte[file.Size];
        file.OpenReadStream().Read(buffer);
        return buffer;
    }

    public static string GetReadableFileSize(this IBrowserFile file, bool fullName = false, IStringLocalizer localizer = null)
    {
        return FileHelper.GetReadableFileSize(file.Size, fullName, localizer != null ? s => localizer[s] : null);
    }
}