using Nextended.ResponseFilters.Reflection;
using Nextended.ResponseFilters.Rules;

namespace Nextended.ResponseFilters.Builders;

/// <summary>Builder for "set property to null when predicate matches" rules.</summary>
public sealed class NullifyBuilder<T> : RuleBuilderBase<NullifyBuilder<T>, T> where T : class
{
    private readonly PropertyAccessor[] _accessors;

    internal NullifyBuilder(ResponseFilter<T> filter, PropertyAccessor[] accessors) : base(filter)
    {
        _accessors = accessors;
    }

    protected override void RegisterRule(AsyncPredicate<T> predicate)
    {
        Filter.AddRule(new PropertyMutationRule<T>(
            _accessors,
            predicate,
            valueProducer: static (_, _, _) => null));
    }
}
