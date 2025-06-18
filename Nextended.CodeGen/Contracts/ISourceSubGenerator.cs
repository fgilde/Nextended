using Microsoft.CodeAnalysis;

namespace Nextended.CodeGen.Contracts;

public interface ISourceSubGenerator<TSettings>
{
    void Execute(
        GeneratorExecutionContext context,
        IEnumerable<TSettings> settings,
        AdditionalText additionalFile);
}
