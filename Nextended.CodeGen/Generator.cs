using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Nextended.CodeGen;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Contracts;
using Nextended.CodeGen.Generators.DtoGeneration;
using Nextended.CodeGen.Helper;


[Generator]
public class MainGenerator : ISourceGenerator
{
    public static readonly Guid BuildId = new Guid("44348419-f441-4ea5-8bc6-3178858c720e");
    private bool attachDebugger = false;
    private bool generationEnabled = true;
    private DateTime LastGenerated = DateTime.MinValue;
    private bool allowWithoutConfig = false;
    private List<ISourceSubGenerator> _generators;

    public void Initialize(GeneratorInitializationContext context)
    {
        Console.WriteLine("Nextended.CodeGen Initialize");
        _generators = GetType().Assembly.GetTypes().Where(t => typeof(ISourceSubGenerator).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => (ISourceSubGenerator)Activator.CreateInstance(t))
            .ToList();
    }

    public void Execute(GeneratorExecutionContext context)
    {
        Console.WriteLine("Execute with Build " + BuildId);
        if (!generationEnabled)
            return;

        LastGenerated = DateTime.Now;
        if (attachDebugger && !System.Diagnostics.Debugger.IsAttached)
            System.Diagnostics.Debugger.Launch();

        if (!context.AdditionalFiles.Any())
        {
            Console.WriteLine("Nextended.CodeGen didnt find an config file");
            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("NCG001", "Info", "Did not find additional files", "Nextended.CodeGen", DiagnosticSeverity.Info, true), Location.None));
            if(!allowWithoutConfig)
                return;
        }

        ExecuteGeneration(context);
        LastGenerated = DateTime.Now;
        context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("NCG003", "Info", $"Generation completed at {LastGenerated}", "Nextended.CodeGen", DiagnosticSeverity.Info, true), Location.None));
    }

    private void ExecuteGeneration(GeneratorExecutionContext context)
    {
        var generators = _generators.ToList();
        bool executed = false;
        var additionalFiles = context.AdditionalFiles;
        new DtoGenerator(new DtoGenerationConfig()).Execute(context);

        foreach (var configFile in additionalFiles.Where(text => text?.Path?.ToLower()?.EndsWith(".json") == true))
        {
            Console.WriteLine("Nextended.CodeGen generate for " + configFile.Path);

            try
            {
                var json = configFile.GetText()?.ToString();
                var config = JsonConvert.DeserializeObject<MainConfig>(json);
                var ctx = new GenerationContext(configFile, context, config);
                if(Execute(generators, ctx))
                    executed = true;
                foreach (var sourceSubGenerator in _generators.Where(g => !g.RequireConfig))
                    generators.Remove(sourceSubGenerator); // remove generators that do not require a config, so they are not executed again

            }
            catch (Exception)
            {
                //context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("NCG002", "Warning", $"Could not proceed configuration {configFile.Path}", "Nextended.CodeGen", DiagnosticSeverity.Warning, true), Location.None));
            }
        }

        if (!executed)
        {
            var ctx = new GenerationContext(new NamespaceResolver("", context), context, null);
            Execute(generators.Where(e => !e.RequireConfig), ctx);
        }
    }

    private bool Execute(IEnumerable<ISourceSubGenerator> generators, GenerationContext context)
    {
        var generatedFiles = new List<GeneratedFile>();
        foreach (var sourceSubGenerator in generators)
        {
            try
            {
                generatedFiles.AddRange(sourceSubGenerator.Execute(context));
            }
            catch (Exception e)
            {
                Console.WriteLine("Nextended.CodeGen ERROR: " + e.Message);
            }
        }
       // var generatedFiles = Task.WhenAll(generators.Select(g => Task.Run(() => g.Execute(context)))).Result.SelectMany(f => f).ToList();
        WriteOrAddFiles(context, generatedFiles);
        return generatedFiles.Any();
    }

    private void WriteOrAddFiles(GenerationContext context, IEnumerable<GeneratedFile> generatedFiles)
    {
        foreach (var generatedFile in generatedFiles.Where(f => f != null))
        {
            Console.WriteLine("Nextended.CodeGen save generated file: "+ generatedFile.FileName);

            if (string.IsNullOrWhiteSpace(generatedFile.OutputPath))
                context.ExecutionContext.AddSource(generatedFile.FileName, generatedFile.Content);
            else
            {
                var path = NamespaceResolver.GetAbsolutePath(generatedFile.OutputPath!, context.AdditionalFile.Path);

                if (Directory.Exists(path) || path.EndsWith(Path.DirectorySeparatorChar.ToString()) || path.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                {
                    path = Path.Combine(path, generatedFile.FileName);
                }

                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string lineEnding = Environment.NewLine;
                
                File.WriteAllText(path, generatedFile.Content
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n")
                    .Replace("\n", lineEnding));
            }
        }
    }
}




























































































































































































