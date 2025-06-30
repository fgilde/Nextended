namespace Nextended.CodeGen.Generators.DtoGeneration;

public class DtoGeneratedFile(string fileName, string ns, string content, IEnumerable<string> usings)
{
    public string FileName { get; } = fileName;
    public string Namespace { get; } = ns;
    public string Content { get; } = content;
    public IEnumerable<string> Usings { get; } = usings;
}