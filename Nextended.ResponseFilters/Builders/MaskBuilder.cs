using System;
using Nextended.ResponseFilters.Reflection;
using Nextended.ResponseFilters.Rules;

namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Builder for masking <see cref="string"/> properties.
/// </summary>
/// <remarks>
/// Composes the mask via three optional knobs:
/// <list type="bullet">
///   <item><see cref="KeepFirst(int)"/> — leave the first N characters visible.</item>
///   <item><see cref="KeepLast(int)"/> — leave the last N characters visible.</item>
///   <item><see cref="With(char)"/> — replacement character (default <c>'*'</c>).</item>
/// </list>
/// Or replace the whole value with a fixed pattern via <see cref="WithPattern(string?)"/>.
/// <para>
/// Null inputs are passed through unchanged so the rule can run unconditionally. Closes with
/// the standard <c>When/Unless/Always</c> vocabulary.
/// </para>
/// </remarks>
public sealed class MaskBuilder<T> : RuleBuilderBase<MaskBuilder<T>, T> where T : class
{
    private readonly PropertyAccessor _accessor;
    private int _keepFirst;
    private int _keepLast;
    private char _maskChar = '*';
    private string? _fixedPattern;

    internal MaskBuilder(ResponseFilter<T> filter, PropertyAccessor accessor) : base(filter)
    {
        _accessor = accessor;
    }

    /// <summary>Keep the first <paramref name="count"/> characters visible. Capped at the string length.</summary>
    public MaskBuilder<T> KeepFirst(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        _keepFirst = count;
        return this;
    }

    /// <summary>Keep the last <paramref name="count"/> characters visible. Capped at the string length.</summary>
    public MaskBuilder<T> KeepLast(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        _keepLast = count;
        return this;
    }

    /// <summary>Use a different mask character (default: <c>'*'</c>).</summary>
    public MaskBuilder<T> With(char maskChar)
    {
        _maskChar = maskChar;
        return this;
    }

    /// <summary>Replace the whole value with a fixed pattern (ignores Keep* settings).</summary>
    public MaskBuilder<T> WithPattern(string? pattern)
    {
        _fixedPattern = pattern;
        return this;
    }

    protected override void RegisterRule(AsyncPredicate<T> predicate)
    {
        var keepFirst = _keepFirst;
        var keepLast = _keepLast;
        var maskChar = _maskChar;
        var fixedPattern = _fixedPattern;

        Filter.AddRule(new PropertyMutationRule<T>(
            FilterProperties(_accessor),
            predicate,
            valueProducer: (instance, accessor, _) =>
            {
                if (fixedPattern != null)
                {
                    return fixedPattern;
                }
                if (accessor.GetValue(instance) is not string s)
                {
                    return null;
                }
                return MaskString(s, keepFirst, keepLast, maskChar);
            }));
    }

    internal static string MaskString(string s, int keepFirst, int keepLast, char maskChar)
    {
        if (s.Length == 0) return s;

        // Cap the keeps so they don't overlap or exceed length
        if (keepFirst + keepLast >= s.Length)
        {
            // Nothing to mask — but caller asked for it. Fall back to full-mask of the unkept slice
            // if at least one keep is zero; if both fully cover the string, return as-is.
            if (keepFirst == 0 && keepLast == 0)
            {
                return new string(maskChar, s.Length);
            }
            return s;
        }

        var middleLen = s.Length - keepFirst - keepLast;
        return string.Concat(
            s.AsSpan(0, keepFirst).ToString(),
            new string(maskChar, middleLen),
            s.AsSpan(s.Length - keepLast).ToString());
    }
}
