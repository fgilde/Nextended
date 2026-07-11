using System.Linq;
using Nextended.ResponseFilters.Reflection;
using Nextended.ResponseFilters.Rules;

namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Builder for "drop one or more properties from the serialized output when the predicate matches" rules.
/// </summary>
/// <remarks>
/// Unlike <see cref="NullifyBuilder{T}"/> (which keeps the key with a <c>null</c> value), a removed
/// property disappears entirely — the key is not present in the response at all.
/// </remarks>
public sealed class RemoveBuilder<T> : RuleBuilderBase<RemoveBuilder<T>, T> where T : class
{
    private readonly string[] _propertyNames;

    internal RemoveBuilder(ResponseFilter<T> filter, PropertyAccessor[] accessors) : base(filter)
    {
        _propertyNames = accessors.Select(a => a.Property.Name).ToArray();
    }

    protected override void RegisterRule(AsyncPredicate<T> predicate)
    {
        var names = _propertyNames;
        Filter.AddRule(new StructuralEditRule<T>(
            predicate,
            (_, _) => names.Select(StructuralEdit.Remove)));
    }
}
