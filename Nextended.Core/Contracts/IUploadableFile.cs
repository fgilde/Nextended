using System.Net.Http;
using System.Threading.Tasks;

namespace Nextended.Core.Contracts;

/// <summary>
/// Interface representing a file that can be uploaded, supporting both local file paths and remote URLs.
/// </summary>
public interface IUploadableFile
{
    /// <summary>
    /// Gets or sets the name of the file.
    /// </summary>
    public string FileName { get; set; }
    
    /// <summary>
    /// Gets or sets the file extension (e.g., ".txt", ".jpg").
    /// </summary>
    public string Extension { get; set; }
    
    /// <summary>
    /// Gets or sets the MIME content type of the file (e.g., "text/plain", "image/jpeg").
    /// </summary>
    public string ContentType { get; set; }
    
    /// <summary>
    /// Gets or sets the binary data of the file.
    /// </summary>
    public byte[] Data { get; set; }
    
    /// <summary>
    /// Gets or sets the URL from which the file can be downloaded.
    /// </summary>
    public string Url { get; set; }
    
    /// <summary>
    /// Gets or sets the local file system path to the file.
    /// </summary>
    public string Path { get; set; }
    
    /// <summary>
    /// Gets or sets the size of the file in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Ensures that the file data is loaded. If the data is not yet loaded and a URL is available,
    /// downloads the file from the URL.
    /// </summary>
    /// <param name="client">Optional HttpClient to use for downloading. If null, a default client is used.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task EnsureDataLoadedAsync(HttpClient client = null);
}