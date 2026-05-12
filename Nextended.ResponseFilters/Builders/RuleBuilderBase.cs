using System;
using System.Threading.Tasks;

namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Common terminal vocabulary (<c>When</c>, <c>Unless</c>, <c>Always</c>, <c>WhenAll</c>, <c>WhenAny</c>)
/// shared by all rule builders. Materializes the rule and registers it on the owning filter when a
/// terminal is called.
/// </summary>
/// <remarks>
/// Every terminal accepts predicates in any of these shapes (sync and async, with or without
/// instance/context):
/// <list type="bullet">
///   <item><c>Func&lt;bool&gt;</c> / <c>Func&lt;Task&lt;bool&gt;&gt;</c> — no-arg, e.g. for feature flags.</item>
///   <item><c>Func&lt;ctx, bool&gt;</c> / <c>Func&lt;ctx, Task&lt;bool&gt;&gt;</c> — for predicates that only care about the request context (permissions, current user…).</item>
///   <item><c>Func&lt;T, bool&gt;</c> / <c>Func&lt;T, Task&lt;bool&gt;&gt;</c> — for pure instance checks.</item>
///   <item><see cref="SyncPredicate{T}"/> / <see cref="AsyncPredicate{T}"/> — the canonical (instance, ctx) shape.</item>
/// </list>
/// All overloads adapt to the canonical <see cref="AsyncPredicate{T}"/> internally.
/// </remarks>
public abstract class RuleBuilderBase<TBuilder, T> : IRuleBuilder<T>
    where TBuilder : RuleBuilderBase<TBuilder, T>
    where T : class
{
    protected readonly ResponseFilter<T> Filter;

    protected RuleBuilderBase(ResponseFilter<T> filter)
    {
        Filter = filter;
    }

    // -------------------------------------------------------------------------
    // When — full predicate vocabulary
    // -------------------------------------------------------------------------

    /// <summary>Register the rule under the canonical async predicate.</summary>
    public ResponseFilter<T> When(AsyncPredicate<T> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        RegisterRule(predicate);
        return Filter;
    }

    /// <summary>Register the rule under the canonical sync predicate.</summary>
    public ResponseFilter<T> When(SyncPredicate<T> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return When((instance, ctx) => new ValueTask<bool>(predicate(instance, ctx)));
    }

    /// <summary>Register the rule under a context-only sync predicate.</summary>
    public ResponseFilter<T> When(Func<IResponseFilterContext, bool> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return When((_, ctx) => new ValueTask<bool>(predicate(ctx)));
    }

    /// <summary>Register the rule under a context-only async predicate (Task).</summary>
    public ResponseFilter<T> When(Func<IResponseFilterContext, Task<bool>> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return When(async (_, ctx) => await predicate(ctx).ConfigureAwait(false));
    }

    /// <summary>Register the rule under an instance-only sync predicate.</summary>
    public ResponseFilter<T> When(Func<T, bool> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return When((instance, _) => new ValueTask<bool>(predicate(instance)));
    }

    /// <summary>Register the rule under an instance-only async predicate (Task).</summary>
    public ResponseFilter<T> When(Func<T, Task<bool>> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return When(async (instance, _) => await predicate(instance).ConfigureAwait(false));
    }

    /// <summary>Register the rule under a no-arg sync predicate (e.g. a feature flag).</summary>
    public ResponseFilter<T> When(Func<bool> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return When((_, _) => new ValueTask<bool>(predicate()));
    }

    /// <summary>Register the rule under a no-arg async predicate (Task).</summary>
    public ResponseFilter<T> When(Func<Task<bool>> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return When(async (_, _) => await predicate().ConfigureAwait(false));
    }

    /// <summary>Register the rule under an unconditional predicate (always fires).</summary>
    public ResponseFilter<T> Always()
        => When((_, _) => new ValueTask<bool>(true));

    // -------------------------------------------------------------------------
    // Unless — inverted versions of every When overload
    // -------------------------------------------------------------------------

    /// <summary>Register the rule with an inverted canonical sync predicate.</summary>
    public ResponseFilter<T> Unless(SyncPredicate<T> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return When((instance, ctx) => new ValueTask<bool>(!predicate(instance, ctx)));
    }

    /// <summary>Register the rule with an inverted canonical async predicate.</summary>
    public ResponseFilter<T> Unless(AsyncPredicate<T> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return When(async (instance, ctx) => !await predicate(instance, ctx).ConfigureAwait(false));
    }

    /// <summary>Register the rule with an inverted context-only sync predicate.</summary>
    public ResponseFilter<T> Unless(Func<IResponseFilterContext, bool> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return When((_, ctx) => new ValueTask<bool>(!predicate(ctx)));
    }

    /// <summary>Register the rule with an inverted context-only async predicate (Task).</summary>
    public ResponseFilter<T> Unless(Func<IResponseFilterContext, Task<bool>> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return When(async (_, ctx) => !await predicate(ctx).ConfigureAwait(false));
    }

    /// <summary>Register the rule with an inverted instance-only sync predicate.</summary>
    public ResponseFilter<T> Unless(Func<T, bool> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return When((instance, _) => new ValueTask<bool>(!predicate(instance)));
    }

    /// <summary>Register the rule with an inverted instance-only async predicate (Task).</summary>
    public ResponseFilter<T> Unless(Func<T, Task<bool>> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return When(async (instance, _) => !await predicate(instance).ConfigureAwait(false));
    }

    /// <summary>Register the rule with an inverted no-arg sync predicate.</summary>
    public ResponseFilter<T> Unless(Func<bool> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return When((_, _) => new ValueTask<bool>(!predicate()));
    }

    /// <summary>Register the rule with an inverted no-arg async predicate (Task).</summary>
    public ResponseFilter<T> Unless(Func<Task<bool>> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return When(async (_, _) => !await predicate().ConfigureAwait(false));
    }

    // -------------------------------------------------------------------------
    // Combinators
    // -------------------------------------------------------------------------

    /// <summary>Register the rule under the logical AND of multiple predicates (short-circuits on first false).</summary>
    public ResponseFilter<T> WhenAll(params AsyncPredicate<T>[] predicates)
    {
        if (predicates is null || predicates.Length == 0)
        {
            throw new ArgumentException("At least one predicate required.", nameof(predicates));
        }
        return When(async (instance, ctx) =>
        {
            foreach (var p in predicates)
            {
                if (!await p(instance, ctx).ConfigureAwait(false)) return false;
            }
            return true;
        });
    }

    /// <summary>Register the rule under the logical OR of multiple predicates (short-circuits on first true).</summary>
    public ResponseFilter<T> WhenAny(params AsyncPredicate<T>[] predicates)
    {
        if (predicates is null || predicates.Length == 0)
        {
            throw new ArgumentException("At least one predicate required.", nameof(predicates));
        }
        return When(async (instance, ctx) =>
        {
            foreach (var p in predicates)
            {
                if (await p(instance, ctx).ConfigureAwait(false)) return true;
            }
            return false;
        });
    }

    /// <summary>Subclasses materialize and register the concrete rule here, given the resolved predicate.</summary>
    protected abstract void RegisterRule(AsyncPredicate<T> predicate);
}
