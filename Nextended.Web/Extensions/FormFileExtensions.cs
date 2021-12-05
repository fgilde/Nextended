using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Nextended.Core.Helper;

namespace Nextended.Web.Extensions;

public static class FormFileExtensions
{
    public static async Task<byte[]> GetBytesAsync(this IFormFile file, CancellationToken cancellationToken = default)
    {
        var buffer = new byte[file.Length];
        await file.OpenReadStream().ReadAsync(buffer, cancellationToken);
        return buffer;
    }

    public static byte[] GetBytes(this IFormFile file)
    {
        var buffer = new byte[file.Length];
        file.OpenReadStream().Read(buffer);
        return buffer;
    }

    public static string GetReadableFileSize(this IFormFile file, bool fullName = false, IStringLocalizer localizer = null)
    {
        return FileHelper.GetReadableFileSize(file.Length, fullName, localizer != null ? s => localizer[s] : null);
    }
}