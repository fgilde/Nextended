using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nextended.ResponseFilters.Rules;

/// <summary>
/// Rule that, when its predicate matches, records one or more <see cref="StructuralEdit"/>s for the
/// visited instance into <see cref="IResponseFilterContext.StructuralEdits"/>. The edits themselves
/// are applied later, at serialization time.
/// </summary>
internal sealed class StructuralEditRule<T> : IResponseFilterRule<T> where T : class
{
    private readonly AsyncPredicate<T> _predicate;
    private readonly Func<T, IResponseFilterContext, IEnumerable<StructuralEdit>> _editFactory;

    public StructuralEditRule(
        AsyncPredicate<T> predicate,
        Func<T, IResponseFilterContext, IEnumerable<StructuralEdit>> editFactory)
    {
        _predicate = predicate;
        _editFactory = editFactory;
    }

    public async ValueTask ApplyAsync(T instance, IResponseFilterContext context)
    {
        if (instance is null) return;
        if (!await _predicate(instance, context).ConfigureAwait(false)) return;

        foreach (var edit in _editFactory(instance, context))
        {
            context.StructuralEdits.Record(instance, edit);
        }
    }
}
