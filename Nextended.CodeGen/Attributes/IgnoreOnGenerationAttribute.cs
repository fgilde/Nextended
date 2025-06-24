namespace Nextended.CodeGen.Attributes;

/// <summary>
/// Kann bei einer Klasse die das AutoGenerateComAttribute enthält auf eine Property gesetzt werden diese für die COM Generierung zu ignorieren    
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class IgnoreOnGenerationAttribute : Attribute
{ }