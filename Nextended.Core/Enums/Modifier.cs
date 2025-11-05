using System;

namespace Nextended.Core.Enums;

/// <summary>
/// Specifies the access modifier for generated code (public, private, protected, or internal).
/// </summary>
public enum Modifier
{
    Unset,
    Public,
    Private,
    Protected,
    Internal,
}

public static class ModifierExtensions
{
    public static string ToCSharpKeyword(this Modifier? modifier)
    {
        return modifier.HasValue ? modifier.Value.ToCSharpKeyword() : "public";
    }
    public static string ToCSharpKeyword(this Modifier modifier)
    {
        return modifier switch
        {
            Modifier.Unset => "public",
            Modifier.Public => "public",
            Modifier.Private => "private",
            Modifier.Protected => "protected",
            Modifier.Internal => "internal",
            _ => throw new ArgumentOutOfRangeException(nameof(modifier), modifier, null)
        };
    }
}