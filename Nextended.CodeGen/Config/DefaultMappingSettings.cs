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

    /// <summary>
    /// When true, only the mapping from the source type to its DTO is generated: no DTO-to-source AssignTo and no ToSource (ToNet) method,
    /// for both strategies. For DTOs that are only sent and never mapped back onto an entity. Enum casts are generated in both directions still.
    /// </summary>
    public bool SkipReverseMapping { get; set; }

    /// <summary>
    /// Mapperly strategy only. When true, the mapping of a type with derived types that have DTOs maps every instance to the DTO of its
    /// runtime type (Mapperly's MapDerivedType): an abstract type gets a mapper of its own (e.g. ShipmentBaseMapper with ToDto/ToSource),
    /// a concrete one the attributes on its mapper. Without it Mapperly cannot map a member whose type is abstract.
    /// </summary>
    public bool MapDerivedTypes { get; set; }
}