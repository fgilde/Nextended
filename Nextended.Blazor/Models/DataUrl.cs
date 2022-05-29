using System.Net.Mime;

namespace Nextended.Blazor.Models;

public class DataUrl
{
    private readonly string _url;

    public DataUrl(byte[] bytes, ContentType mimeType) : this(bytes, mimeType.ToString())
    { }

    public DataUrl(byte[] bytes, string mimeType = default)
    {
        _url = GetDataUrl(bytes, mimeType);
    }

    public override string ToString() => _url;

    public static string GetDataUrl(byte[] bytes, string mimeType = "application/octet-stream")
    {
        return $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
    }

}