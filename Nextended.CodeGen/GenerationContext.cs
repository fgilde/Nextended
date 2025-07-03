using Microsoft.CodeAnalysis;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Helper;

namespace Nextended.CodeGen;

public class GenerationContext(
    NamespaceResolver namespaceResolver,
    GeneratorExecutionContext executionContext,
    MainConfig config)
{
    public NamespaceResolver NamespaceResolver { get; } = namespaceResolver;
    public GeneratorExecutionContext ExecutionContext { get; } = executionContext;
    public MainConfig Config { get; } = config;
}