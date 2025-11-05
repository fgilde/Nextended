using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nextended.Core.Extensions;

namespace Nextended.Core.Helper;

internal static class PathScanner
{
    private static readonly ConcurrentDictionary<(Type root, string key), string[]> Cache = new();

    public static IEnumerable<string> GetPaths(
        Type root,
        Func<PropertyInfo, bool> includeCondition,
        int maxDepth,
        bool includeCollections)
    {
        var cacheKey = (root, Key(includeCondition, maxDepth, includeCollections));
        if (Cache.TryGetValue(cacheKey, out var cached)) return cached;

        var result = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<(Type Type, string Path)>(new TypePathComparer());

        void Walk(Type type, string path, int depth)
        {
            if (depth > maxDepth) return;

            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!includeCondition(prop)) continue;

                var (isCollection, elementType) = GetCollectionElementType(prop.PropertyType);
                var navType = isCollection ? elementType : prop.PropertyType;

                if (navType.IsScalar()) continue;
                if (!includeCollections && isCollection) continue;

                var currentPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
                if (!result.Add(currentPath)) { /* already added */ }

                var visitKey = (navType, currentPath);
                if (!visited.Add(visitKey)) continue;

                Walk(navType, currentPath, depth + 1);
            }
        }

        Walk(root, "", 1);

        var arr = result.OrderBy(s => s.Length).ThenBy(s => s, StringComparer.Ordinal).ToArray();
        Cache[cacheKey] = arr;
        return arr;
    }

    // ---------- Utils ----------

    private static string Key(Func<PropertyInfo, bool> cond, int depth, bool inclCols)
        => $"{cond.Method.DeclaringType?.FullName}.{cond.Method.MetadataToken}:{depth}:{inclCols}";



    private static (bool isCollection, Type elementType) GetCollectionElementType(Type t)
    {
        if (t == typeof(string)) return (false, t);
        if (t.IsArray) return (true, t.GetElementType()!);

        var ienum = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? t
            : t.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return ienum != null ? (true, ienum.GetGenericArguments()[0]) : (false, t);
    }

    private sealed class TypePathComparer : IEqualityComparer<(Type Type, string Path)>
    {
        public bool Equals((Type Type, string Path) x, (Type Type, string Path) y)
            => x.Type == y.Type && string.Equals(x.Path, y.Path, StringComparison.Ordinal);

#if !NETSTANDARD2_0
        public int GetHashCode((Type Type, string Path) obj)
            => HashCode.Combine(obj.Type, obj.Path);
#else
        public int GetHashCode((Type Type, string Path) obj)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (obj.Type?.GetHashCode() ?? 0);
                hash = hash * 31 + (obj.Path?.GetHashCode() ?? 0);
                return hash;
            }
        }
#endif
    }
}