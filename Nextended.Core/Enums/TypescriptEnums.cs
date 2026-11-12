namespace Nextended.Core.Enums;

/// <summary>How the server writes property names.</summary>
public enum TypescriptNaming
{
    /// <summary>JsonNamingPolicy.CamelCase (the ASP.NET Core default).</summary>
    CamelCase,

    /// <summary>The C# name as it is (no naming policy).</summary>
    AsIs
}

/// <summary>How the server writes enum values.</summary>
public enum TypescriptEnumValues
{
    /// <summary>As numbers (the System.Text.Json default without a converter); the same as <see cref="Number"/>.</summary>
    Default,

    /// <summary>As numbers.</summary>
    Number,

    /// <summary>As names (JsonStringEnumConverter).</summary>
    String
}

/// <summary>How an enum is written in TypeScript.</summary>
public enum TypescriptEnumStyle
{
    /// <summary><c>export enum State { Draft = 0 }</c></summary>
    Enum,

    /// <summary><c>export const State = { Draft: 0 } as const;</c> plus <c>export type State = ...</c> (fits <c>erasableSyntaxOnly</c>).</summary>
    Const
}

/// <summary>Where the generated files go.</summary>
public enum TypescriptLayout
{
    /// <summary>One folder per namespace (without <c>RootNamespace</c>).</summary>
    Namespace,

    /// <summary>All files in the output folder.</summary>
    Flat
}
