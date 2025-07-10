namespace Nextended.Core.Enums;

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