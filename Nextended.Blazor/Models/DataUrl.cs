using System.Net.Mime;
using System.Text.RegularExpressions;

namespace Nextended.Blazor.Models;

[Obsolete($"Please use {nameof(Nextended.Core.Types.DataUrl)} from namespace Nextended.Core.Types")]
public class DataUrl : Core.Types.DataUrl
{
    public DataUrl(byte[] bytes, ContentType mimeType) : base(bytes, mimeType)
    {}

    public DataUrl(byte[] bytes, string mimeType = default) : base(bytes, mimeType)
    {}

    public DataUrl(string url) : base(url)
    {}
}