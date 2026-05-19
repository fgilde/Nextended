using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Nextended.EF;

/// <summary>
/// Paging and dynamic sorting helpers for <see cref="IQueryable{T}"/>.
/// </summary>
public static class PagingSortingExtensions
{
    /// <summary>
    /// Apply <paramref name="predicate"/> only when <paramref name="condition"/> is true.
    /// </summary>
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> source,
        bool condition,
        Expression<Func<T, bool>> predicate)
        => condition ? source.Where(predicate) : source;

    /// <summary>
    /// Skip and take in one call. <paramref name="pageIndex"/> is zero-based.
    /// </summary>
    public static IQueryable<T> Page<T>(this IQueryable<T> source, int pageIndex, int pageSize)
    {
        if (pageIndex < 0) pageIndex = 0;
        if (pageSize <= 0) return source;
        return source.Skip(pageIndex * pageSize).Take(pageSize);
    }

    /// <summary>
    /// Executes <paramref name="source"/> once to get the page items and once to get the total count,
    /// then assembles a <see cref="PagedResult{T}"/>.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> source,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageIndex < 0) pageIndex = 0;
        if (pageSize <= 0) pageSize = 0;

        var total = await source.CountAsync(cancellationToken);
        var items = pageSize == 0
            ? Array.Empty<T>()
            : await source.Skip(pageIndex * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = total,
            PageIndex = pageIndex,
            PageSize = pageSize,
        };
    }

    /// <summary>
    /// Order by a property identified by name. Supports nested paths via dot notation ("Customer.Name").
    /// </summary>
    public static IOrderedQueryable<T> OrderByMember<T>(
        this IQueryable<T> source,
        string memberPath,
        bool descending = false)
    {
        var (lambda, type) = BuildMemberSelector<T>(memberPath);
        var method = descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy);
        return (IOrderedQueryable<T>)ApplyOrder(source, lambda, type, method);
    }

    /// <summary>
    /// Continue ordering by a second/third/... member. Throws if <paramref name="source"/> isn't
    /// already an ordered queryable — use <see cref="OrderByMember{T}"/> first.
    /// </summary>
    public static IOrderedQueryable<T> ThenByMember<T>(
        this IOrderedQueryable<T> source,
        string memberPath,
        bool descending = false)
    {
        var (lambda, type) = BuildMemberSelector<T>(memberPath);
        var method = descending ? nameof(Queryable.ThenByDescending) : nameof(Queryable.ThenBy);
        return (IOrderedQueryable<T>)ApplyOrder(source, lambda, type, method);
    }

    /// <summary>
    /// Build a multi-column ordering from a list of (member, descending) tuples. Empty input returns
    /// <paramref name="source"/> unchanged.
    /// </summary>
    public static IQueryable<T> OrderByMembers<T>(
        this IQueryable<T> source,
        IEnumerable<(string Member, bool Descending)> orderings)
    {
        IOrderedQueryable<T>? ordered = null;
        foreach (var (member, desc) in orderings)
        {
            ordered = ordered is null
                ? source.OrderByMember(member, desc)
                : ordered.ThenByMember(member, desc);
        }
        return ordered ?? source;
    }

    private static IQueryable ApplyOrder<T>(IQueryable<T> source, LambdaExpression selector, Type memberType, string methodName)
    {
        var call = Expression.Call(
            typeof(Queryable),
            methodName,
            new[] { typeof(T), memberType },
            source.Expression,
            Expression.Quote(selector));
        return source.Provider.CreateQuery(call);
    }

    private static (LambdaExpression Lambda, Type MemberType) BuildMemberSelector<T>(string memberPath)
    {
        if (string.IsNullOrWhiteSpace(memberPath))
            throw new ArgumentException("Member path must not be empty.", nameof(memberPath));

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression body = parameter;

        foreach (var part in memberPath.Split('.'))
        {
            var info = body.Type.GetProperty(part, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
                       ?? throw new ArgumentException($"Property '{part}' not found on type '{body.Type.Name}'.", nameof(memberPath));
            body = Expression.Property(body, info);
        }

        var delegateType = typeof(Func<,>).MakeGenericType(typeof(T), body.Type);
        return (Expression.Lambda(delegateType, body, parameter), body.Type);
    }
}
