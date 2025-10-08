using Nextended.Core.Enums;

namespace Nextended.CodeGen.Config;

public class ClassStructureCodeGenerationConfig : CodeGenerationConfigBase
{
    public string RootClassName { get; set; } = string.Empty;
    public string SourceFile { get; set; } = string.Empty;
    public string[] Ignore { get; set; } = Array.Empty<string>();
    public JsonArrayGeneration ArrayGeneration { get; set; }
}
