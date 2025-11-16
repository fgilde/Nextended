using System.Collections.Generic;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Contracts;

namespace Nextended.CodeGen.Generators.CodeToText;

public class CodeToTextGenerator : ISourceSubGenerator
{
    private List<CodeToDocsConfig> _config;
    public bool RequireConfig => true;
    public IEnumerable<GeneratedFile> Execute(GenerationContext context)
    {
       _config = context.Config.CodeToDocs; // List<CodeToDocsConfig>
        yield break;
    }
}