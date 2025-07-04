namespace Nextended.CodeGen.Config;

public class MainConfig
{
    public DtoGenerationConfig DtoGeneration { get; set; } = new();
    public List<ClassStructureCodeGenerationConfig> StructureGenerations { get; set; } = new();
}


