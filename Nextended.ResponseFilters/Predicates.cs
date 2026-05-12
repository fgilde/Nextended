using System;
using System.Threading.Tasks;

namespace Nextended.ResponseFilters;

/// <summary>Async predicate used by rule builders.</summary>
/// <remarks>
/// <see cref="ValueTask{TResult}"/> keeps the synchronous fast-path cheap (most predicates are sync).
/// </remarks>
public delegate ValueTask<bool> AsyncPredicate<in T>(T instance, IResponseFilterContext context);

/// <summary>Sync predicate used by rule builders.</summary>
public delegate bool SyncPredicate<in T>(T instance, IResponseFilterContext context);

internal static class PredicateExtensions
{
    public static AsyncPredicate<T> ToAsync<T>(this SyncPredicate<T> sync)
        => (instance, ctx) => new ValueTask<bool>(sync(instance, ctx));

    public static AsyncPredicate<T> ToAsync<T>(this Func<bool> sync)
        => (_, _) => new ValueTask<bool>(sync());
}
