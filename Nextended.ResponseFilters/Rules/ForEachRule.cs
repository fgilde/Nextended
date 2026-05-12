using System.Collections;
using System.Threading.Tasks;
using Nextended.ResponseFilters.Reflection;

namespace Nextended.ResponseFilters.Rules;

/// <summary>
/// Rule that iterates a property of type <see cref="IEnumerable"/> and applies a sub-filter to each element.
/// </summary>
internal sealed class ForEachRule<T, TItem> : IResponseFilterRule<T>
    where T : class
    where TItem : class
{
    private readonly PropertyAccessor _accessor;
    private readonly ResponseFilter<TItem> _subFilter;

    public ForEachRule(PropertyAccessor accessor, ResponseFilter<TItem> subFilter)
    {
        _accessor = accessor;
        _subFilter = subFilter;
    }

    public async ValueTask ApplyAsync(T instance, IResponseFilterContext context)
    {
        var collection = _accessor.GetValue(instance);
        if (collection is not IEnumerable enumerable)
        {
            return;
        }

        foreach (var item in enumerable)
        {
            if (item is TItem typed)
            {
                await _subFilter.ApplyAsync(typed, context).ConfigureAwait(false);
            }
        }
    }
}
