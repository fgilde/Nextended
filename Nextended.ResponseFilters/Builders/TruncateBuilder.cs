using System;
using Nextended.ResponseFilters.Reflection;
using Nextended.ResponseFilters.Rules;

namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Two-step builder: first specify the cutoff via <c>After(...)</c>,
/// then close with <c>When(...)</c>/<c>Unless(...)</c>/<c>Always()</c>.
/// </summary>
public sealed class TruncateBuilder<T> where T : class
{
    private readonly ResponseFilter<T> _filter;
    private readonly PropertyAccessor _accessor;

    internal TruncateBuilder(ResponseFilter<T> filter, PropertyAccessor accessor)
    {
        _filter = filter;
        _accessor = accessor;
    }

    /// <summary>Truncate after <paramref name="maxLength"/> characters. No suffix appended.</summary>
    public TruncateTerminal<T> After(int maxLength)
    {
        if (maxLength < 0) throw new ArgumentOutOfRangeException(nameof(maxLength));
        return new TruncateTerminal<T>(_filter, _accessor, maxLength, suffix: null);
    }

    /// <summary>Truncate after <paramref name="maxLength"/> and append <paramref name="suffix"/> if a cut occurs.</summary>
    /// <remarks>The final string length is <paramref name="maxLength"/> + suffix length when truncation happens.</remarks>
    public TruncateTerminal<T> After(int maxLength, string suffix)
    {
        if (maxLength < 0) throw new ArgumentOutOfRangeException(nameof(maxLength));
        return new TruncateTerminal<T>(_filter, _accessor, maxLength, suffix ?? string.Empty);
    }
}

/// <summary>Terminal phase of a <c>Truncate</c> rule.</summary>
public sealed class TruncateTerminal<T> : RuleBuilderBase<TruncateTerminal<T>, T> where T : class
{
    private readonly PropertyAccessor _accessor;
    private readonly int _maxLength;
    private readonly string? _suffix;

    internal TruncateTerminal(ResponseFilter<T> filter, PropertyAccessor accessor, int maxLength, string? suffix) : base(filter)
    {
        _accessor = accessor;
        _maxLength = maxLength;
        _suffix = suffix;
    }

    protected override void RegisterRule(AsyncPredicate<T> predicate)
    {
        var maxLength = _maxLength;
        var suffix = _suffix;

        Filter.AddRule(new PropertyMutationRule<T>(
            FilterProperties(_accessor),
            predicate,
            valueProducer: (instance, accessor, _) =>
            {
                if (accessor.GetValue(instance) is not string s) return null;
                if (s.Length <= maxLength) return s;
                return suffix == null
                    ? s.Substring(0, maxLength)
                    : s.Substring(0, maxLength) + suffix;
            }));
    }
}
