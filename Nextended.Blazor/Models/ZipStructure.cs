namespace Nextended.Blazor.Models;

[Obsolete("Use Nextended.Blazor.Models.ArchiveStructure instead")]
public class ZipStructure : ArchiveStructure
{

    public ZipStructure(ZipBrowserFile browserFile)
        : base(browserFile)
    { }

    public ZipStructure(string name) : base(name)
    { }

}