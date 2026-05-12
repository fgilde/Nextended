using System.Collections.Generic;
using Nextended.ResponseFilters.Reflection;
using Nextended.ResponseFilters.Rules;

namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Two-step builder: first specify the per-item predicate via <c>Where(...)</c>, then close with
/// the standard predicate vocabulary (<c>When/Unless/Always/...</c>).
/// </summary>
/// <remarks>
/// <para>
/// Tries to mutate the source collection in-place when it is an <see cref="IList{T}"/> with a writable
/// <c>IsReadOnly = false</c>. Otherwise falls back to building a new <see cref="List{T}"/> of the kept
/// items and assigning it back via the property setter — which means the property must be writable in
/// that case (or you'll get a logged warning at runtime).
/// </para>
/// <para>Use <see cref="ResponseFilter{T}.KeepOnly{TItem}"/> for the inverse semantics.</para>
/// </remarks>
public sealed class RemoveItemsBuilder<T, TItem> where T : class
{
    private readonly ResponseFilter<T> _filter;
    private readonly PropertyAccessor _accessor;

    internal RemoveItemsBuilder(ResponseFilter<T> filter, PropertyAccessor accessor)
    {
        _filter = filter;
        _accessor = accessor;
    }

    /// <summary>Items matching the predicate are removed from the collection.</summary>
    public RemoveItemsTerminal<T, TItem> Where(SyncPredicate<TItem> itemPredicate)
        => new(_filter, _accessor, (item, ctx) => new System.Threading.Tasks.ValueTask<bool>(itemPredicate(item, ctx)));

    /// <summary>Items matching the async predicate are removed.</summary>
    public RemoveItemsTerminal<T, TItem> Where(AsyncPredicate<TItem> itemPredicate)
        => new(_filter, _accessor, itemPredicate);

    /// <summary>Items matching the simple (instance-only) predicate are removed.</summary>
    public RemoveItemsTerminal<T, TItem> Where(System.Func<TItem, bool> itemPredicate)
        => new(_filter, _accessor, (item, _) => new System.Threading.Tasks.ValueTask<bool>(itemPredicate(item)));
}

/// <summary>Terminal phase of a <c>RemoveItems</c> rule.</summary>
public sealed class RemoveItemsTerminal<T, TItem> : RuleBuilderBase<RemoveItemsTerminal<T, TItem>, T> where T : class
{
    private readonly PropertyAccessor _accessor;
    private readonly AsyncPredicate<TItem> _itemPredicate;
    private readonly bool _invert;

    internal RemoveItemsTerminal(ResponseFilter<T> filter, PropertyAccessor accessor, AsyncPredicate<TItem> itemPredicate, bool invert = false) : base(filter)
    {
        _accessor = accessor;
        _itemPredicate = itemPredicate;
        _invert = invert;
    }

    /// <summary>Internal: build a "keep" semantics terminal (used by <c>KeepOnly</c>).</summary>
    internal static RemoveItemsTerminal<T, TItem> AsKeeper(ResponseFilter<T> filter, PropertyAccessor accessor, AsyncPredicate<TItem> keepPredicate)
        => new(filter, accessor, keepPredicate, invert: true);

    protected override void RegisterRule(AsyncPredicate<T> outerPredicate)
    {
        var accessor = _accessor;
        var itemPredicate = _itemPredicate;
        var keep = _invert;

        Filter.AddRule(new InlineRule<T>(async (instance, ctx) =>
        {
            if (!await outerPredicate(instance, ctx).ConfigureAwait(false)) return;

            var collection = accessor.GetValue(instance);
            if (collection is null) return;

            // Fast path: in-place mutation on a writable IList<T>
            if (collection is IList<TItem> writable && !writable.IsReadOnly)
            {
                for (var i = writable.Count - 1; i >= 0; i--)
                {
                    var matches = await itemPredicate(writable[i], ctx).ConfigureAwait(false);
                    var shouldRemove = keep ? !matches : matches;
                    if (shouldRemove) writable.RemoveAt(i);
                }
                return;
            }

            // Fallback: rebuild and assign back via the setter — match the property's runtime shape.
            if (collection is IEnumerable<TItem> enumerable)
            {
                var kept = new List<TItem>();
                foreach (var item in enumerable)
                {
                    var matches = await itemPredicate(item, ctx).ConfigureAwait(false);
                    var shouldRemove = keep ? !matches : matches;
                    if (!shouldRemove) kept.Add(item);
                }
                object? toAssign = accessor.PropertyType.IsArray ? (object)kept.ToArray() : kept;
                accessor.SetValue(instance, toAssign);
            }
        }));
    }
}
