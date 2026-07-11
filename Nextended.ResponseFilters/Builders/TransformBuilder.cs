using System;
using Nextended.ResponseFilters.Reflection;
using Nextended.ResponseFilters.Rules;

namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Two-step builder: first specify the transform via <c>Using(...)</c>,
/// then close with <c>When(...)</c>/<c>Unless(...)</c>/<c>Always()</c>.
/// </summary>
public sealed class TransformBuilder<T, TProp> where T : class
{
    private readonly ResponseFilter<T> _filter;
    private readonly PropertyAccessor _accessor;

    internal TransformBuilder(ResponseFilter<T> filter, PropertyAccessor accessor)
    {
        _filter = filter;
        _accessor = accessor;
    }

    /// <summary>Apply a function to the current value to produce the new value.</summary>
    public TransformTerminal<T, TProp> Using(Func<TProp, TProp> transform)
        => new(_filter, _accessor, (instance, accessor, _) => transform((TProp)accessor.GetValue(instance)!));

    /// <summary>Apply a function that has access to the parent instance.</summary>
    public TransformTerminal<T, TProp> Using(Func<T, TProp, TProp> transform)
        => new(_filter, _accessor, (instance, accessor, _) => transform(instance, (TProp)accessor.GetValue(instance)!));

    /// <summary>Apply a function that has access to instance + context.</summary>
    public TransformTerminal<T, TProp> Using(Func<T, TProp, IResponseFilterContext, TProp> transform)
        => new(_filter, _accessor, (instance, accessor, ctx) => transform(instance, (TProp)accessor.GetValue(instance)!, ctx));
}

/// <summary>Terminal phase of a <c>Transform</c> rule — applies the predicate vocabulary.</summary>
public sealed class TransformTerminal<T, TProp> : RuleBuilderBase<TransformTerminal<T, TProp>, T> where T : class
{
    private readonly PropertyAccessor _accessor;
    private readonly Func<T, PropertyAccessor, IResponseFilterContext, object?> _valueProducer;

    internal TransformTerminal(
        ResponseFilter<T> filter,
        PropertyAccessor accessor,
        Func<T, PropertyAccessor, IResponseFilterContext, object?> valueProducer) : base(filter)
    {
        _accessor = accessor;
        _valueProducer = valueProducer;
    }

    protected override void RegisterRule(AsyncPredicate<T> predicate)
    {
        Filter.AddRule(new PropertyMutationRule<T>(
            FilterProperties(_accessor),
            predicate,
            _valueProducer));
    }
}
