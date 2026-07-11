using System;
using Nextended.ResponseFilters.Rules;

namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Two-step builder that injects an extra key into the serialized output — a key that does not exist
/// on the CLR type. First specify the value via <c>From(...)</c>/<c>WithValue(...)</c>, then close
/// with the predicate vocabulary.
/// </summary>
/// <remarks>
/// The value is computed while the pipeline walks the graph (so it can read the instance and context)
/// and serialized with the same options as the rest of the response. Use it for computed/derived
/// fields (a display label, a signed URL, a permission flag) that the DTO itself doesn't carry.
/// </remarks>
public sealed class AddPropertyBuilder<T> where T : class
{
    private readonly ResponseFilter<T> _filter;
    private readonly string _name;

    internal AddPropertyBuilder(ResponseFilter<T> filter, string name)
    {
        _filter = filter;
        _name = name;
    }

    /// <summary>Inject a constant value.</summary>
    public AddPropertyTerminal<T> WithValue(object? value)
        => new(_filter, _name, (_, _) => value);

    /// <summary>Inject a value computed from the instance.</summary>
    public AddPropertyTerminal<T> From(Func<T, object?> valueFactory)
    {
        if (valueFactory is null) throw new ArgumentNullException(nameof(valueFactory));
        return new(_filter, _name, (instance, _) => valueFactory(instance));
    }

    /// <summary>Inject a value computed from the instance and the request context.</summary>
    public AddPropertyTerminal<T> From(Func<T, IResponseFilterContext, object?> valueFactory)
    {
        if (valueFactory is null) throw new ArgumentNullException(nameof(valueFactory));
        return new(_filter, _name, valueFactory);
    }
}

/// <summary>Terminal phase of an <c>AddProperty</c> rule — applies the predicate vocabulary.</summary>
public sealed class AddPropertyTerminal<T> : RuleBuilderBase<AddPropertyTerminal<T>, T> where T : class
{
    private readonly string _name;
    private readonly Func<T, IResponseFilterContext, object?> _valueFactory;

    internal AddPropertyTerminal(ResponseFilter<T> filter, string name, Func<T, IResponseFilterContext, object?> valueFactory) : base(filter)
    {
        _name = name;
        _valueFactory = valueFactory;
    }

    protected override void RegisterRule(AsyncPredicate<T> predicate)
    {
        var name = _name;
        var valueFactory = _valueFactory;
        Filter.AddRule(new StructuralEditRule<T>(
            predicate,
            (instance, ctx) => new[] { StructuralEdit.AddProperty(name, valueFactory(instance, ctx)) }));
    }
}
