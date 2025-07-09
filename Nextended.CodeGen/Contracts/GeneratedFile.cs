using Nextended.CodeGen.Config;

namespace Nextended.CodeGen.Contracts;

public class GeneratedFile
{
    public GeneratedFile(string fileName, string ns, string content, string? outputPath = null)
    {
        FileName = fileName;
        Namespace = ns;
        Content = content;
        OutputPath = outputPath;
    }

    public GeneratedFile(string fileName, string ns, string content, CodeGenerationConfigBase? config)
        : this(fileName, ns, content, config?.OutputPath)
    {}

    /// <summary>
    /// The path where the generated file will be saved.
    /// By default it's null and will be added to the generated code context.
    /// If a path is set, the file will be saved to that path and NOT added to the generated code context.
    /// </summary>
    public string? OutputPath { get; set; }
    
    public string FileName { get; }
    public string Namespace { get; }
    public string Content { get; }
}