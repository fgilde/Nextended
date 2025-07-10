using Nextended.Core.Enums;

namespace Nextended.CodeGen.Config;

public class ClassStructureCodeGenerationConfig : CodeGenerationConfigBase
{
    public string RootClassName { get; set; }
    public string SourceFile { get; set; }
    public string[] Ignore { get; set; }
    public JsonArrayGeneration ArrayGeneration { get; set; }
}
