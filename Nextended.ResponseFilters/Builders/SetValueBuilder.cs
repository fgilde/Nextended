using System;
using Nextended.ResponseFilters.Reflection;
using Nextended.ResponseFilters.Rules;

namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Two-step builder: first specify the value via <c>To(...)</c>,
/// then close with <c>When(...)</c>/<c>Unless(...)</c>/<c>Always()</c>.
/// </summary>
/// <remarks>
/// Semantically equivalent to <see cref="ReplaceBuilder{T, TProp}"/> but with a more imperative
/// vocabulary that reads better when there isn't necessarily an "existing" value being replaced
/// (e.g. setting a flag on a freshly returned DTO).
/// </remarks>
public sealed class SetValueBuilder<T, TProp> where T : class
{
    private readonly ResponseFilter<T> _filter;
    private readonly PropertyAccessor _accessor;

    internal SetValueBuilder(ResponseFilter<T> filter, PropertyAccessor accessor)
    {
        _filter = filter;
        _accessor = accessor;
    }

    /// <summary>Set the property to a constant value.</summary>
    public SetValueTerminal<T, TProp> To(TProp value)
        => new(_filter, _accessor, (_, _, _) => value);

    /// <summary>Set the property to a value computed per-instance.</summary>
    public SetValueTerminal<T, TProp> To(Func<T, TProp> valueFactory)
        => new(_filter, _accessor, (instance, _, _) => valueFactory(instance));

    /// <summary>Set the property to a value computed per-instance and context.</summary>
    public SetValueTerminal<T, TProp> To(Func<T, IResponseFilterContext, TProp> valueFactory)
        => new(_filter, _accessor, (instance, _, ctx) => valueFactory(instance, ctx));
}

/// <summary>Terminal phase of a <c>SetValue</c> rule — applies the predicate vocabulary.</summary>
public sealed class SetValueTerminal<T, TProp> : RuleBuilderBase<SetValueTerminal<T, TProp>, T> where T : class
{
    private readonly PropertyAccessor _accessor;
    private readonly Func<T, PropertyAccessor, IResponseFilterContext, object?> _valueProducer;

    internal SetValueTerminal(
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
            new[] { _accessor },
            predicate,
            _valueProducer));
    }
}
