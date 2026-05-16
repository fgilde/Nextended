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

        // Defensive guard against OData aggregate wrappers (Microsoft.AspNetCore.OData.Query.Wrapper.*).
        // Types like GroupByWrapper / AggregationWrapper / AggregationPropertyContainer expose CLR
        // properties (e.g. AggregationPropertyContainer.NestedValue declared as GroupByWrapper) whose
        // getters internally cast a dynamic, type-erased slot (NamedProperty<object>.Value) to the
        // declared property type. For $apply=groupby(...)/aggregate(...) responses that slot holds
        // boxed scalars (Decimal, etc.), so reflectively reading the property throws
        // InvalidCastException ("Unable to cast object of type 'System.Decimal' to type
        // 'GroupByWrapper'"). These wrappers are not user POCOs and never carry filter-targeted
        // properties, so we treat them as opaque leaves. Detected by namespace string to avoid taking
        // a hard dependency on the OData package.
        if (IsODataWrapperType(type))
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

            props.Add(new NavigableProperty(accessor, prop.PropertyType.IsEnumerableOrArray(), memberType));
        }

        return props.ToArray();
    }

    /// <summary>
    /// True for OData query-result wrapper types whose reflective property getters perform internal
    /// downcasts of dynamic <c>object</c> slots and therefore explode for aggregated/grouped rows.
    /// Walked as opaque leaves so the response-filter pipeline stays compatible with
    /// <c>$apply=groupby(...)/aggregate(...)</c> OData endpoints.
    /// </summary>
    private static bool IsODataWrapperType(Type type)
    {
        var ns = type.Namespace;
        if (ns is null) return false;
        return ns.StartsWith("Microsoft.AspNetCore.OData.Query.Wrapper", StringComparison.Ordinal)
            || ns.StartsWith("Microsoft.AspNetCore.OData.Query.Container", StringComparison.Ordinal);
    }

    /// <summary>
    /// Element type for enumerables (e.g. <c>List&lt;T&gt;</c> → <c>T</c>); otherwise the type itself.
    /// </summary>
    internal static Type UnwrapEnumerable(Type type)
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

    internal readonly record struct NavigableProperty(PropertyAccessor Accessor, bool IsEnumerable, Type MemberType);
}
