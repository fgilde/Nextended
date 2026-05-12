using System;
using Nextended.ResponseFilters.Reflection;
using Nextended.ResponseFilters.Rules;

namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Builder for "reset properties to their <c>default(TProperty)</c>" rules.
/// </summary>
/// <remarks>
/// Differs from <see cref="NullifyBuilder{T}"/> in two ways:
/// (a) it tolerates non-nullable value types (sets them to <c>0</c> / <c>false</c> / etc. — built via
/// <see cref="Activator.CreateInstance(Type)"/>);
/// (b) it accepts selectors with heterogeneous property types in a single call (Cost → 0m, Name → null,
/// IsActive → false in one chain).
/// </remarks>
public sealed class SetToDefaultBuilder<T> : RuleBuilderBase<SetToDefaultBuilder<T>, T> where T : class
{
    private readonly PropertyAccessor[] _accessors;

    internal SetToDefaultBuilder(ResponseFilter<T> filter, PropertyAccessor[] accessors) : base(filter)
    {
        _accessors = accessors;
    }

    protected override void RegisterRule(AsyncPredicate<T> predicate)
    {
        Filter.AddRule(new PropertyMutationRule<T>(
            _accessors,
            predicate,
            valueProducer: static (_, accessor, _) => DefaultValueFor(accessor.PropertyType)));
    }

    private static object? DefaultValueFor(Type type)
    {
        if (!type.IsValueType)
        {
            return null; // reference types
        }
        if (Nullable.GetUnderlyingType(type) != null)
        {
            return null; // Nullable<T>
        }
        return Activator.CreateInstance(type); // non-nullable value types → 0, false, default struct
    }
}
