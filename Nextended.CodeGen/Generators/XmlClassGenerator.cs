using Nextended.CodeGen.Helper;
using Nextended.CodeGen.Config;
using System.Text;
using System.Xml.Linq;

namespace Nextended.CodeGen.Generators;


public static class XmlClassGenerator
{
    public static string GenerateClasses(string xml, string rootClassName, ClassStructureCodeGenerationConfig config)
    {
        XElement root = XElement.Parse(xml);
        var mainClassName = config.Prefix + rootClassName + config.Suffix;

        var classDefs = new Dictionary<string, string>();
        BuildClass(root, mainClassName, config, classDefs);

        var sb = new StringBuilder();
        sb.AppendLine($"namespace {config.Namespace}");
        sb.AppendLine("{");

        foreach (var classDef in classDefs.Values.Reverse())
            sb.AppendLine(classDef);

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void BuildClass(XElement elem, string className, ClassStructureCodeGenerationConfig config, Dictionary<string, string> classDefs)
    {
        if (classDefs.ContainsKey(className))
            return;

        var fields = new List<string>();
        // Attribute
        foreach (var attr in elem.Attributes())
            fields.Add($"    public string {attr.Name.LocalName.ToPascalCase()} {{ get; set; }}");

        // Elemente
        var grouped = elem.Elements().GroupBy(e => e.Name.LocalName);
        foreach (var group in grouped)
        {
            var propName = group.Key.ToPascalCase();
            bool isList = group.Count() > 1;
            string typeName = config.Prefix + propName + config.Suffix;

            BuildClass(group.First(), typeName, config, classDefs);
            fields.Add(isList
                ? $"    public List<{typeName}> {propName} {{ get; set; }}"
                : $"    public {typeName} {propName} {{ get; set; }}");
        }

        // Textinhalt
        if (!elem.HasElements && !string.IsNullOrWhiteSpace(elem.Value))
            fields.Add("    public string Value { get; set; }");

        var classDef = $"public class {className}\n{{\n" + string.Join("\n", fields) + "\n}\n";
        classDefs[className] = classDef;
    }
}
