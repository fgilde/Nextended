using System.Collections.Generic;

namespace Nextended.CodeGen.Config;

public class MainConfig
{
    public DtoGenerationConfig? DtoGeneration { get; set; } = new();
    public List<ClassStructureCodeGenerationConfig> StructureGenerations { get; set; } = new();
    public List<ExcelGenerationConfig> ExcelGenerations { get; set; } = new();
    public List<CodeToDocsConfig> CodeToDocs { get; set; } = new();
}


