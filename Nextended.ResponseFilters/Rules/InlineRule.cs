using System;
using System.Threading.Tasks;

namespace Nextended.ResponseFilters.Rules;

/// <summary>
/// Adapter rule: wraps an arbitrary async lambda as an <see cref="IResponseFilterRule{T}"/>.
/// Used by <c>Apply</c>/extension builders that don't fit the property-mutation shape.
/// </summary>
internal sealed class InlineRule<T> : IResponseFilterRule<T>
{
    private readonly Func<T, IResponseFilterContext, Task> _action;

    public InlineRule(Func<T, IResponseFilterContext, Task> action)
    {
        _action = action;
    }

    public async ValueTask ApplyAsync(T instance, IResponseFilterContext context)
    {
        await _action(instance, context).ConfigureAwait(false);
    }
}
