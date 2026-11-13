using System;
using System.Linq.Expressions;
using Nextended.Core.Enums;

namespace Nextended.Core.CodeGen;

/// <summary>
/// One TypeScript generation, named like an entry of "Typescript" in codegen.json. Nothing is generated without <see cref="OutputPath"/>;
/// the files are written to disk, never compiled into the project.
/// </summary>
public interface ITypescriptGeneration
{
#pragma warning disable CS1591 // named exactly like the codegen.json settings documented there
    ITypescriptGeneration DisableGeneration(bool value = true);
    ITypescriptGeneration OutputPath(string value);
    ITypescriptGeneration PropertyNaming(TypescriptNaming value);
    ITypescriptGeneration EnumValues(TypescriptEnumValues value);
    ITypescriptGeneration EnumStyle(TypescriptEnumStyle value);
    ITypescriptGeneration LongAsString(bool value = true);
    ITypescriptGeneration DecimalAsString(bool value = true);
    ITypescriptGeneration OptionalNullable(bool value = true);
    ITypescriptGeneration TypeAliases(bool value = true);
    ITypescriptGeneration Layout(TypescriptLayout value);
    ITypescriptGeneration RootNamespace(string? value);
    ITypescriptGeneration Barrel(bool value = true);
    ITypescriptGeneration FromGeneratedDtos(bool value = true);
    ITypescriptGeneration IncludeReferencedTypes(bool value = true);
    ITypescriptGeneration IncludeNamespaces(params string[] value);
    ITypescriptGeneration IncludeTypes(params string[] value);
    ITypescriptGeneration IncludeBaseTypes(params string[] value);
    ITypescriptGeneration ExcludeNamespaces(params string[] value);
    ITypescriptGeneration ExcludeTypes(params string[] value);
    ITypescriptGeneration ExcludeBaseTypes(params string[] value);
    ITypescriptGeneration Header(string? value);
    ITypescriptGeneration OptionalDefaultValues(bool value = true);
    ITypescriptGeneration IncludeMembersWithAttributes(params string[] value);
    ITypescriptGeneration IncludeTypesWithAttributes(params string[] value);
#pragma warning restore CS1591

    /// <summary>
    /// Adds the attribute to <c>IncludeTypesWithAttributes</c>: every type carrying it is generated, in the project and in referenced
    /// assemblies. With <paramref name="firstArgument"/> only where its first constructor argument has that value:
    /// <c>IncludeTypesWithAttribute&lt;GeneratedCodeAttribute&gt;("Nextended.CodeGen")</c> takes the generated DTOs of other projects,
    /// not the code of other generators (resources, clients).
    /// </summary>
    ITypescriptGeneration IncludeTypesWithAttribute<TAttribute>(string? firstArgument = null) where TAttribute : Attribute;

    /// <summary>Adds the attribute to <c>IncludeMembersWithAttributes</c>: only members carrying it are generated.</summary>
    ITypescriptGeneration IncludeMembersWithAttribute<TAttribute>() where TAttribute : Attribute;

    /// <summary>Adds the type to <c>IncludeTypes</c>.</summary>
    ITypescriptGeneration Include<T>();

    /// <summary>Adds the type to <c>IncludeTypes</c>; open generic types through <c>typeof(Page&lt;&gt;)</c>.</summary>
    ITypescriptGeneration Include(Type type);

    /// <summary>Adds the type to <c>ExcludeTypes</c>.</summary>
    ITypescriptGeneration Exclude<T>();

    /// <summary>Adds the type to <c>ExcludeTypes</c>.</summary>
    ITypescriptGeneration Exclude(Type type);

    /// <summary>Adds the type to <c>IncludeBaseTypes</c>: every type deriving from it or implementing it.</summary>
    ITypescriptGeneration IncludeDerivedFrom<T>();

    /// <summary>Adds the type to <c>IncludeBaseTypes</c>; open generic types through <c>typeof(EntityDto&lt;&gt;)</c>.</summary>
    ITypescriptGeneration IncludeDerivedFrom(Type type);

    /// <summary>Adds the type to <c>ExcludeBaseTypes</c>.</summary>
    ITypescriptGeneration ExcludeDerivedFrom<T>();

    /// <summary>Adds the type to <c>ExcludeBaseTypes</c>.</summary>
    ITypescriptGeneration ExcludeDerivedFrom(Type type);

    /// <summary>
    /// One entry of "TypeMap": the C# type (full name, generic as <c>Page`1</c> or <c>Page&lt;T&gt;</c>) and its TypeScript type,
    /// optionally with the module to import it from: <c>"PagedResultDto from '@/types/abp'"</c>. Type arguments are appended.
    /// </summary>
    ITypescriptGeneration TypeMap(string type, string typescript);

