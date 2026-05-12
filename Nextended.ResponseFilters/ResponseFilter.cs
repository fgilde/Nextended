using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Nextended.ResponseFilters.Builders;
using Nextended.ResponseFilters.Reflection;
using Nextended.ResponseFilters.Rules;

namespace Nextended.ResponseFilters;

/// <summary>
/// Base class for declarative response filters. Inherit and configure rules in the constructor
/// via the protected fluent builders (<see cref="Nullify{TProp}"/>, <see cref="Replace{TProp}"/>,
/// <see cref="Transform{TProp}"/>, <see cref="ForEach{TItem}"/>).
/// </summary>
/// <example>
/// <code>
/// public class OrderResponseFilter : ResponseFilter&lt;OrderDto&gt;
/// {
///     public OrderResponseFilter()
///     {
///         Nullify(x =&gt; x.TotalCost).When(NotInRole("Finance"));
///         Replace(x =&gt; x.Email).With("***@***.***").Unless(IsOwner);
///         ForEach(x =&gt; x.Lines, line =&gt; line.Nullify(l =&gt; l.UnitCost).When(NotInRole("Finance")));
///     }
/// }
/// </code>
/// </example>
public abstract class ResponseFilter<T> : IResponseFilter where T : class
{
    private readonly List<IResponseFilterRule<T>> _rules = new();

    public Type TargetType => typeof(T);

    /// <inheritdoc />
    public async ValueTask ApplyAsync(object instance, IResponseFilterContext context)
    {
        if (instance is not T typed) return;
        await ApplyAsync(typed, context).ConfigureAwait(false);
    }

    /// <summary>Strongly-typed entry point (no boxing). Called by the non-generic <see cref="ApplyAsync(object, IResponseFilterContext)"/>.</summary>
    public async ValueTask ApplyAsync(T instance, IResponseFilterContext context)
    {
        if (instance is null) return;

        foreach (var rule in _rules)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            await rule.ApplyAsync(instance, context).ConfigureAwait(false);
        }
    }

    internal void AddRule(IResponseFilterRule<T> rule) => _rules.Add(rule);

    // -------------------------------------------------------------------------
    // Fluent builders
    // -------------------------------------------------------------------------

    /// <summary>Begin a rule that nulls one or more nullable properties when the predicate matches.</summary>
    protected NullifyBuilder<T> Nullify<TProp>(params Expression<Func<T, TProp>>[] selectors)
    {
        if (selectors is null || selectors.Length == 0)
        {
            throw new ArgumentException("At least one selector is required.", nameof(selectors));
        }
        var accessors = selectors.Select(s => PropertyAccessor.For(PropertySelector.Resolve(s))).ToArray();
        return new NullifyBuilder<T>(this, accessors);
    }

    /// <summary>Begin a rule that replaces a property with a fixed value or a per-instance value.</summary>
    protected ReplaceBuilder<T, TProp> Replace<TProp>(Expression<Func<T, TProp>> selector)
    {
        var accessor = PropertyAccessor.For(PropertySelector.Resolve(selector));
        return new ReplaceBuilder<T, TProp>(this, accessor);
    }

    /// <summary>Begin a rule that transforms a property in place using a function.</summary>
    protected TransformBuilder<T, TProp> Transform<TProp>(Expression<Func<T, TProp>> selector)
    {
        var accessor = PropertyAccessor.For(PropertySelector.Resolve(selector));
        return new TransformBuilder<T, TProp>(this, accessor);
    }

    /// <summary>
    /// Recurse into a collection property and apply a sub-filter to every element.
    /// The sub-filter is configured inline via a lambda, reusing the same fluent builders.
    /// </summary>
    protected ResponseFilter<T> ForEach<TItem>(
        Expression<Func<T, IEnumerable<TItem>?>> selector,
        Action<InlineFilter<TItem>> configure)
        where TItem : class
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        var accessor = PropertyAccessor.For(PropertySelector.Resolve(selector));
        var sub = new InlineFilter<TItem>();
        configure(sub);
        _rules.Add(new ForEachRule<T, TItem>(accessor, sub));
        return this;
    }

    // -------------------------------------------------------------------------
    // Predicate sugar — overridable shortcuts subclasses can build on
    // -------------------------------------------------------------------------

    /// <summary>Always-true predicate (rule fires for every instance).</summary>
    protected static SyncPredicate<T> Always() => (_, _) => true;

    /// <summary>Convenience: build a predicate from a context-only lambda.</summary>
    protected static SyncPredicate<T> WhenContext(Func<IResponseFilterContext, bool> predicate)
        => (_, ctx) => predicate(ctx);

    /// <summary>Convenience: build a predicate from an instance-only lambda.</summary>
    protected static SyncPredicate<T> WhenInstance(Func<T, bool> predicate)
        => (instance, _) => predicate(instance);
}

/// <summary>
/// Concrete <see cref="ResponseFilter{T}"/> used internally by <c>ForEach</c> sub-filters,
/// also exposed for ad-hoc filters configured at runtime (e.g. in tests).
/// </summary>
public sealed class InlineFilter<T> : ResponseFilter<T> where T : class
{
    // Re-expose the protected builders publicly so InlineFilter can be configured fluently outside the class hierarchy.
    public new NullifyBuilder<T> Nullify<TProp>(params Expression<Func<T, TProp>>[] selectors) => base.Nullify(selectors);
    public new ReplaceBuilder<T, TProp> Replace<TProp>(Expression<Func<T, TProp>> selector) => base.Replace(selector);
    public new TransformBuilder<T, TProp> Transform<TProp>(Expression<Func<T, TProp>> selector) => base.Transform(selector);
    public new ResponseFilter<T> ForEach<TItem>(Expression<Func<T, IEnumerable<TItem>?>> selector, Action<InlineFilter<TItem>> configure) where TItem : class
        => base.ForEach(selector, configure);
}
