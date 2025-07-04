using Microsoft.CodeAnalysis;

namespace Nextended.CodeGen.Contracts;

public interface ISourceSubGenerator<TSettings>
{
    IEnumerable<GeneratedFile> Execute(GenerationContext context);
}
