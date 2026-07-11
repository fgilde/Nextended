using System;
using System.Collections.Generic;
using Nextended.ResponseFilters.Reflection;
using Nextended.ResponseFilters.Rules;

namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Two-step builder for limiting a collection property to the first <c>N</c> elements
/// (or the last <c>N</c>).
/// </summary>
/// <remarks>
/// Mutates an <see cref="IList{T}"/> in-place when writable; otherwise rebuilds as a
/// <see cref="List{T}"/> and assigns via the setter.
/// </remarks>
public sealed class TakeBuilder<T, TItem> where T : class
{
    private readonly ResponseFilter<T> _filter;
    private readonly PropertyAccessor _accessor;

    internal TakeBuilder(ResponseFilter<T> filter, PropertyAccessor accessor)
    {
        _filter = filter;
        _accessor = accessor;
    }

    /// <summary>Keep only the first <paramref name="count"/> items.</summary>
    public TakeTerminal<T, TItem> First(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        return new TakeTerminal<T, TItem>(_filter, _accessor, count, fromEnd: false);
    }

    /// <summary>Keep only the last <paramref name="count"/> items.</summary>
    public TakeTerminal<T, TItem> Last(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        return new TakeTerminal<T, TItem>(_filter, _accessor, count, fromEnd: true);
    }
}

/// <summary>Terminal phase of a <c>Take</c> rule.</summary>
public sealed class TakeTerminal<T, TItem> : RuleBuilderBase<TakeTerminal<T, TItem>, T> where T : class
{
    private readonly PropertyAccessor _accessor;
    private readonly int _count;
    private readonly bool _fromEnd;

    internal TakeTerminal(ResponseFilter<T> filter, PropertyAccessor accessor, int count, bool fromEnd) : base(filter)
    {
        _accessor = accessor;
        _count = count;
        _fromEnd = fromEnd;
    }

    protected override void RegisterRule(AsyncPredicate<T> outerPredicate)
    {
        if (!PropertyAllowed(_accessor)) return; // WhenProperty gate on the collection property

        var accessor = _accessor;
        var count = _count;
        var fromEnd = _fromEnd;

        Filter.AddRule(new InlineRule<T>(async (instance, ctx) =>
        {
            if (!await outerPredicate(instance, ctx).ConfigureAwait(false)) return;

            var collection = accessor.GetValue(instance);
            if (collection is null) return;

            if (collection is IList<TItem> writable && !writable.IsReadOnly)
            {
                TrimList(writable, count, fromEnd);
                return;
            }

            if (collection is IEnumerable<TItem> enumerable)
            {
                var asList = new List<TItem>(enumerable);
                TrimList(asList, count, fromEnd);
                object? toAssign = accessor.PropertyType.IsArray ? (object)asList.ToArray() : asList;
                accessor.SetValue(instance, toAssign);
            }
        }));
    }

    private static void TrimList(IList<TItem> list, int count, bool fromEnd)
    {
        if (list.Count <= count) return;
        if (fromEnd)
        {
            var toRemove = list.Count - count;
            for (var i = 0; i < toRemove; i++) list.RemoveAt(0);
        }
        else
        {
            for (var i = list.Count - 1; i >= count; i--) list.RemoveAt(i);
        }
    }
}
