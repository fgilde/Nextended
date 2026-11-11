using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Nextended.CodeGen.Generators.TypescriptGeneration;

/// <summary>Type keys (as used in "Types", "TypeMap", "IncludeTypes", ...) and the naming policy of the serializer.</summary>
public static class TypescriptNames
{
    /// <summary><c>Ns.Outer.Name</c>, generic types with their arity: <c>Ns.Page`1</c>.</summary>
    public static string Key(ITypeSymbol type)
    {
        type = type.OriginalDefinition;
        var name = new StringBuilder();
        for (var t = type; t != null; t = t.ContainingType)
        {
            var part = t is INamedTypeSymbol { Arity: > 0 } named ? $"{t.Name}`{named.Arity}" : t.Name;
            name.Insert(0, name.Length == 0 ? part : part + ".");
        }
        var ns = type.ContainingNamespace is { IsGlobalNamespace: false } n ? n.ToDisplayString() + "." : "";
        return ns + name;
    }

    /// <summary>A type name as users write it (<c>Page&lt;T&gt;</c>, <c>Page&lt;,&gt;</c>, <c>global::Ns.Page`1</c>) in key form.</summary>
    public static string Normalize(string name)
    {
        name = name.Trim();
        if (name.StartsWith("global::", System.StringComparison.Ordinal))
            name = name.Substring(8);
        var open = name.IndexOf('<');
        if (open < 0)
            return name;
        var args = name.Substring(open + 1).TrimEnd('>');
        return $"{name.Substring(0, open)}`{args.Count(c => c == ',') + 1}";
    }

    /// <summary>The key without the arity (<c>Ns.Page</c>).</summary>
    public static string WithoutArity(string key)
    {
        var tick = key.LastIndexOf('`');
        return tick < 0 ? key : key.Substring(0, tick);
    }

    /// <summary>System.Text.Json's JsonNamingPolicy.CamelCase.</summary>
    public static string CamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0]))
            return name;
        var chars = name.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (i == 1 && !char.IsUpper(chars[i]))
                break;
            var hasNext = i + 1 < chars.Length;
            if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
            {
                if (chars[i + 1] == ' ')
                    chars[i] = char.ToLowerInvariant(chars[i]);
                break;
            }
            chars[i] = char.ToLowerInvariant(chars[i]);
        }
        return new string(chars);
    }

    /// <summary>True when <paramref name="name"/> can stand unquoted as a property name.</summary>
    public static bool IsIdentifier(string name)
        => name.Length > 0 && (char.IsLetter(name[0]) || name[0] is '_' or '$') && name.All(c => char.IsLetterOrDigit(c) || c is '_' or '$');

    /// <summary>A TypeScript string literal in single quotes.</summary>
    public static string Literal(string value) => "'" + value.Replace("\\", "\\\\").Replace("'", "\\'") + "'";

    /// <summary>A property name, quoted when it is not an identifier.</summary>
    public static string PropertyName(string name) => IsIdentifier(name) ? name : Literal(name);
}
