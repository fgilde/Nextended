using System.Net.Http;
using System.Threading.Tasks;

namespace Nextended.Core.Contracts;

public interface IUploadableFile
{
    public string FileName { get; set; }
    public string Extension { get; set; }
    public string ContentType { get; set; }
    public byte[] Data { get; set; }
    public string Url { get; set; }
    public string Path { get; set; }
    public long Size { get; set; }

    /// <summary>
    /// If file is not loaded, loads it from Url
    /// </summary>
    public Task EnsureDataLoadedAsync(HttpClient client = null);
}