using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Helper;
using Nextended.Core.Enums;

namespace Nextended.CodeGen.Generators.StructureGeneration;

public static class JsonClassGenerator 
{
    public static string GenerateClasses(string json, ClassStructureCodeGenerationConfig config)
    {
        JObject root = JObject.Parse(json);
        var mainClassName = config.RootClassName ?? "RootObject";

        var classDefs = new Dictionary<string, string>();
        BuildClass(root, mainClassName, config, classDefs);

        var sb = new StringBuilder();
        sb.AppendFileHeader(mainClassName);
        sb.AppendLine($"namespace {config.Namespace}");
        sb.AppendLine("{");

        foreach (var classDef in classDefs.Values.Reverse()) // Reverse, damit Subtypen am Ende stehen
            sb.AppendLine(classDef);

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void BuildClass(JObject obj, string className, ClassStructureCodeGenerationConfig config, Dictionary<string, string> classDefs, string currentPath = "")
    {
        if (classDefs.ContainsKey(className))
            return;

        var fields = new List<string>();
        foreach (var prop in obj.Properties())
        {
            var propName = prop.Name.ToPascalCase();
            var fullPath = string.IsNullOrEmpty(currentPath) ? prop.Name : currentPath + "." + prop.Name;

            // Property ignorieren?
            if (config.Ignore != null && config.Ignore.Any(i => i.Equals(fullPath, StringComparison.OrdinalIgnoreCase)))
                continue;

            string propType = GetCSharpType(prop.Value, propName, config, classDefs, fullPath);
            fields.Add($"\t\tpublic {propType} {propName} {{ get; set; }}");
        }

        var classDef = $"\tpublic partial {config.ModelType.ToCSharpKeyword()} {className}\n\t{{\n" + string.Join("\n", fields) + "\n\t}\n";
        classDefs[className] = classDef;
    }

    private static string GetCSharpType(JToken token, string propName, ClassStructureCodeGenerationConfig config, Dictionary<string, string> classDefs, string currentPath)
    {
        if (config.Ignore != null && config.Ignore.Any(i => i.Equals(currentPath, StringComparison.OrdinalIgnoreCase)))
            return null;
        
        switch (token.Type)
        {
            case JTokenType.Object:
                var subClassName = config.Prefix + propName + config.Suffix;
                BuildClass((JObject)token, subClassName, config, classDefs, currentPath);
                return subClassName;

            case JTokenType.Array:
                var array = token as JArray;
                if (array.Count > 0)
                    return $"List<{GetCSharpType(array[0], propName, config, classDefs, currentPath)}>";
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
            case JTokenType.Bytes:
                return "byte[]";
            case JTokenType.Guid:
                return "Guid";
            case JTokenType.Uri:
                return "Uri";
            case JTokenType.TimeSpan:
                return "TimeSpan";

            case JTokenType.Null:
            case JTokenType.Undefined:
                return "object";

            case JTokenType.None:
            case JTokenType.Constructor:
            case JTokenType.Property:
            case JTokenType.Comment:
            case JTokenType.Raw:
            default:
                return "object";
        }
    }

    private static bool ShouldIgnore(string path, string[] ignoreList)
    {
        return ignoreList != null && ignoreList.Any(i => i.Equals(path, StringComparison.OrdinalIgnoreCase));
    }

}
