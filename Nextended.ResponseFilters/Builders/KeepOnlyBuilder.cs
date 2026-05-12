using Nextended.ResponseFilters.Reflection;

namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Mirror of <see cref="RemoveItemsBuilder{T, TItem}"/> with inverted semantics:
/// items matching the predicate are <em>kept</em>; everything else is removed.
/// </summary>
public sealed class KeepOnlyBuilder<T, TItem> where T : class
{
    private readonly ResponseFilter<T> _filter;
    private readonly PropertyAccessor _accessor;

    internal KeepOnlyBuilder(ResponseFilter<T> filter, PropertyAccessor accessor)
    {
        _filter = filter;
        _accessor = accessor;
    }

    public RemoveItemsTerminal<T, TItem> Where(SyncPredicate<TItem> itemPredicate)
        => RemoveItemsTerminal<T, TItem>.AsKeeper(_filter, _accessor,
            (item, ctx) => new System.Threading.Tasks.ValueTask<bool>(itemPredicate(item, ctx)));

    public RemoveItemsTerminal<T, TItem> Where(AsyncPredicate<TItem> itemPredicate)
        => RemoveItemsTerminal<T, TItem>.AsKeeper(_filter, _accessor, itemPredicate);

    public RemoveItemsTerminal<T, TItem> Where(System.Func<TItem, bool> itemPredicate)
        => RemoveItemsTerminal<T, TItem>.AsKeeper(_filter, _accessor,
            (item, _) => new System.Threading.Tasks.ValueTask<bool>(itemPredicate(item)));
}
