using System;
using Nextended.ResponseFilters.Reflection;
using Nextended.ResponseFilters.Rules;

namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Two-step builder: first specify the replacement value via <c>With(...)</c>,
/// then close with <c>When(...)</c>/<c>Unless(...)</c>/<c>Always()</c>.
/// </summary>
public sealed class ReplaceBuilder<T, TProp> where T : class
{
    private readonly ResponseFilter<T> _filter;
    private readonly PropertyAccessor _accessor;

    internal ReplaceBuilder(ResponseFilter<T> filter, PropertyAccessor accessor)
    {
        _filter = filter;
        _accessor = accessor;
    }

    /// <summary>Replace with a constant value.</summary>
    public ReplaceTerminal<T, TProp> With(TProp value)
        => new(_filter, _accessor, (_, _, _) => value);

    /// <summary>Replace with a value computed per-instance.</summary>
    public ReplaceTerminal<T, TProp> With(Func<T, TProp> valueFactory)
        => new(_filter, _accessor, (instance, _, _) => valueFactory(instance));

    /// <summary>Replace with a value computed per-instance and context.</summary>
    public ReplaceTerminal<T, TProp> With(Func<T, IResponseFilterContext, TProp> valueFactory)
        => new(_filter, _accessor, (instance, _, ctx) => valueFactory(instance, ctx));
}

/// <summary>Terminal phase of a <c>Replace</c> rule — applies the predicate vocabulary.</summary>
public sealed class ReplaceTerminal<T, TProp> : RuleBuilderBase<ReplaceTerminal<T, TProp>, T> where T : class
{
    private readonly PropertyAccessor _accessor;
    private readonly Func<T, PropertyAccessor, IResponseFilterContext, object?> _valueProducer;

    internal ReplaceTerminal(
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
