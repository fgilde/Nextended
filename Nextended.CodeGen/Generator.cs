using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Nextended.CodeGen;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Contracts;
using Nextended.CodeGen.Generators;
using Nextended.CodeGen.Generators.DtoGeneration;
using Nextended.CodeGen.Helper;


[Generator]
public class MainGenerator : ISourceGenerator
{
    private bool attachDebugger = false; 
    private bool generationEnabled = true;
    private DateTime LastGenerated = DateTime.MinValue;


    public void Initialize(GeneratorInitializationContext context)
    { }

    public void Execute(GeneratorExecutionContext context)
    {
        if (!generationEnabled)
            return;

        LastGenerated = DateTime.Now;
        if (attachDebugger && !System.Diagnostics.Debugger.IsAttached)
            System.Diagnostics.Debugger.Launch();


        if (!context.AdditionalFiles.Any())
        {
            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("NCG001", "Info", "Did not find additional files", "Nextended.CodeGen", DiagnosticSeverity.Info, true), Location.None));
            return;
        }

        ExecuteGeneration(context);
        LastGenerated = DateTime.Now;
        context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("NCG003", "Info", $"Generation completed at {LastGenerated}", "Nextended.CodeGen", DiagnosticSeverity.Info, true), Location.None));
    }

    private void ExecuteGeneration(GeneratorExecutionContext context)
    {
        var additionalFiles = context.AdditionalFiles;
        new DtoGenerator(new DtoGenerationConfig()).Execute(context);
        

        foreach (var configFile in additionalFiles)
        {
            try
            {
                var json = configFile.GetText()?.ToString();
                var config = JsonConvert.DeserializeObject<MainConfig>(json);
                var ctx = new GenerationContext(new NamespaceResolver(configFile, context), context, config);

                foreach (var c in config.StructureGenerations)
                {
                    var path = NamespaceResolver.GetAbsolutePath(c.SourceFile, configFile.Path);
                    if (File.Exists(path))
                    {
                        var text = File.ReadAllText(path);
                        string classes = JsonClassGenerator.GenerateClasses(text, c);
                       // File.WriteAllText("C:\\dev\\privat\\github\\Nextended\\CodeGenSample\\GenTest.cs", classes);
                        context.AddSource("JsonClasses.g.cs", classes);
                    }
                }


                /////

            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("NCG002", "Warning", $"Could not proceed configuration {configFile.Path}", "Nextended.CodeGen", DiagnosticSeverity.Warning, true), Location.None));
            }
        }
    }
}
