using System;
using System.Threading.Tasks;

namespace Nextended.ResponseFilters;

/// <summary>
/// Non-generic marker for filters keyed by <see cref="TargetType"/>.
/// Implemented by <see cref="ResponseFilter{T}"/>; consumers typically don't implement this directly.
/// </summary>
public interface IResponseFilter
{
    /// <summary>The exact DTO type this filter applies to (no inheritance walking).</summary>
    Type TargetType { get; }

    /// <summary>
    /// Apply all configured rules to <paramref name="instance"/>.
    /// Implementations MUST tolerate <paramref name="instance"/> being of a derived type or null-safe assignable.
    /// </summary>
    ValueTask ApplyAsync(object instance, IResponseFilterContext context);
}

/// <summary>Single rule attached to a <see cref="ResponseFilter{T}"/>.</summary>
public interface IResponseFilterRule<in T>
{
    ValueTask ApplyAsync(T instance, IResponseFilterContext context);
}