    /// <summary>One entry of "TypeMap" for <typeparamref name="T"/> (any construction maps its generic type).</summary>
    ITypescriptGeneration TypeMap<T>(string typescript);

    /// <summary>One entry of "TypeMap"; open generic types through <c>typeof(PagedResultDto&lt;&gt;)</c>.</summary>
    ITypescriptGeneration TypeMap(Type type, string typescript);

    /// <summary>The TypeScript of one type in this generation (an entry of "Types").</summary>
    ITypescriptGeneration Type<T>(Action<ITypescriptType<T>> configure);

    /// <summary>The TypeScript of one type by its full name, e.g. a generated DTO view (an entry of "Types").</summary>
    ITypescriptGeneration Type(string typeName, Action<ITypescriptTypeBuilder> configure);
}

/// <summary>The TypeScript of one type: name, members left out, member settings (named like [TypescriptProperty]).</summary>
/// <typeparam name="TSelf">The builder type, for chaining.</typeparam>
public interface ITypescriptTypeSettings<out TSelf>
{
    /// <summary>The TypeScript name of the type.</summary>
    TSelf Name(string value);

    /// <summary>Leaves the type out of the TypeScript.</summary>
    TSelf Ignore();

    /// <summary>Leaves members out of the TypeScript, by name.</summary>
    TSelf Ignore(params string[] memberNames);

    /// <summary>The TypeScript settings of one member, by name.</summary>
    ITypescriptProperty Property(string memberName);

    /// <summary>Members with a default value (initializer or [DefaultValue]) optional, for this type (overrides the generation's setting).</summary>
    TSelf OptionalDefaultValues(bool value = true);

    /// <summary>Only the members carrying one of these attributes (full or simple name, "Attribute" optional); replaces the generation's list.</summary>
    TSelf IncludeMembersWithAttributes(params string[] attributeNames);

    /// <summary>Only the members carrying <typeparamref name="TAttribute"/> (adds it to this type's <c>IncludeMembersWithAttributes</c>).</summary>
    TSelf IncludeMembersWithAttribute<TAttribute>() where TAttribute : Attribute;

    /// <summary>
    /// The type becomes a union of these derived types (full or simple names, <c>*</c> globs; <c>"*"</c> = every non-abstract derived type
    /// found), for a base type without [JsonDerivedType]. <c>"Name=value"</c> gives the derived type a discriminator value.
    /// </summary>
    TSelf DerivedTypes(params string[] typeNames);

    /// <summary>Adds <typeparamref name="TDerived"/> to <c>DerivedTypes</c>, optionally with the discriminator value the server writes.</summary>
    TSelf DerivedType<TDerived>(string? discriminator = null);

    /// <summary>Adds the type to <c>DerivedTypes</c>, optionally with the discriminator value the server writes.</summary>
    TSelf DerivedType(Type type, string? discriminator = null);

    /// <summary>The name of the discriminator property of the union (<c>$type</c> when not set and a derived type has a value).</summary>
    TSelf Discriminator(string propertyName);
}

/// <summary>The TypeScript of a type given by name.</summary>
public interface ITypescriptTypeBuilder : ITypescriptTypeSettings<ITypescriptTypeBuilder>;

/// <summary>The TypeScript of <typeparamref name="T"/>, with member selectors.</summary>
public interface ITypescriptType<T> : ITypescriptTypeSettings<ITypescriptType<T>>
{
    /// <summary>Leaves members out of the TypeScript.</summary>
    ITypescriptType<T> Ignore(params Expression<Func<T, object?>>[] members);

    /// <summary>The TypeScript settings of one member.</summary>
    ITypescriptProperty Property<TProperty>(Expression<Func<T, TProperty>> member);
}

/// <summary>The TypeScript settings of one member, named like the properties of [TypescriptProperty].</summary>
public interface ITypescriptProperty
{
    /// <summary>The member name in TypeScript (by default the JSON name).</summary>
    ITypescriptProperty Name(string value);

    /// <summary>The TypeScript type, written as it is.</summary>
    ITypescriptProperty Type(string value);

    /// <summary>Writes the member as optional (<c>name?: ...</c>).</summary>
    ITypescriptProperty Optional(bool value = true);

    /// <summary>Leaves the member out of the TypeScript.</summary>
    ITypescriptProperty Ignore();

    /// <summary>Takes the member in again that [TypescriptIgnore] or [JsonIgnore] leave out.</summary>
    ITypescriptProperty Include();
}
