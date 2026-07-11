using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Nextended.ResponseFilters.Reflection;

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

    private Func<PropertyInfo, bool>? _propertyFilter;

    protected RuleBuilderBase(ResponseFilter<T> filter)
    {
        Filter = filter;
    }

    // -------------------------------------------------------------------------
    // WhenProperty — metadata-aware, build-time property gate
    // -------------------------------------------------------------------------

    /// <summary>
    /// Restrict the rule to the target properties whose <see cref="PropertyInfo"/> matches
    /// <paramref name="predicate"/> (e.g. carry a certain attribute). Because property metadata is
    /// static, this is evaluated once at build time — the non-matching properties are simply dropped
    /// from the rule, at zero runtime cost. Composes with <c>When</c>/<c>Unless</c> and can be chained
    /// multiple times (logical AND).
    /// </summary>
    /// <example>
    /// <code>
    /// Nullify(x => x.A, x => x.B).WhenProperty(p => p.GetCustomAttribute&lt;SecretAttribute&gt;() != null).Always();
    /// Remove(x => x.Token).WhenProperty(p => p.PropertyType == typeof(string));
    /// </code>
    /// </example>
    /// <remarks>Applies to property-targeting builders; it has no effect on <c>Apply</c> (which targets no property).</remarks>
    public TBuilder WhenProperty(Func<PropertyInfo, bool> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        var existing = _propertyFilter;
        _propertyFilter = existing is null ? predicate : p => existing(p) && predicate(p);
        return (TBuilder)this;
    }

    /// <summary>Apply the <see cref="WhenProperty"/> gate to a candidate accessor set (subclasses call this in <see cref="RegisterRule"/>).</summary>
    protected PropertyAccessor[] FilterProperties(params PropertyAccessor[] accessors)
        => _propertyFilter is null ? accessors : accessors.Where(a => _propertyFilter(a.Property)).ToArray();

    /// <summary>Whether a single target property passes the <see cref="WhenProperty"/> gate (for whole-rule gating).</summary>
    protected bool PropertyAllowed(PropertyAccessor accessor)
        => _propertyFilter is null || _propertyFilter(accessor.Property);

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
