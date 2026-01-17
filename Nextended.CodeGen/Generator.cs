using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Newtonsoft.Json;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Contracts;
using Nextended.CodeGen.Helper;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Nextended.CodeGen
{
    [Generator]
    public sealed class MainGenerator : IIncrementalGenerator
    {
        public static readonly Guid BuildId = new("f0be6515-1df6-4d81-b096-91432f31fdcc");

        private bool attachDebugger = false;
        private bool generationEnabled = true;
        private bool allowWithoutConfig = false;

        private static readonly DiagnosticDescriptor InfoNoAdditionalFiles = new(
            "NCG001", "Info", "Did not find additional files",
            "Nextended.CodeGen", DiagnosticSeverity.Info, isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor InfoGenerationCompleted = new(
            "NCG003", "Info", "Generation completed at {0}",
            "Nextended.CodeGen", DiagnosticSeverity.Info, isEnabledByDefault: true);

        private static readonly Lazy<ImmutableArray<ISourceSubGenerator>> CachedSubGenerators =
            new(() =>
            {
                Console.WriteLine("Nextended.CodeGen Initialize (incremental)");
                return typeof(MainGenerator).Assembly.GetTypes()
                    .Where(t => typeof(ISourceSubGenerator).IsAssignableFrom(t) && !t.IsAbstract)
                    .Select(t => (ISourceSubGenerator)Activator.CreateInstance(t)!)
                    .ToImmutableArray();
            });

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var allAdditional = context.AdditionalTextsProvider.Collect();

            var jsonConfigs = context.AdditionalTextsProvider
                .Where(at => at.Path != null && at.Path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                .Select(static (at, ct) => new JsonConfigInput(at, at.GetText(ct)?.ToString()))
                .Collect();

            var combined = context.CompilationProvider
                .Combine(context.AnalyzerConfigOptionsProvider)
                .Combine(allAdditional)
                .Combine(jsonConfigs);

            context.RegisterSourceOutput(combined, (spc, data) =>
            {
                var (((compilation, optionsProvider), additionalFiles), configs) = data;
                ExecuteIncremental(spc, compilation, optionsProvider, additionalFiles, configs);
            });
        }

        private void ExecuteIncremental(
            SourceProductionContext context,
            Compilation compilation,
            AnalyzerConfigOptionsProvider optionsProvider,
            ImmutableArray<AdditionalText> additionalFiles,
            ImmutableArray<JsonConfigInput> jsonConfigs)
        {
            Console.WriteLine("Execute with Build " + BuildId);

            if (!generationEnabled)
                return;

            if (attachDebugger && !System.Diagnostics.Debugger.IsAttached)
                System.Diagnostics.Debugger.Launch();

            if (additionalFiles.IsDefaultOrEmpty || additionalFiles.Length == 0)
            {
                Console.WriteLine("Nextended.CodeGen didnt find an config file");
                context.ReportDiagnostic(Diagnostic.Create(InfoNoAdditionalFiles, Location.None));
                if (!allowWithoutConfig)
                    return;
            }

            // DTO Generator: erst portieren/adapter bauen, dann wieder aktivieren
            // new DtoGenerator(new DtoGenerationConfig()).Execute(...);

            var generators = CachedSubGenerators.Value.ToList();
            bool executed = false;

            foreach (var cfg in jsonConfigs)
            {
                Console.WriteLine("Nextended.CodeGen generate for " + cfg.File.Path);

                try
                {
                    var json = cfg.Content ?? cfg.File.GetText()?.ToString();
                    var config = JsonConvert.DeserializeObject<MainConfig>(json);

                    var genCtx = new GenerationContext(cfg.File, context, compilation, optionsProvider, config);

                    if (ExecuteGenerators(generators, genCtx))
                        executed = true;

                    // remove generators that do not require a config, so they are not executed again
                    foreach (var g in CachedSubGenerators.Value.Where(g => !g.RequireConfig))
                        generators.Remove(g);
                }
                catch (Exception e)
                {
                    // Verhalten wie vorher: Diagnostic war auskommentiert
                    Console.WriteLine("Nextended.CodeGen config error: " + e.Message);
                }
            }

            if (!executed)
            {
                var fallbackResolver = new NamespaceResolver("", compilation, optionsProvider);
                var fallbackCtx = new GenerationContext(fallbackResolver, context, compilation, optionsProvider, null);

                ExecuteGenerators(generators.Where(g => !g.RequireConfig), fallbackCtx);
            }

            context.ReportDiagnostic(Diagnostic.Create(InfoGenerationCompleted, Location.None, DateTime.Now));
        }

        private bool ExecuteGenerators(IEnumerable<ISourceSubGenerator> generators, GenerationContext context)
        {
            var generatedFiles = new List<GeneratedFile>();

            foreach (var gen in generators)
            {
                try
                {
                    generatedFiles.AddRange(gen.Execute(context));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Nextended.CodeGen ERROR: " + e.Message);
                }
            }

            WriteOrAddFiles(context, generatedFiles);
            return generatedFiles.Any();
        }

        private void WriteOrAddFiles(GenerationContext context, IEnumerable<GeneratedFile> generatedFiles)
        {
            foreach (var generatedFile in generatedFiles.Where(f => f != null))
            {
                Console.WriteLine("Nextended.CodeGen save generated file: " + generatedFile.FileName);

                if (string.IsNullOrWhiteSpace(generatedFile.OutputPath))
                {
                    context.AddSource(generatedFile.FileName, generatedFile.Content);
                }
                else
                {
                    var path = NamespaceResolver.GetAbsolutePath(generatedFile.OutputPath!, context.AdditionalFile.Path);

                    if (Directory.Exists(path) ||
                        path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                        path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                    {
                        path = Path.Combine(path, generatedFile.FileName);
                    }

                    var directory = Path.GetDirectoryName(path);
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory!);

                    string lineEnding = Environment.NewLine;

                    var contents = generatedFile.Content
                        .Replace("\r\n", "\n")
                        .Replace("\r", "\n")
                        .Replace("\n", lineEnding);

                    if (File.Exists(path))
                    {
                        var contentsFromFile = File.ReadAllText(path);
                        if (contentsFromFile == contents)
                        {
                            continue;
                        }
                    }
                    File.WriteAllText(path, contents);
                }
            }
        }

        private readonly record struct JsonConfigInput(AdditionalText File, string? Content)
        {
            public AdditionalText File { get; } = File;
            public string? Content { get; } = Content;
        }
    }
}







