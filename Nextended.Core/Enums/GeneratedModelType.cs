using System;

namespace Nextended.Core.Enums;

/// <summary>
/// Specifies the type of model to generate during code generation (class, struct, record, or record struct).
/// </summary>
public enum GeneratedModelType
{
    Unset,
    Class,
    Struct,
    Record,
    RecordStruct
}

public static class GeneratedModelTypeExtensions
{
    public static string ToCSharpKeyword(this GeneratedModelType? modifier)
    {
        return modifier.HasValue ? modifier.Value.ToCSharpKeyword() : "class";
    }
    public static string ToCSharpKeyword(this GeneratedModelType modifier)
    {
        return modifier switch
        {
            GeneratedModelType.Unset => "class",
            GeneratedModelType.Class => "class",
            GeneratedModelType.Struct => "struct",
            GeneratedModelType.Record => "record",
            GeneratedModelType.RecordStruct => "record struct",
            _ => throw new ArgumentOutOfRangeException(nameof(modifier), modifier, null)
        };
    }
}