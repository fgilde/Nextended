using System;
using System.Collections;
using Nextended.ResponseFilters.Reflection;
using Nextended.ResponseFilters.Rules;

namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Sets a property to its "empty" state:
/// <list type="bullet">
///   <item><see cref="string"/> → <see cref="string.Empty"/></item>
///   <item><see cref="IList"/> with <c>IsReadOnly = false</c> → in-place <c>.Clear()</c></item>
///   <item>Arrays → new zero-length array of the element type</item>
///   <item>Anything else → <c>null</c> (logged warning at pipeline level if assignment fails)</item>
/// </list>
/// </summary>
public sealed class ClearBuilder<T> : RuleBuilderBase<ClearBuilder<T>, T> where T : class
{
    private readonly PropertyAccessor _accessor;

    internal ClearBuilder(ResponseFilter<T> filter, PropertyAccessor accessor) : base(filter)
    {
        _accessor = accessor;
    }

    protected override void RegisterRule(AsyncPredicate<T> predicate)
    {
        Filter.AddRule(new PropertyMutationRule<T>(
            new[] { _accessor },
            predicate,
            valueProducer: static (instance, accessor, _) => EmptyValueFor(accessor.GetValue(instance), accessor.PropertyType)));
    }

    private static object? EmptyValueFor(object? current, Type propertyType)
    {
        if (current is string) return string.Empty;

        // Arrays first — they advertise IList.IsReadOnly == false but throw on Clear() because they are fixed-size.
        // Replace with a new zero-length array of the same element type.
        if (propertyType.IsArray)
        {
            return Array.CreateInstance(propertyType.GetElementType()!, 0);
        }

        // In-place clear for mutable IList — return the same reference so the setter writes back the same object.
        if (current is IList list && !list.IsReadOnly && !list.IsFixedSize)
        {
            list.Clear();
            return list;
        }

        if (propertyType == typeof(string))
        {
            return string.Empty;
        }

        return null;
    }
}
