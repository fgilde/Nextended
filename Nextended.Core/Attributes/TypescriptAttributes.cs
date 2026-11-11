using System;

namespace Nextended.Core.Attributes;

/// <summary>
/// Puts the type into every TypeScript generation of the project (a "Typescript" entry in codegen.json or <c>Typescript(...)</c> in a
/// fluent configuration; without one nothing is generated).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface, Inherited = false)]
public class GenerateTypescriptAttribute : Attribute
{
    /// <summary>The TypeScript name of the type; the C# name when not set.</summary>
    public string? Name { get; set; }
}

/// <summary>Leaves a type or a member out of the generated TypeScript (the JSON stays as it is).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Field)]
public class TypescriptIgnoreAttribute : Attribute
{ }

/// <summary>TypeScript settings of one member.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class TypescriptPropertyAttribute : Attribute
{
    /// <summary>The member name in TypeScript; by default the JSON name ([JsonPropertyName] or the naming policy).</summary>
    public string? Name { get; set; }

    /// <summary>The TypeScript type, written as it is (e.g. <c>"string"</c>, <c>"Record&lt;string, number&gt;"</c>).</summary>
    public string? Type { get; set; }

    /// <summary>Writes the member as optional (<c>name?: ...</c>).</summary>
    public bool Optional { get; set; }
}
