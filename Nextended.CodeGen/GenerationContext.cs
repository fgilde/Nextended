using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Helper;
using System;

namespace Nextended.CodeGen;

public sealed class GenerationContext
{
    // ======== Classic (ISourceGenerator) ========

    public GenerationContext(
        AdditionalText additionalFile,
        GeneratorExecutionContext executionContext,
        MainConfig? config)
    {
        NamespaceResolver = new NamespaceResolver(additionalFile, executionContext);
        AdditionalFile = additionalFile;
        ExecutionContext = executionContext;
        Config = config;
    }

    public GenerationContext(
        NamespaceResolver namespaceResolver,
        GeneratorExecutionContext executionContext,
        MainConfig? config)
    {
        NamespaceResolver = namespaceResolver;
        ExecutionContext = executionContext;
        Config = config;
    }

    // ======== Incremental (IIncrementalGenerator) ========

    public GenerationContext(
        AdditionalText additionalFile,
        SourceProductionContext productionContext,
        Compilation compilation,
        AnalyzerConfigOptionsProvider optionsProvider,
        MainConfig? config)
    {
        NamespaceResolver = new NamespaceResolver(additionalFile, compilation, optionsProvider);
        AdditionalFile = additionalFile;
        ProductionContext = productionContext;
        Compilation = compilation;
        OptionsProvider = optionsProvider;
        Config = config;
    }

    public GenerationContext(
        NamespaceResolver namespaceResolver,
        SourceProductionContext productionContext,
        Compilation compilation,
        AnalyzerConfigOptionsProvider optionsProvider,
        MainConfig? config)
    {
        NamespaceResolver = namespaceResolver;
        ProductionContext = productionContext;
        Compilation = compilation;
        OptionsProvider = optionsProvider;
        Config = config;
    }

    public NamespaceResolver NamespaceResolver { get; }
    public AdditionalText AdditionalFile { get; } = null!;

    // Classic-only
    public GeneratorExecutionContext? ExecutionContext { get; }

    // Incremental-only
    public SourceProductionContext? ProductionContext { get; }
    public Compilation? Compilation { get; }
    public AnalyzerConfigOptionsProvider? OptionsProvider { get; }

    public MainConfig? Config { get; }

    // === Helper APIs (damit du nicht hart vom Context-Typ abhängst) ===

    public void AddSource(string hintName, string content)
    {
        if (ExecutionContext is { } gec)
        {
            gec.AddSource(hintName, content);
            return;
        }

        if (ProductionContext is { } spc)
        {
            spc.AddSource(hintName, SourceText.From(content, System.Text.Encoding.UTF8));
            return;
        }

        throw new InvalidOperationException("No Roslyn context available to AddSource.");
    }

    public void ReportDiagnostic(Diagnostic diagnostic)
    {
        if (ExecutionContext is { } gec)
        {
            gec.ReportDiagnostic(diagnostic);
            return;
        }

        if (ProductionContext is { } spc)
        {
            spc.ReportDiagnostic(diagnostic);
            return;
        }

        throw new InvalidOperationException("No Roslyn context available to ReportDiagnostic.");
    }
}