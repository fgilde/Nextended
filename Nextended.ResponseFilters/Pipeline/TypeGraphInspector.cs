using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nextended.Core.Extensions;
using Nextended.ResponseFilters.Reflection;

namespace Nextended.ResponseFilters.Pipeline;

/// <summary>
/// Process-wide reflection cache: per type, the list of "navigable" properties the graph walker
/// must descend into (non-scalar, non-indexed, readable). Strings, primitives, enums, decimals,
/// dates and other leaves are excluded via <c>Nextended.Core</c>'s <c>IsScalar</c>.
/// </summary>
internal static class TypeGraphInspector
{
    private static readonly ConcurrentDictionary<Type, NavigableProperty[]> Cache = new();

    public static NavigableProperty[] GetNavigableProperties(Type type)
        => Cache.GetOrAdd(type, Build);

    private static NavigableProperty[] Build(Type type)
    {
        if (type.IsScalar())
        {
            return Array.Empty<NavigableProperty>();
        }

        var props = new List<NavigableProperty>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var memberType = UnwrapEnumerable(prop.PropertyType);
            if (memberType.IsScalar())
            {
                continue;
            }

            var accessor = PropertyAccessor.For(prop);
            if (accessor.Getter is null) continue;

            props.Add(new NavigableProperty(accessor, prop.PropertyType.IsEnumerableOrArray()));
        }

        return props.ToArray();
    }

    private static Type UnwrapEnumerable(Type type)
    {
        if (type == typeof(string))
        {
            return type;
        }
        if (type.IsArray)
        {
            return type.GetElementType() ?? type;
        }
        var enumerable = type.GetInterfaces().FirstOrDefault(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0] ?? type;
    }

    internal readonly record struct NavigableProperty(PropertyAccessor Accessor, bool IsEnumerable);
}
