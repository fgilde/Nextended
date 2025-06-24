using System.Text;

namespace Nextended.CodeGen.Helper;

internal static class StringBuilderHelper
{
    public static StringBuilder AppendIf(this StringBuilder sb, string value, bool condition = true)
    {
        if (condition)
            sb.Append(value);
        return sb;
    }
    
    public static StringBuilder AppendLineIf(this StringBuilder sb, string line, bool condition = true)
    {
        if (condition)
            sb.AppendLine(line);
        return sb;
    }
    public static StringBuilder AppendUsings(this StringBuilder sb, params string[] namespaces)
    {
        foreach (var ns in namespaces)
        {
            sb.AppendLine($"using {ns};");
        }
        return sb;
    }
    public static StringBuilder AppendFileHeader(this StringBuilder sb, params string[] names)
    {
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// --- AUTO GENERATED CODE ({DateTime.Now:G}) ---");
        sb.AppendLine("/// --- COM Klassen ---");
        foreach (var name in names) sb.AppendLine($"/// --- {name} ---");
        sb.AppendLine("/// </summary>\n");
        return sb;
    }
}