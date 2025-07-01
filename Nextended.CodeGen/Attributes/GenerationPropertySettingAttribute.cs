using Nextended.CodeGen.Enums;

namespace Nextended.CodeGen.Attributes;

/// <summary>
/// Can be applied to a property or field of a class that uses the AutoGenerateComAttribute
/// to specify the type to be used for the COM interface and the COM class, or to provide custom names.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class GenerationPropertySettingAttribute : Attribute
{

    /// <summary>        
    /// The name to be used for the property in the COM interface and COM class.
    /// </summary>
    public string PropertyName { get; set; }

    /// <summary>
    /// If this property is set to true, the property will use the class mapper
    /// when automatic .NET mapping is generated for the class.
    /// </summary>
    public bool? MapWithClassMapper { get; set; }

    public InterfaceProperty InterfaceAccess { get; set; }

    /// <summary>
    /// A string that will be added before the generated property on the interface, useful for adding attributes or something.
    /// </summary>
    public string PreInterfaceString { get; set; }

    /// <summary>
    /// A string that will be added before the generated property on the generated class, useful for adding attributes or something.
    /// </summary>
    public string PreClassString { get; set; }

    /// <summary>
    /// If set to true, the generated classes will keep the attributes from the original class.
    /// </summary>
    public bool KeepAttributesOnGeneratedClass { get; set; }

    /// <summary>
    /// If set to true, the generated interfaces will keep the attributes from the original class.
    /// </summary>
    public bool KeepAttributesOnGeneratedInterface { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerationPropertySettingAttribute"/> class.
    /// </summary>
    public GenerationPropertySettingAttribute()
    { }


    /// <summary>
    /// Initializes a new instance of the <see cref="GenerationPropertySettingAttribute"/> class
    /// with the specified property name for the COM interface/class.
    /// </summary>
    public GenerationPropertySettingAttribute(string comPropertyName)
    {
        PropertyName = comPropertyName;
    }
}
