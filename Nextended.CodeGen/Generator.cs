using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Newtonsoft.Json;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Contracts;
using Nextended.CodeGen.Fluent;
using Nextended.CodeGen.Generators.DtoGeneration;
using Nextended.CodeGen.Generators.TypescriptGeneration;
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
        public static readonly Guid BuildId = new("62ae3a99-f173-424e-9b6b-3475061d3257");

        private bool attachDebugger = false;
        private bool generationEnabled = true;
        private bool allowWithoutConfig = false;

        private static readonly DiagnosticDescriptor InfoNoAdditionalFiles = new(
            "NCG001", "Info", "Did not find additional files",
            "Nextended.CodeGen", DiagnosticSeverity.Info, isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor InfoGenerationCompleted = new(
            "NCG003", "Info", "Generation completed at {0}",
            "Nextended.CodeGen", DiagnosticSeverity.Info, isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor TypescriptWithoutOutputPath = new(
            "NCG030", "TypeScript generation without OutputPath", "A TypeScript generation needs an OutputPath (the folder the .ts files go to); nothing is generated",
            "Nextended.CodeGen", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor TypescriptNotWritten = new(
            "NCG031", "TypeScript not written", "The TypeScript in {0} was not written: a source file of a generated type has syntax errors",
            "Nextended.CodeGen", DiagnosticSeverity.Info, isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor TypescriptStale = new(
            "NCG032", "Generated TypeScript is stale", "The TypeScript in {0} is not up to date ({1}); build without NextendedCodeGenTypescriptCheck to regenerate it and commit the result",
            "Nextended.CodeGen", DiagnosticSeverity.Error, isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor TypescriptFailed = new(
            "NCG033", "TypeScript generation failed", "The TypeScript in {0} could not be generated: {1}",
            "Nextended.CodeGen", DiagnosticSeverity.Warning, isEnabledByDefault: true);

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

            // a fluent configuration class makes codegen.json optional
            var fluent = FluentConfigReader.Read(compilation);

            if (additionalFiles.IsDefaultOrEmpty || additionalFiles.Length == 0)
            {
                Console.WriteLine("Nextended.CodeGen didnt find an config file");
                context.ReportDiagnostic(Diagnostic.Create(InfoNoAdditionalFiles, Location.None));
                if (!allowWithoutConfig && !fluent.HasProfiles)
                    return;
            }

            // DTO Generator: erst portieren/adapter bauen, dann wieder aktivieren
            // new DtoGenerator(new DtoGenerationConfig()).Execute(...);

            var generators = CachedSubGenerators.Value.ToList();
            bool executed = false;
            var generatedFiles = new List<GeneratedFile>();
            var typescript = new List<(TypescriptGenerationConfig Config, string? BasePath)>();

            foreach (var cfg in jsonConfigs)
            {
                Console.WriteLine("Nextended.CodeGen generate for " + cfg.File.Path);

                try
                {
                    var json = cfg.Content ?? cfg.File.GetText()?.ToString();
                    var config = JsonConvert.DeserializeObject<MainConfig>(json);

                    var genCtx = new GenerationContext(cfg.File, context, compilation, optionsProvider, config);

                    if (ExecuteGenerators(generators, genCtx, generatedFiles))
                        executed = true;
                    foreach (var ts in config?.Typescript ?? [])
                        if (ts != null)
                            typescript.Add((ts, genCtx.BasePath));

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

            // Excel and structure generations of the fluent configuration, relative to the project directory
            var projectCtx = new GenerationContext(new NamespaceResolver("", compilation, optionsProvider), context, compilation, optionsProvider, null);
            foreach (var ts in fluent.TypescriptGenerations)
                typescript.Add((ts, projectCtx.BasePath));
            if (fluent.ExcelGenerations.Count > 0 || fluent.StructureGenerations.Count > 0)
            {
                var fluentConfig = new MainConfig
                {
                    DtoGeneration = null,
                    ExcelGenerations = fluent.ExcelGenerations,
                    StructureGenerations = fluent.StructureGenerations,
                    CodeToDocs = []
                };
                var fluentCtx = new GenerationContext(new NamespaceResolver("", compilation, optionsProvider), context, compilation, optionsProvider, fluentConfig);
                ExecuteGenerators(CachedSubGenerators.Value.Where(g => g.RequireConfig), fluentCtx, generatedFiles);
            }

            if (!executed)
            {
                var fallbackResolver = new NamespaceResolver("", compilation, optionsProvider);
                var fallbackCtx = new GenerationContext(fallbackResolver, context, compilation, optionsProvider, null);

                ExecuteGenerators(generators.Where(g => !g.RequireConfig), fallbackCtx, generatedFiles);
            }

            // TypeScript last: it may take the DTOs generated above; it never adds sources, it writes files below its OutputPath
            if (typescript.Count > 0)
                RunTypescript(context, compilation, optionsProvider, typescript, generatedFiles);

            context.ReportDiagnostic(Diagnostic.Create(InfoGenerationCompleted, Location.None, DateTime.Now));
        }

        private void RunTypescript(SourceProductionContext context, Compilation compilation, AnalyzerConfigOptionsProvider optionsProvider,
            List<(TypescriptGenerationConfig Config, string? BasePath)> generations, List<GeneratedFile> generatedFiles)
        {
            var check = optionsProvider.GlobalOptions.TryGetValue("build_property.NextendedCodeGenTypescriptCheck", out var value)
                        && string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
            var dtos = CachedSubGenerators.Value.OfType<DtoGenerator>().FirstOrDefault()?.GeneratedDtos();
            foreach (var (config, basePath) in generations)
            {
                if (config.DisableGeneration)
                    continue;
                if (string.IsNullOrWhiteSpace(config.OutputPath))
                {
                    context.ReportDiagnostic(Diagnostic.Create(TypescriptWithoutOutputPath, Location.None));
                    continue;
                }
                var root = NamespaceResolver.GetAbsolutePath(config.OutputPath!, basePath);
                try
                {
                    var result = TypescriptGenerator.Generate(compilation, config, generatedFiles, dtos);
                    if (result.Unstable)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(TypescriptNotWritten, Location.None, root));
                        continue;
                    }
                    var differences = TypescriptWriter.Write(root, result.Files, check);
                    if (check && differences.Count > 0)
                        context.ReportDiagnostic(Diagnostic.Create(TypescriptStale, Location.None, root, string.Join(", ", differences.Take(10)) + (differences.Count > 10 ? ", ..." : "")));
                }
                catch (Exception e)
                {
                    // e.g. two target frameworks writing the same file at once: the other build writes the same content
                    context.ReportDiagnostic(Diagnostic.Create(TypescriptFailed, Location.None, root, e.Message));
                }
            }
        }

        private bool ExecuteGenerators(IEnumerable<ISourceSubGenerator> generators, GenerationContext context, List<GeneratedFile> collected)
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
            collected.AddRange(generatedFiles.Where(f => f != null));
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
                    var path = NamespaceResolver.GetAbsolutePath(generatedFile.OutputPath!, context.BasePath);

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




















































































