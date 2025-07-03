namespace Nextended.CodeGen.Contracts;

public class GeneratedFile(string fileName, string ns, string content, IEnumerable<string> usings)
{
    public string FileName { get; } = fileName;
    public string Namespace { get; } = ns;
    public string Content { get; } = content;
    public IEnumerable<string> Usings { get; } = usings;
}