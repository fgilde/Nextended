using Nextended.Core.Enums;

namespace Nextended.CodeGen.Config;

public class DefaultMappingSettings
{
    public bool? MapWithClassMapper { get; set; }

    public string? ToSourceMethodName { get; set; }
    public string? ToDtoMethodName { get; set; }

    /// <summary>
    /// Mapping strategy for all types whose attribute leaves MappingStrategy at Default.
    /// </summary>
    public DtoMappingStrategy? Strategy { get; set; }
}