using Microsoft.CodeAnalysis;

namespace Nextended.CodeGen.Contracts;

public interface ISourceSubGenerator
{
    bool RequireConfig { get;  }
    IEnumerable<GeneratedFile> Execute(GenerationContext context);
}
