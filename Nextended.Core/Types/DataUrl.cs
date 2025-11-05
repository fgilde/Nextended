using System;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nextended.Core.Types;

/// <summary>
/// Represents a data URL (RFC 2397) that encodes binary data in a base64 string with an optional MIME type.
/// </summary>
public class DataUrl
{
    private readonly string _url;
    private const string _pattern = @"^data:([\w/]+);base64,(.+)$";

    /// <summary>
    /// Gets the binary data encoded in the data URL.
    /// </summary>
    public byte[] Bytes { get; }
    
    /// <summary>
    /// Gets the MIME type of the data.
    /// </summary>
    public string MimeType { get; }

    /// <summary>
    /// Initializes a new instance of the DataUrl class with the specified bytes and MIME type.
    /// </summary>
    /// <param name="bytes">The binary data to encode.</param>
    /// <param name="mimeType">The MIME type of the data.</param>
    public DataUrl(byte[] bytes, ContentType mimeType) : this(bytes, mimeType.ToString())
    { }

    /// <summary>
    /// Initializes a new instance of the DataUrl class with the specified bytes and optional MIME type.
    /// </summary>
    /// <param name="bytes">The binary data to encode.</param>
    /// <param name="mimeType">The MIME type of the data. If null, defaults to application/octet-stream.</param>
    public DataUrl(byte[] bytes, string mimeType = default)
    {
        _url = GetDataUrl(Bytes = bytes, MimeType = mimeType);
    }

    /// <summary>
    /// Initializes a new instance of the DataUrl class by parsing an existing data URL string.
    /// </summary>
    /// <param name="url">The data URL string to parse.</param>
    public DataUrl(string url)
    {
        var data = ReadFromDataUrl(url);
        Bytes = data.bytes;
        MimeType = data.mime;
        _url = url;
    }

    public override string ToString() => _url;

    private static (byte[] bytes, string mime) ReadFromDataUrl(string url)
    {
        var match = Regex.Match(url, _pattern);
        if (!match.Success)
            throw new ArgumentException("Invalid data url", nameof(url));
        var mime = match.Groups[1].Value;
        var base64 = match.Groups[2].Value;
        var bytes = Convert.FromBase64String(base64);
        return (bytes, mime);
    }

    public static bool TryParse(string url, out DataUrl dataUrl)
    {
        try
        {
            dataUrl = Parse(url);
            return dataUrl != null;
        }
        catch
        {
            dataUrl = null;
            return false;
        }
    }

    public static DataUrl? Parse(string url)
    {
        return IsDataUrl(url) ? new DataUrl(url) : null;
    }

    public static bool IsDataUrl(string url)
    {
        // Use a regular expression to check if the URL is a valid data URL
        var regex = new Regex(_pattern);
        var match = !string.IsNullOrEmpty(url) ? regex.Match(url) : null;
        return match?.Success ?? false;
    }

    public static Task<string> GetDataUrlAsync(byte[] bytes, string mimeType = "application/octet-stream")
    {
        return Task.Run(() => GetDataUrl(bytes, mimeType));
    }

    public static string GetDataUrl(byte[] bytes, string mimeType = "application/octet-stream")
    {
        return $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
    }

}