namespace Nextended.CodeGen.Enums;

public enum InterfaceProperty
{
    Unset,
    GetAndSet,
    Get, 
    Set,
}

internal static class InterfacePropertyExtensions
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