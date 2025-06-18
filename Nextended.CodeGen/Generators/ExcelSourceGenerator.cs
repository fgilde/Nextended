using Microsoft.CodeAnalysis;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Contracts;

namespace Nextended.CodeGen.Generators;

public class ExcelSourceGenerator : ISourceSubGenerator<ExcelFileConfig>
{
    public void Execute(
        GeneratorExecutionContext context,
        IEnumerable<ExcelFileConfig> settings,
        AdditionalText additionalFile)
    {
        foreach (var cfg in settings)
        {
           
        }
    }
}