namespace Nextended.Core.Enums;

/// <summary>
/// Specifies the accessor types for interface properties (get, set, or both).
/// </summary>
public enum InterfaceProperty
{
    Unset,
    GetAndSet,
    Get, 
    Set,
}

public static class InterfacePropertyExtensions
{
    public static string ToCSharpKeyword(this InterfaceProperty property)
    {
        return property switch
        {
            InterfaceProperty.Unset => "get; set;",
            InterfaceProperty.GetAndSet => "get; set;",
            InterfaceProperty.Get => "get;",
            InterfaceProperty.Set => "set;",
            _ => ""
        };
    }
}