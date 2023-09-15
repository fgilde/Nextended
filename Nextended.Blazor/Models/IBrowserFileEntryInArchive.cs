using Microsoft.AspNetCore.Components.Forms;

namespace Nextended.Blazor.Models;

public interface IBrowserFileEntryInArchive : IBrowserFile
{
    byte[] FileBytes { get; }
    public string FullName { get; }
    public string Path { get; }
    public bool IsDirectory { get; }
    public string[] PathArray { get; }
    public string ParentDirectoryName { get; }
}