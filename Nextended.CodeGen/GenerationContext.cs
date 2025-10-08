using Microsoft.CodeAnalysis;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Helper;

namespace Nextended.CodeGen;

public class GenerationContext
{
    public GenerationContext(AdditionalText additionalFile,
        GeneratorExecutionContext executionContext,
        MainConfig? config)
    {
        NamespaceResolver = new NamespaceResolver(additionalFile, executionContext);
        AdditionalFile = additionalFile;
        ExecutionContext = executionContext;
        Config = config;
    }
    public GenerationContext(NamespaceResolver namespaceResolver,
        GeneratorExecutionContext executionContext,
        MainConfig? config)
    {
        NamespaceResolver = namespaceResolver;
        ExecutionContext = executionContext;
        Config = config;
    }

    public NamespaceResolver NamespaceResolver { get; }
    public AdditionalText AdditionalFile { get; } = null!;
    public GeneratorExecutionContext ExecutionContext { get; }
    public MainConfig? Config { get; }
}