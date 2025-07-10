using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Contracts;
using Nextended.CodeGen.Helper;

namespace Nextended.CodeGen.Generators.StructureGeneration;

public class StructureGenerator : ISourceSubGenerator
{
    public bool RequireConfig => true;
    public IEnumerable<GeneratedFile> Execute(GenerationContext context)
    {
        foreach (var config in (context.Config?.StructureGenerations ?? []).OfType<ClassStructureCodeGenerationConfig>())
        {
            var path = NamespaceResolver.GetAbsolutePath(config.SourceFile, context.AdditionalFile.Path);
            if (File.Exists(path))
            {
                var isXmlFile = Path.GetExtension(path).Equals(".xml", StringComparison.OrdinalIgnoreCase);
                var isJsonFile = Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase);
                var text = File.ReadAllText(path);
                var fileName = Path.ChangeExtension(Path.GetFileName(config.SourceFile), "g.cs");

                if (isXmlFile)
                    yield return new GeneratedFile(fileName, config.Namespace, XmlClassGenerator.GenerateClasses(text, config), config);
                else if (isJsonFile)
                    yield return new GeneratedFile(fileName, config.Namespace, JsonClassGenerator.GenerateClasses(text, config), config);
            }
        }
    }
}