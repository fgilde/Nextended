using System.Globalization;
using System.Text;

namespace Nextended.CodeGen.Helper;

internal static class StringExtensions
{
    public static string InsertUsings(this string str, IEnumerable<string> usings)
    {
        var sb = new StringBuilder().AppendUsings(usings?.Distinct()?.ToArray());
        sb.Append(str);
        return sb.ToString();
    }
    public static string ToPascalCase(this string s)
    {
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s)
            .Replace("_", "")
            .Replace("-", "")
            .Replace(" ", "");
    }
}