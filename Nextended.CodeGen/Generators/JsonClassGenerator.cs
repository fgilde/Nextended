using System.Text;
using Newtonsoft.Json.Linq;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Helper;

public static class JsonClassGenerator
{
    public static string GenerateClasses(string json, string rootClassName, ClassStructureCodeGenerationConfig config)
    {
        JObject root = JObject.Parse(json);
        var mainClassName = config.Prefix + rootClassName + config.Suffix;

        var classDefs = new Dictionary<string, string>();
        BuildClass(root, mainClassName, config, classDefs);

        var sb = new StringBuilder();
        sb.AppendLine($"namespace {config.Namespace}");
        sb.AppendLine("{");

        foreach (var classDef in classDefs.Values.Reverse()) // Reverse, damit Subtypen am Ende stehen
            sb.AppendLine(classDef);

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void BuildClass(JObject obj, string className, ClassStructureCodeGenerationConfig config, Dictionary<string, string> classDefs)
    {
        if (classDefs.ContainsKey(className))
            return;

        var fields = new List<string>();
        foreach (var prop in obj.Properties())
        {
            var propName = prop.Name.ToPascalCase();
            string propType = GetCSharpType(prop.Value, propName, config, classDefs);
            fields.Add($"\t\tpublic {propType} {propName} {{ get; set; }}");
        }

        var classDef = $"\tpublic partial class {className}\n\t{{\n" + string.Join("\n", fields) + "\n\t}\n";
        classDefs[className] = classDef;
    }

    private static string GetCSharpType(JToken token, string propName, ClassStructureCodeGenerationConfig config, Dictionary<string, string> classDefs)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                var subClassName = config.Prefix + propName + config.Suffix;
                BuildClass((JObject)token, subClassName, config, classDefs);
                return subClassName;
            case JTokenType.Array:
                var array = token as JArray;
                if (array.Count > 0)
                    return $"List<{GetCSharpType(array[0], propName, config, classDefs)}>";
                else
                    return "List<object>";
            case JTokenType.Integer:
                return "int";
            case JTokenType.Float:
                return "double";
            case JTokenType.Boolean:
                return "bool";
            case JTokenType.String:
                return "string";
            case JTokenType.Date:
                return "DateTime";
            default:
                return "object";
        }
    }

}
