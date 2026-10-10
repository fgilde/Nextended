namespace Nextended.Core.Enums;

/// <summary>
/// Specifies how the mapping between a source type and its generated DTO is generated.
/// </summary>
public enum DtoMappingStrategy
{
    /// <summary>Uses the strategy from the generator configuration, or <see cref="Extensions"/> when none is configured.</summary>
    Default,

    /// <summary>Generates plain extension methods (AssignTo, ToDto, ToNet) in the partial MappingExtensions class.</summary>
    Extensions,

    /// <summary>
    /// Generates a partial Riok.Mapperly mapper class (e.g. ProductMapper) that Mapperly implements.
    /// Requires a reference to Riok.Mapperly and files written to disk (OutputPath / MappingOutputPath).
    /// Every method or the [Mapper] attribute you declare in your own part of the class replaces the generated one.
    /// </summary>
    Mapperly
}
