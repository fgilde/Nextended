using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nextended.CodeGen.Helper;
using Nextended.CodeGen.Config;
using System.Text;
using System.Xml.Linq;
using Nextended.Core.Enums;

namespace Nextended.CodeGen.Generators.StructureGeneration;


public static class XmlClassGenerator
{
    public static string GenerateClasses(string xml, ClassStructureCodeGenerationConfig config)
    {
        XElement root = XElement.Parse(xml);
        var mainClassName = config.RootClassName ?? "RootObject";

        var classDefs = new Dictionary<string, string>();
        BuildClass(root, mainClassName, config, classDefs);

        var sb = new StringBuilder();
        sb.AppendFileHeader(mainClassName);
        sb.AppendLine($"namespace {config.Namespace}");
        sb.AppendLine("{");

        foreach (var classDef in classDefs.Values.Reverse())
            sb.AppendLine(classDef);

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void BuildClass(XElement elem, string className, ClassStructureCodeGenerationConfig config, Dictionary<string, string> classDefs, string currentPath = "")
    {
        if (classDefs.ContainsKey(className))
            return;

        var fields = new List<string>();
        // Attribute
        foreach (var attr in elem.Attributes())
        {
            var attrPath = string.IsNullOrEmpty(currentPath) ? "@" + attr.Name.LocalName : currentPath + ".@" + attr.Name.LocalName;
            if (config.Ignore != null && config.Ignore.Any(i => i.Equals(attrPath, StringComparison.OrdinalIgnoreCase)))
                continue;

            fields.Add($"    public string {attr.Name.LocalName.ToPascalCase()} {{ get; set; }}");
        }

        var grouped = elem.Elements().GroupBy(e => e.Name.LocalName);
        foreach (var group in grouped)
        {
            var propName = group.Key.ToPascalCase();
            var fullPath = string.IsNullOrEmpty(currentPath) ? group.Key : currentPath + "." + group.Key;
            if (config.Ignore != null && config.Ignore.Any(i => i.Equals(fullPath, StringComparison.OrdinalIgnoreCase)))
                continue;

            bool isList = group.Count() > 1;
            string typeName = config.Prefix + propName + config.Suffix;

            string arrayType = config.ArrayGeneration == JsonArrayGeneration.Array 
                ? $"{typeName}[]"
                : $"System.Collections.Generic.List<{typeName}>";
            
            BuildClass(group.First(), typeName, config, classDefs, fullPath);
            fields.Add(isList
                ? $"    public {arrayType} {propName} {{ get; set; }}"
                : $"    public {typeName} {propName} {{ get; set; }}");
        }

        if (!elem.HasElements && !string.IsNullOrWhiteSpace(elem.Value))
            fields.Add("    public string Value { get; set; }");

        var classDef = $"public partial class {className}\n{{\n" + string.Join("\n", fields) + "\n}\n";
        classDefs[className] = classDef;
    }
}
