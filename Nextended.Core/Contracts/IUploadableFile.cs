namespace Nextended.Core.Contracts;

public interface IUploadableFile
{
    public string FileName { get; set; }
    public string Extension { get; set; }
    public string ContentType { get; set; }
    public byte[] Data { get; set; }
    public string Url { get; set; }
}