using Nextended.Core.Attributes;
using Nextended.Core.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nextended.Core.Helper;

namespace Nextended.Core.IncludeDefinitions;

public sealed class AttributeIncludePathDefinition<T>(
    string? group = null,
    int maxDepth = 6,
    bool includeCollections = true)
    : IIncludePathDefinition
    where T : class
{
    private static readonly ConcurrentDictionary<(Type Root, string? Group, int Depth, bool InclColl), string[]> Cache = new();

    public IEnumerable<string> GetPaths()
    {
        var key = (typeof(T), _group: group, _maxDepth: maxDepth, _includeCollections: includeCollections);

        if (Cache.TryGetValue(key, out var cached))
            return cached;

        bool MatchesAttr(PropertyInfo pi) =>
            pi.GetCustomAttributes<IncludeInDetailsAttribute>(inherit: true)
                .Any(a => a.Group == group || (a.Group == null && group == null));

        var paths = PathScanner
            .GetPaths(typeof(T), MatchesAttr, maxDepth, includeCollections)
            .OrderBy(s => s.Length)
            .ThenBy(s => s, StringComparer.Ordinal)
            .ToArray();

        Cache[key] = paths;
        return paths;
    }
}
