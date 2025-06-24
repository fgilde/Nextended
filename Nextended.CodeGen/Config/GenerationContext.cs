using Microsoft.CodeAnalysis;
using Nextended.CodeGen.Helper;

namespace Nextended.CodeGen.Config;

public class GenerationContext(
    NamespaceResolver namespaceResolver,
    GeneratorExecutionContext executionContext,
    CodeGenConfig config)
{
    public NamespaceResolver NamespaceResolver { get; } = namespaceResolver;
    public GeneratorExecutionContext ExecutionContext { get; } = executionContext;
    public CodeGenConfig Config { get; } = config;
}