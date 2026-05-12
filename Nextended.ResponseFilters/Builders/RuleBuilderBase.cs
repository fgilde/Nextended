using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Common terminal vocabulary (<c>When</c>, <c>Unless</c>, <c>Always</c>, <c>WhenAll</c>, <c>WhenAny</c>)
/// shared by all rule builders. Materializes the rule and registers it on the owning filter when a
/// terminal is called.
/// </summary>
public abstract class RuleBuilderBase<TBuilder, T>
    where TBuilder : RuleBuilderBase<TBuilder, T>
    where T : class
{
    protected readonly ResponseFilter<T> Filter;

    protected RuleBuilderBase(ResponseFilter<T> filter)
    {
        Filter = filter;
    }

    /// <summary>Register the rule under an async predicate.</summary>
    public ResponseFilter<T> When(AsyncPredicate<T> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        RegisterRule(predicate);
        return Filter;
    }

    /// <summary>Register the rule under a sync predicate.</summary>
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

    /// <summary>Register the rule under an unconditional predicate (always fires).</summary>
    public ResponseFilter<T> Always()
        => When((_, _) => new ValueTask<bool>(true));

    /// <summary>Register the rule with an inverted sync predicate.</summary>
    public ResponseFilter<T> Unless(SyncPredicate<T> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return When((instance, ctx) => new ValueTask<bool>(!predicate(instance, ctx)));
    }

    /// <summary>Register the rule with an inverted async predicate.</summary>
    public ResponseFilter<T> Unless(AsyncPredicate<T> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return When(async (instance, ctx) => !await predicate(instance, ctx).ConfigureAwait(false));
    }

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
