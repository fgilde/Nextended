using Microsoft.CodeAnalysis;

namespace Nextended.CodeGen.Contracts;

/// <summary>A DTO the DTO generator generated: its source type and its metadata name (<c>Ns.Name</c>, <c>Ns.Name`1</c>).</summary>
public sealed class GeneratedDto(INamedTypeSymbol source, string metadataName)
{
    public INamedTypeSymbol Source { get; } = source;
    public string MetadataName { get; } = metadataName;
}
