using System.Collections.Generic;

namespace Nextended.CodeGen.Config;

/// <summary>
/// One TypeScript generation: an entry of "Typescript" in codegen.json or a fluent <c>Typescript(t => ...)</c>.
/// The files are written below <see cref="OutputPath"/>, never added to the compilation; without OutputPath nothing happens.
/// </summary>
public class TypescriptGenerationConfig
{
    public bool DisableGeneration { get; set; }

    /// <summary>The folder the .ts files go to (required); relative to the json file or the project directory.</summary>
    public string? OutputPath { get; set; }

    public TypescriptPreset Preset { get; set; }
    public TypescriptNaming PropertyNaming { get; set; }
    public TypescriptEnumValues EnumValues { get; set; }
    public TypescriptEnumStyle EnumStyle { get; set; }

    /// <summary><c>long</c>/<c>ulong</c> (and 128-bit integers) as <c>string</c>, for servers that write them as strings.</summary>
    public bool LongAsString { get; set; }

    /// <summary><c>decimal</c> as <c>string</c>.</summary>
    public bool DecimalAsString { get; set; }

    /// <summary>Nullable members also optional (<c>name?: T | null</c>).</summary>
    public bool OptionalNullable { get; set; }

    /// <summary>Guid and date/time members typed by aliases (<c>Guid</c>, <c>DateTime</c>, ... = <c>string</c>) from <c>scalars.ts</c>.</summary>
    public bool TypeAliases { get; set; }

    public TypescriptLayout Layout { get; set; }

    /// <summary>The namespace prefix left out of the folders (namespace layout).</summary>
    public string? RootNamespace { get; set; }

    /// <summary>Writes an <c>index.ts</c> that re-exports every file.</summary>
    public bool Barrel { get; set; } = true;

    /// <summary>Also the DTOs this generator generates in the same run (also those only added to the compilation).</summary>
    public bool FromGeneratedDtos { get; set; }

    /// <summary>Types that selected types use (members, base types, derived types of unions) are generated too; otherwise <c>unknown</c>.</summary>
    public bool IncludeReferencedTypes { get; set; } = true;

    /// <summary>Namespaces whose types are generated (the namespace and below; also in referenced assemblies).</summary>
    public List<string> IncludeNamespaces { get; set; } = [];

    /// <summary>Types by full or simple name; <c>*</c> matches any text (<c>*Dto</c>, <c>MyApp.Contracts.*</c>).</summary>
    public List<string> IncludeTypes { get; set; } = [];

    /// <summary>Types deriving from or implementing one of these (full names, generic as <c>EntityDto`1</c> or <c>EntityDto&lt;T&gt;</c>).</summary>
    public List<string> IncludeBaseTypes { get; set; } = [];

    public List<string> ExcludeNamespaces { get; set; } = [];
    public List<string> ExcludeTypes { get; set; } = [];
    public List<string> ExcludeBaseTypes { get; set; } = [];

    /// <summary>C# type (full name) → TypeScript type, optionally <c>"Name from 'module'"</c>; type arguments are appended.</summary>
    public Dictionary<string, string> TypeMap { get; set; } = new();

    /// <summary>Settings per type (full name).</summary>
    public Dictionary<string, TypescriptTypeConfig> Types { get; set; } = new();

    /// <summary>Set for a fluent Typescript(...): its "Types" win over the attributes (json "Types" lose to them).</summary>
    [Newtonsoft.Json.JsonIgnore]
    public bool IsFluent { get; set; }
}

/// <summary>The TypeScript of one type.</summary>
public class TypescriptTypeConfig
{
    public string? Name { get; set; }
    public bool? Ignore { get; set; }
    public Dictionary<string, TypescriptMemberConfig> Members { get; set; } = new();

    public TypescriptMemberConfig Member(string name)
    {
        if (!Members.TryGetValue(name, out var member))
            Members[name] = member = new TypescriptMemberConfig();
        return member;
    }
}

/// <summary>The TypeScript of one member.</summary>
public class TypescriptMemberConfig
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public bool? Optional { get; set; }
    public bool? Ignore { get; set; }
}

// the same values as Nextended.Core.Enums.Typescript* (the generator references an older Nextended.Core)
public enum TypescriptPreset { None, Abp, Coworkee }
public enum TypescriptNaming { CamelCase, AsIs }
public enum TypescriptEnumValues { Default, Number, String }
public enum TypescriptEnumStyle { Enum, Const }
public enum TypescriptLayout { Namespace, Flat }
