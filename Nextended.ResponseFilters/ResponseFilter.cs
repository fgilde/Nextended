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
        var accessors = ResolveSelectors(selectors);
        return new NullifyBuilder<T>(this, accessors);
    }

    /// <summary>Begin a rule that replaces a property with a fixed value or a per-instance value.</summary>
    protected ReplaceBuilder<T, TProp> Replace<TProp>(Expression<Func<T, TProp>> selector)
    {
        var accessor = PropertyAccessor.For(PropertySelector.Resolve(selector));
        return new ReplaceBuilder<T, TProp>(this, accessor);
    }

    /// <summary>
    /// Begin a rule that sets a property to a value. Functionally identical to <see cref="Replace{TProp}"/>
    /// but reads more naturally when there isn't necessarily an "existing" value being replaced
    /// (closes with <c>.To(...)</c> instead of <c>.With(...)</c>).
    /// </summary>
    protected SetValueBuilder<T, TProp> SetValue<TProp>(Expression<Func<T, TProp>> selector)
    {
        var accessor = PropertyAccessor.For(PropertySelector.Resolve(selector));
        return new SetValueBuilder<T, TProp>(this, accessor);
    }

    /// <summary>
    /// Begin a rule that resets one or more properties to <c>default(TProperty)</c>.
    /// Accepts heterogeneous property types in a single call (each property gets the default of its own type).
    /// </summary>
    /// <example>
    /// <code>SetToDefault(x =&gt; x.Cost, x =&gt; x.IsActive, x =&gt; x.Notes).When(...);</code>
    /// </example>
    protected SetToDefaultBuilder<T> SetToDefault(params Expression<Func<T, object?>>[] selectors)
    {
        var accessors = ResolveSelectors(selectors);
        return new SetToDefaultBuilder<T>(this, accessors);
    }

    /// <summary>Begin a rule that transforms a property in place using a function.</summary>
    protected TransformBuilder<T, TProp> Transform<TProp>(Expression<Func<T, TProp>> selector)
    {
        var accessor = PropertyAccessor.For(PropertySelector.Resolve(selector));
        return new TransformBuilder<T, TProp>(this, accessor);
    }

    /// <summary>Begin a rule that masks a <see cref="string"/> property (e.g. <c>***@***.***</c>).</summary>
    protected MaskBuilder<T> Mask(Expression<Func<T, string?>> selector)
    {
        var accessor = PropertyAccessor.For(PropertySelector.Resolve(selector));
        return new MaskBuilder<T>(this, accessor);
    }

    /// <summary>Begin a rule that truncates a <see cref="string"/> property after N characters.</summary>
    protected TruncateBuilder<T> Truncate(Expression<Func<T, string?>> selector)
    {
        var accessor = PropertyAccessor.For(PropertySelector.Resolve(selector));
        return new TruncateBuilder<T>(this, accessor);
    }

    /// <summary>
    /// Begin an "escape hatch" rule: run an arbitrary action on the instance when the predicate matches.
    /// Use when the structured builders don't fit (e.g. mutating multiple unrelated properties together).
    /// </summary>
    protected ApplyBuilder<T> Apply(Action<T, IResponseFilterContext> action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        return new ApplyBuilder<T>(this, action);
    }

    /// <inheritdoc cref="Apply(Action{T, IResponseFilterContext})"/>
    protected ApplyBuilder<T> ApplyAsync(Func<T, IResponseFilterContext, Task> action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        return new ApplyBuilder<T>(this, action);
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

    /// <summary>Begin a rule that removes items from a collection where the predicate matches.</summary>
    protected RemoveItemsBuilder<T, TItem> RemoveItems<TItem>(Expression<Func<T, IEnumerable<TItem>?>> selector)
    {
        var accessor = PropertyAccessor.For(PropertySelector.Resolve(selector));
        return new RemoveItemsBuilder<T, TItem>(this, accessor);
    }

    /// <summary>Inverse of <see cref="RemoveItems{TItem}"/>: keep only items where the predicate matches.</summary>
    protected KeepOnlyBuilder<T, TItem> KeepOnly<TItem>(Expression<Func<T, IEnumerable<TItem>?>> selector)
    {
        var accessor = PropertyAccessor.For(PropertySelector.Resolve(selector));
        return new KeepOnlyBuilder<T, TItem>(this, accessor);
    }

    /// <summary>Begin a rule that limits a collection to the first/last N items.</summary>
    protected TakeBuilder<T, TItem> Take<TItem>(Expression<Func<T, IEnumerable<TItem>?>> selector)
    {
        var accessor = PropertyAccessor.For(PropertySelector.Resolve(selector));
        return new TakeBuilder<T, TItem>(this, accessor);
    }

    /// <summary>Begin a rule that hashes a string property (SHA-256 hex by default).</summary>
    protected HashBuilder<T> Hash(Expression<Func<T, string?>> selector)
    {
        var accessor = PropertyAccessor.For(PropertySelector.Resolve(selector));
        return new HashBuilder<T>(this, accessor);
    }

    /// <summary>Begin a rule that rounds a numeric property to N decimals.</summary>
    /// <remarks>
    /// <typeparamref name="TNum"/> is constrained to <see cref="System.Numerics.INumber{TSelf}"/>,
    /// which compile-time prevents accidental use on non-numeric properties. Runtime rounding is
    /// applied for <see cref="decimal"/>, <see cref="double"/>, and <see cref="float"/>; other
    /// numeric types (e.g. integers, BigInteger) pass through unchanged.
    /// </remarks>
    protected RoundBuilder<T> Round<TNum>(Expression<Func<T, TNum>> selector)
        where TNum : System.Numerics.INumber<TNum>
    {
        var accessor = PropertyAccessor.For(PropertySelector.Resolve(selector));
        return new RoundBuilder<T>(this, accessor);
    }

    /// <inheritdoc cref="Round{TNum}(Expression{Func{T, TNum}})"/>
    protected RoundBuilder<T> Round<TNum>(Expression<Func<T, TNum?>> selector)
        where TNum : struct, System.Numerics.INumber<TNum>
    {
        var accessor = PropertyAccessor.For(PropertySelector.Resolve(selector));
        return new RoundBuilder<T>(this, accessor);
    }

    /// <summary>Begin a rule that empties a property (string → "", IList → in-place Clear, arrays → empty).</summary>
    protected ClearBuilder<T> Clear<TProp>(Expression<Func<T, TProp>> selector)
    {
        var accessor = PropertyAccessor.For(PropertySelector.Resolve(selector));
        return new ClearBuilder<T>(this, accessor);
    }

    private static PropertyAccessor[] ResolveSelectors<TProp>(Expression<Func<T, TProp>>[] selectors)
    {
        if (selectors is null || selectors.Length == 0)
        {
            throw new ArgumentException("At least one selector is required.", nameof(selectors));
        }
        return selectors.Select(s => PropertyAccessor.For(PropertySelector.Resolve(s))).ToArray();
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
    public new SetValueBuilder<T, TProp> SetValue<TProp>(Expression<Func<T, TProp>> selector) => base.SetValue(selector);
    public new SetToDefaultBuilder<T> SetToDefault(params Expression<Func<T, object?>>[] selectors) => base.SetToDefault(selectors);
    public new TransformBuilder<T, TProp> Transform<TProp>(Expression<Func<T, TProp>> selector) => base.Transform(selector);
    public new MaskBuilder<T> Mask(Expression<Func<T, string?>> selector) => base.Mask(selector);
    public new TruncateBuilder<T> Truncate(Expression<Func<T, string?>> selector) => base.Truncate(selector);
    public new ApplyBuilder<T> Apply(Action<T, IResponseFilterContext> action) => base.Apply(action);
    public new ApplyBuilder<T> ApplyAsync(Func<T, IResponseFilterContext, Task> action) => base.ApplyAsync(action);
    public new ResponseFilter<T> ForEach<TItem>(Expression<Func<T, IEnumerable<TItem>?>> selector, Action<InlineFilter<TItem>> configure) where TItem : class
        => base.ForEach(selector, configure);
    public new RemoveItemsBuilder<T, TItem> RemoveItems<TItem>(Expression<Func<T, IEnumerable<TItem>?>> selector) => base.RemoveItems(selector);
    public new KeepOnlyBuilder<T, TItem> KeepOnly<TItem>(Expression<Func<T, IEnumerable<TItem>?>> selector) => base.KeepOnly(selector);
    public new TakeBuilder<T, TItem> Take<TItem>(Expression<Func<T, IEnumerable<TItem>?>> selector) => base.Take(selector);
    public new HashBuilder<T> Hash(Expression<Func<T, string?>> selector) => base.Hash(selector);
    public new RoundBuilder<T> Round<TNum>(Expression<Func<T, TNum>> selector) where TNum : System.Numerics.INumber<TNum> => base.Round(selector);
    public new RoundBuilder<T> Round<TNum>(Expression<Func<T, TNum?>> selector) where TNum : struct, System.Numerics.INumber<TNum> => base.Round(selector);
    public new ClearBuilder<T> Clear<TProp>(Expression<Func<T, TProp>> selector) => base.Clear(selector);
}
