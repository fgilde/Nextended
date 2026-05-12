namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Marker interface for every fluent rule builder produced by <see cref="ResponseFilter{T}"/>.
/// </summary>
/// <remarks>
/// Carries the target DTO type <typeparamref name="T"/> so consumer-side extension methods
/// (<c>this IRuleBuilder&lt;T&gt; builder</c>) can infer the generic without having to repeat
/// the F-bounded <c>TBuilder</c> at every call site.
/// <para>
/// Exposes the canonical <see cref="When(AsyncPredicate{T})"/> overload — all other predicate
/// shapes still live on the concrete <see cref="Builders.RuleBuilderBase{TBuilder, T}"/> so the
/// fluent surface stays unchanged.
/// </para>
/// </remarks>
public interface IRuleBuilder<T> where T : class
{
    /// <summary>Register the rule under the canonical async predicate.</summary>
    ResponseFilter<T> When(AsyncPredicate<T> predicate);
}
