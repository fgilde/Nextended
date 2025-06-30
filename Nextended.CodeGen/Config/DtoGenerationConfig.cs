namespace Nextended.CodeGen.Config;

public class DtoGenerationConfig
{
    /// <summary>
    /// Default namespace for generated DTOs will only work when not set on Attribute layer.
    /// If null same as source will be used.
    /// (can be overwritten per class in Attribute layer)
    /// </summary>
    public string? Namespace { get; set; } = "N.CG.AutoGen";

    /// <summary>
    /// Default suffix for generated DTOs will only work when not set on Attribute layer.
    /// </summary>
    public string? Suffix { get; set; }

    /// <summary>
    /// Default prefix for generated DTOs will only work when not set on Attribute layer.
    /// (can be overwritten per class in Attribute layer)
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Default setting if the generated DTOs should be COM compatible.
    /// (can be overwritten per class in Attribute layer)
    /// </summary>
    public bool? IsComCompatible { get; set; }
    
    /// <summary>
    /// If Compatible is set to true then the generated classes are COM visible and fully com compatible.
    /// This setting represents the CLassName for the generated com id class that contains all required ids
    /// </summary>
    public string ComIdClassName { get; set; }= "ComGuids";

    /// <summary>
    /// Format string for the name of the Id property inside the generated com id class.
    /// </summary>
    public string ComIdClassPropertyFormat { get; set; } = "Id{0}";

    /// <summary>
    /// Modifier for the generated COM id class.
    /// </summary>
    public string ComIdClassModifier { get; set; } = "internal";

    /// <summary>
    /// Default Modifier for generated DTO classes. (can be overwritten per class in Attribute layer)
    /// </summary>
    public string ClassModifier { get; set; } = "public";

    /// <summary>
    /// Default Modifier for generated DTO interfaces. (can be overwritten per class in Attribute layer)
    /// </summary>
    public string InterfaceModifier { get; set; } = "public";

    public bool OneFilePerClass { get; set; } = false;
    public string[]? Usings { get; set; }
    public bool CreateRegions = true;
    public bool CreateComments = true;
    public bool GeneratePartial = true;
}