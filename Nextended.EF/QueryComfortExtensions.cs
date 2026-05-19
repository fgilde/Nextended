using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Nextended.EF;

/// <summary>
/// Convenience extensions that round out common query patterns: range filters, set membership,
/// conditional Include / tracking, existence checks.
/// </summary>
public static class QueryComfortExtensions
{
    /// <summary>
    /// Filters where <paramref name="selector"/>'s value is within [<paramref name="from"/>, <paramref name="to"/>] inclusive.
    /// </summary>
    public static IQueryable<T> WhereBetween<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey>> selector,
        TKey from,
        TKey to) where TKey : IComparable<TKey>
    {
        var parameter = selector.Parameters[0];
        var fromConst = Expression.Constant(from, typeof(TKey));
        var toConst = Expression.Constant(to, typeof(TKey));
        var greaterOrEqual = Expression.GreaterThanOrEqual(selector.Body, fromConst);
        var lessOrEqual = Expression.LessThanOrEqual(selector.Body, toConst);
        var body = Expression.AndAlso(greaterOrEqual, lessOrEqual);
        return source.Where(Expression.Lambda<Func<T, bool>>(body, parameter));
    }

    /// <summary>
    /// Filters where <paramref name="selector"/>'s value is contained in <paramref name="values"/>.
    /// Empty <paramref name="values"/> yields an empty query (deferred).
    /// </summary>
    public static IQueryable<T> WhereIn<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey>> selector,
        IEnumerable<TKey> values)
    {
        var materialized = values as ICollection<TKey> ?? values?.ToList();
        if (materialized is null || materialized.Count == 0)
            return source.Where(_ => false);

        var parameter = selector.Parameters[0];
        var valuesConst = Expression.Constant(materialized, typeof(IEnumerable<TKey>));
        var containsCall = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            new[] { typeof(TKey) },
            valuesConst, selector.Body);
        return source.Where(Expression.Lambda<Func<T, bool>>(containsCall, parameter));
    }

    /// <summary>Adds an <c>Include</c> only if <paramref name="condition"/> is true.</summary>
    public static IQueryable<T> IncludeIf<T, TProperty>(
        this IQueryable<T> source,
        bool condition,
        Expression<Func<T, TProperty>> navigation) where T : class
        => condition ? source.Include(navigation) : source;

    /// <summary>Adds a string-based <c>Include</c> only if <paramref name="condition"/> is true.</summary>
    public static IQueryable<T> IncludeIf<T>(
        this IQueryable<T> source,
        bool condition,
        string navigationPath) where T : class
        => condition ? source.Include(navigationPath) : source;

    /// <summary>Apply <c>AsNoTracking</c> only when <paramref name="condition"/> is true.</summary>
    public static IQueryable<T> AsNoTrackingIf<T>(this IQueryable<T> source, bool condition) where T : class
        => condition ? source.AsNoTracking() : source;

    /// <summary>Apply <c>AsTracking</c> only when <paramref name="condition"/> is true.</summary>
    public static IQueryable<T> AsTrackingIf<T>(this IQueryable<T> source, bool condition) where T : class
        => condition ? source.AsTracking() : source;

    /// <summary>
    /// Returns true if at least one element matches <paramref name="predicate"/>.
    /// Thin wrapper around <see cref="EntityFrameworkQueryableExtensions.AnyAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
    /// for naming symmetry with synchronous code.
    /// </summary>
    public static Task<bool> ExistsAsync<T>(
        this IQueryable<T> source,
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => source.AnyAsync(predicate, cancellationToken);

    /// <summary>Returns true if the query has any element.</summary>
    public static Task<bool> ExistsAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default)
        => source.AnyAsync(cancellationToken);
}
