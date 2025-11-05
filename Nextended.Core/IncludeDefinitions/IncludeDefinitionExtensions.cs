using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Nextended.Core.Contracts;
using Nextended.Core.Extensions;

namespace Nextended.Core.IncludeDefinitions;

public static class IncludeDefinitionFilterExtensions
{
    public static IIncludePathDefinition Without(this IIncludePathDefinition def, params string[] exactOrGlob)
        => def.WithoutWhere(GlobPredicate(exactOrGlob));

    public static IIncludePathDefinition WithoutPrefix(this IIncludePathDefinition def, params string[] prefixes)
        => def.WithoutWhere(p => prefixes.Any(px => p.Equals(px, StringComparison.Ordinal) ||
                                                    p.StartsWith(px + ".", StringComparison.Ordinal)));

    public static IIncludePathDefinition Except(this IIncludePathDefinition def, IIncludePathDefinition remove)
    {
        var removeSet = new HashSet<string>(remove.GetPaths(), StringComparer.Ordinal);
        return def.WithoutWhere(p => removeSet.Contains(p));
    }

    public static IIncludePathDefinition Without<TEntity>(this IIncludePathDefinition def,
        params Expression<Func<TEntity, object?>>[] expressions)
    {
        var prefixes = expressions.Select(e => e.GetPropertyPath()).ToArray();
        return def.WithoutPrefix(prefixes);
    }

    public static IIncludePathDefinition WithoutWhere(this IIncludePathDefinition def, Func<string, bool> predicate)
        => new FilteredIncludePathDefinition(def, new[] { predicate });

    public static IIncludePathDefinition WithoutRegex(this IIncludePathDefinition def, params string[] regexes)
        => def.WithoutWhere(p => regexes.Any(rx => Regex.IsMatch(p, rx)));

    private static Func<string, bool> GlobPredicate(IEnumerable<string> patterns)
    {
        var regexes = patterns.Select(GlobToRegex).Select(p => new Regex(p, RegexOptions.CultureInvariant)).ToArray();
        return s => regexes.Any(rx => rx.IsMatch(s));
    }

    private static string GlobToRegex(string glob)
    {
        var escaped = Regex.Escape(glob)
            .Replace(@"\*\*", "___GLOBSTAR___")
            .Replace(@"\*", @"[^.]*")
            .Replace(@"\?", @".");
        return "^" + escaped.Replace("___GLOBSTAR___", ".*") + "$";
    }

}