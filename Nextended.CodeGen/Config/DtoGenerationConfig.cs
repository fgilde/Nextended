using Nextended.CodeGen.Enums;

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
    public Modifier ComIdClassModifier { get; set; } = Modifier.Internal;


    /// <summary>
    /// Default Sets the base type for all the generated DTO class when not overriden.
    /// </summary>
    public string? BaseType { get; set; }
    

    /// <summary>
    /// Gets or sets a value indicating whether to include `using` directives for namespaces
    /// referenced by the properties of the generated DTOs.
    /// </summary>
    public bool? AddReferencedNamespacesUsings { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include `using` directives for namespaces
    /// </summary>
    public bool? AddContainingNamespaceUsings { get; set; }

    /// <summary>
    /// A string that will be added before the generated interface, useful for adding attributes or something.
    /// </summary>
    public string? PreInterfaceString { get; set; }

    /// <summary>
    /// A string that will be added before the generated class, useful for adding attributes or something.
    /// </summary>
    public string? PreClassString { get; set; }

    /// <summary>
    /// If set to true, the generated classes will keep the attributes from the original class.
    /// </summary>
    public bool KeepAttributesOnGeneratedClass { get; set; }

    /// <summary>
    /// If set to true, the generated interfaces will keep the attributes from the original class.
    /// </summary>
    public bool KeepAttributesOnGeneratedInterface { get; set; }

    /// <summary>
    /// Adds interfaces to all generated DTO class and generated interface where setting is not overwritten.
    /// </summary>
    public string[]? Interfaces { get; set; }

    /// <summary>
    /// Default mapping generation settings.
    /// </summary>
    public DefaultMappingSettings? DefaultMappingSettings { get; set; }

    public bool OneFilePerClass { get; set; } = true;
    public string[]? Usings { get; set; }
    public bool CreateRegions { get; set; } = true;
    public bool CreateComments { get; set; } = true;
    public bool GeneratePartial { get; set; } = true;
}


public class DefaultMappingSettings
{
    public bool? MapWithClassMapper { get; set; }

    public string? ToSourceMethodName { get; set; }
    public string? ToDtoMethodName { get; set; }
}