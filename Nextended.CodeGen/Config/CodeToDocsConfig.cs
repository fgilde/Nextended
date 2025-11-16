using System.ComponentModel;

namespace Nextended.CodeGen.Config;

public class CodeToDocsConfig
{ 
    public bool DisableGeneration { get; set; }
    public string InputFolder { get; set; }
    public string? OutputPath { get; set; }
    public string FileIncludePattern { get; set; }
    public SourceExportType[] SourceExportTypes { get; set; } 
}

public enum SourceExportType
{
    [Description(".txt")]
    PlainText,
    [Description(".md")]
    Markdown,
    [Description(".html")]
    Html
}