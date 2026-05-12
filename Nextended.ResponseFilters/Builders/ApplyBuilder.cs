using System;
using System.Threading.Tasks;

namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Builder for the catch-all <see cref="ResponseFilter{T}.Apply(System.Action{T, IResponseFilterContext})"/>
/// rule: runs an arbitrary <see cref="Action"/> on the instance when the predicate matches.
/// </summary>
/// <remarks>
/// Use when none of the structured builders (<c>Nullify</c>, <c>SetValue</c>, <c>Mask</c>, …) fit.
/// Common cases: setting cross-property flags, removing items from a collection, lazy-loading.
/// <para>
/// The action receives a strongly-typed <typeparamref name="T"/> and the context. It can be sync or
/// async (<see cref="Func{T, IResponseFilterContext, Task}"/>). Exceptions are caught at the pipeline
/// level and logged — they will not crash the response.
/// </para>
/// </remarks>
public sealed class ApplyBuilder<T> : RuleBuilderBase<ApplyBuilder<T>, T> where T : class
{
    private readonly Func<T, IResponseFilterContext, Task> _action;

    internal ApplyBuilder(ResponseFilter<T> filter, Action<T, IResponseFilterContext> action) : base(filter)
    {
        _action = (instance, ctx) =>
        {
            action(instance, ctx);
            return Task.CompletedTask;
        };
    }

    internal ApplyBuilder(ResponseFilter<T> filter, Func<T, IResponseFilterContext, Task> action) : base(filter)
    {
        _action = action;
    }

    protected override void RegisterRule(AsyncPredicate<T> predicate)
    {
        var action = _action;
        Filter.AddRule(new Rules.InlineRule<T>(async (instance, ctx) =>
        {
            if (!await predicate(instance, ctx).ConfigureAwait(false)) return;
            await action(instance, ctx).ConfigureAwait(false);
        }));
    }
}
