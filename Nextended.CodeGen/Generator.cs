using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Contracts;
using Nextended.CodeGen.Generators;


[Generator]
public class MainGenerator : ISourceGenerator
{
    private int i = 1;
    private bool attachDebugger = true;
    private bool generationEnabled = true;
    private DateTime LastGenerated = DateTime.MinValue;

    private readonly List<object> _subGenerators = new()
    {
        new ExcelSourceGenerator(),
        // new JsonSourceGenerator(),
        // Neue einfach hinzufügen!
    };

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
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("LOC001", "Info", "Did not find additional files", "UnLocodeGen", DiagnosticSeverity.Info, true),
                Location.None));
            return;
        }

        ExecuteGeneration(context);

    }

    private void ExecuteGeneration(GeneratorExecutionContext context)
    {
        // 1. AdditionalFiles auflisten:
        var additionalFiles = context.AdditionalFiles;
        new ComSourceGenerator().Execute(context, null, null);
        
        
        foreach (var configFile in additionalFiles)
        {
            var json = configFile.GetText()?.ToString();
            var config = JsonConvert.DeserializeObject<CodeGenConfig>(json);

            // 3. Subgeneratoren aufrufen:
            foreach (var subGen in _subGenerators)
            {
                switch (subGen)
                {
                    case ISourceSubGenerator<ExcelFileConfig> excelGen when config.ExcelFiles?.Count > 0:
                        excelGen.Execute(context, config.ExcelFiles, configFile);
                        break;
                    case ISourceSubGenerator<JsonFileConfig> jsonGen when config.JsonFiles?.Count > 0:
                        jsonGen.Execute(context, config.JsonFiles, configFile);
                        break;
                    // usw. für neue Generatoren
                }
            }
        }
    }
}
