using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Nextended.CodeGen.Helper;

internal static class StringExtensions
{
    public static string InsertUsings(this string str, IEnumerable<string> usings)
    {
        var sb = new StringBuilder().AppendUsings(usings?.Distinct()?.ToArray());
        sb.Append(str);
        return sb.ToString();
    }
    public static string ToPascalCase(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var parts = Regex.Matches(input, @"[A-Z]?[a-z]+|[0-9]+|[A-Z]+(?![a-z])")
            .Cast<Match>()
            .Select(m => m.Value)
            .ToList();

        if (parts.Count == 0)
            parts = Regex.Split(input, @"[_\-\s]+").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        for (int i = 0; i < parts.Count; i++)
            parts[i] = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(parts[i].ToLower());

        return string.Join("", parts);
    }

    public static string EscapeForCSharp(this string str) =>
        str.Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
}