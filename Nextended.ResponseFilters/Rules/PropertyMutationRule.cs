using System;
using System.Threading.Tasks;
using Nextended.ResponseFilters.Reflection;

namespace Nextended.ResponseFilters.Rules;

/// <summary>
/// Generic rule that mutates one or more properties on a <typeparamref name="T"/> instance
/// when a predicate matches.
/// </summary>
/// <remarks>
/// Used as the underlying primitive for <c>Nullify</c>, <c>Replace</c>, and <c>Transform</c>:
/// they all reduce to "evaluate predicate; if true, apply a value-producer per property".
/// </remarks>
internal sealed class PropertyMutationRule<T> : IResponseFilterRule<T> where T : class
{
    private readonly PropertyAccessor[] _accessors;
    private readonly AsyncPredicate<T> _predicate;
    private readonly Func<T, PropertyAccessor, IResponseFilterContext, object?> _valueProducer;

    public PropertyMutationRule(
        PropertyAccessor[] accessors,
        AsyncPredicate<T> predicate,
        Func<T, PropertyAccessor, IResponseFilterContext, object?> valueProducer)
    {
        _accessors = accessors;
        _predicate = predicate;
        _valueProducer = valueProducer;
    }

    public async ValueTask ApplyAsync(T instance, IResponseFilterContext context)
    {
        if (!await _predicate(instance, context).ConfigureAwait(false))
        {
            return;
        }

        foreach (var accessor in _accessors)
        {
            var value = _valueProducer(instance, accessor, context);
            accessor.SetValue(instance, value);
        }
    }
}
