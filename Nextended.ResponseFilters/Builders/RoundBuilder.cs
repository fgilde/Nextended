using System;
using Nextended.ResponseFilters.Reflection;
using Nextended.ResponseFilters.Rules;

namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Two-step builder: first specify the precision via <c>To(n)</c>, then close with the standard
/// predicate vocabulary.
/// </summary>
/// <remarks>
/// Detects the runtime type (<see cref="decimal"/>, <see cref="double"/>, plus their nullable variants)
/// and uses <see cref="Math.Round(decimal, int, MidpointRounding)"/> / <see cref="Math.Round(double, int, MidpointRounding)"/>.
/// Property selectors can be of any numeric type — unknown types pass through unchanged.
/// </remarks>
public sealed class RoundBuilder<T> where T : class
{
    private readonly ResponseFilter<T> _filter;
    private readonly PropertyAccessor _accessor;

    internal RoundBuilder(ResponseFilter<T> filter, PropertyAccessor accessor)
    {
        _filter = filter;
        _accessor = accessor;
    }

    /// <summary>Round to <paramref name="decimals"/> places using <see cref="MidpointRounding.ToEven"/> (banker's rounding).</summary>
    public RoundTerminal<T> To(int decimals) => new(_filter, _accessor, decimals, MidpointRounding.ToEven);

    /// <summary>Round to <paramref name="decimals"/> places with an explicit midpoint rule.</summary>
    public RoundTerminal<T> To(int decimals, MidpointRounding mode) => new(_filter, _accessor, decimals, mode);

    /// <summary>Round to whole numbers (0 decimal places).</summary>
    public RoundTerminal<T> ToInteger() => new(_filter, _accessor, 0, MidpointRounding.ToEven);
}

/// <summary>Terminal phase of a <c>Round</c> rule.</summary>
public sealed class RoundTerminal<T> : RuleBuilderBase<RoundTerminal<T>, T> where T : class
{
    private readonly PropertyAccessor _accessor;
    private readonly int _decimals;
    private readonly MidpointRounding _mode;

    internal RoundTerminal(ResponseFilter<T> filter, PropertyAccessor accessor, int decimals, MidpointRounding mode) : base(filter)
    {
        _accessor = accessor;
        _decimals = decimals;
        _mode = mode;
    }

    protected override void RegisterRule(AsyncPredicate<T> predicate)
    {
        var decimals = _decimals;
        var mode = _mode;

        Filter.AddRule(new PropertyMutationRule<T>(
            FilterProperties(_accessor),
            predicate,
            valueProducer: (instance, accessor, _) =>
            {
                var current = accessor.GetValue(instance);
                return current switch
                {
                    null => null,
                    decimal d => Math.Round(d, decimals, mode),
                    double d => Math.Round(d, decimals, mode),
                    float f => (float)Math.Round(f, decimals, mode),
                    _ => current, // non-numeric → pass through; pipeline-level safety net will swallow if SetValue rejects
                };
            }));
    }
}
