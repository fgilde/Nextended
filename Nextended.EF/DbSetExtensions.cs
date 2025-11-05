using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Nextended.Core.Extensions;

namespace Nextended.EF;


public static class DbSetExtensions
{
    public static async Task<T> LoadGraphAsync<T>(
        this DbContext context,
        T entity,
        int maxDepth = 1,
        HashSet<object>? seen = null
    ) where T : class
    {
        seen ??= new HashSet<object>();
        if (entity == null || maxDepth < 0 || !seen.Add(entity))
            return entity;

        var entry = context.Entry(entity);
        var navigations = entry.Metadata.GetNavigations();

        foreach (var nav in navigations)
        {
            if (nav.IsCollection)
            {
                await entry.Collection(nav.Name).LoadAsync();
                var children = (entry.Collection(nav.Name).CurrentValue
                    as IEnumerable<object>) ?? [];
                foreach (var child in children)
                    await context.LoadGraphAsync(child, maxDepth - 1, seen);
            }
            else
            {
                await entry.Reference(nav.Name).LoadAsync();
                var child = entry.Reference(nav.Name).CurrentValue;
                if (child != null)
                    await context.LoadGraphAsync(child, maxDepth - 1, seen);
            }
        }

        return entity;
    }


    private static DbContext GetDbContextFromSet<T>(DbSet<T> set) where T : class
    {
        var infrastructure = (IInfrastructure<IServiceProvider>)set;
        var serviceProvider = infrastructure.Instance;
        return serviceProvider.GetService(typeof(ICurrentDbContext)) is ICurrentDbContext currentContext
        ? currentContext.Context
            : throw new InvalidOperationException("Could not retrieve DbContext.");
    }

    public static IQueryable<T> IncludeAll<T>(this DbSet<T> set, string[] excludePaths) where T : class
    {
        var context = GetDbContextFromSet(set);
        return set.IncludeAll(context, excludePaths);
    }

    public static IQueryable<T> IncludeAll<T>(this DbSet<T> set, params Expression<Func<T, object>>[] excludeExpressions) where T : class
    {
        var context = GetDbContextFromSet(set);
        return set.IncludeAll(context, excludeExpressions);
    }

    public static IQueryable<T> IncludeAll<T>(this IQueryable<T> query, DbContext context, params Expression<Func<T, object>>[] excludeExpressions) where T : class
    {
        var excludePaths = excludeExpressions?
            .Select(expr => expr.Body.GetPropertyPath())
            .Where(path => !string.IsNullOrEmpty(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new();

        return IncludeNavigations(query, context, typeof(T), "", excludePaths);
    }

    public static IQueryable<T> IncludeAll<T>(this IQueryable<T> query, DbContext context, string[] excludePaths) where T : class
    {
        var excluded = new HashSet<string>(excludePaths ?? [], StringComparer.OrdinalIgnoreCase);
        return IncludeNavigations(query, context, typeof(T), "", excluded);
    }

    public static IIncludableQueryable<TEntity, TProperty2> Include<TEntity, TProperty1, TProperty2>(
        this IQueryable<TEntity> queryable,
        Expression<Func<TEntity, IEnumerable<TProperty1>>> include1,
        Expression<Func<TProperty1, TProperty2>> include2
    ) where TEntity : class =>
        queryable.Include(include1)
            .ThenInclude(include2);

    public static IIncludableQueryable<TEntity, TProperty3> Include<TEntity, TProperty1, TProperty2, TProperty3>(
        this IQueryable<TEntity> queryable,
        Expression<Func<TEntity, IEnumerable<TProperty1>>> include1,
        Expression<Func<TProperty1, TProperty2>> include2,
        Expression<Func<TProperty2, TProperty3>> include3
    ) where TEntity : class =>
        queryable.Include(include1)
            .ThenInclude(include2)
            .ThenInclude(include3);

    public static IQueryable<TEntity> MultiInclude<TEntity, TProperty1>(
        this IQueryable<TEntity> queryable,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, IEnumerable<TProperty1>>> baseInclude,
        params Expression<Func<TProperty1, object?>>[] includes
    ) where TEntity : class
    {
        return includes.Aggregate(
            queryable,
            (current, include) => baseInclude(current)
                .ThenInclude(include)
        );
    }

    public static IQueryable<TEntity> MultiInclude<TEntity, TProperty1>(
        this IQueryable<TEntity> queryable,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, TProperty1>> baseInclude,
        params Expression<Func<TProperty1, object?>>[] includes
    ) where TEntity : class
    {
        return includes.Aggregate(
            queryable,
            (current, include) => baseInclude(current)
                .ThenInclude(include)
        );
    }

    private static IQueryable<T> IncludeNavigations<T>(IQueryable<T> query, DbContext context, Type clrType, string prefix, HashSet<string> excluded) where T : class
    {
        var entityType = context.Model.FindEntityType(clrType);
        var navigations = entityType?.GetNavigations().Where(n => !n.IsOnDependent) ?? [];

        foreach (var navigation in navigations)
        {
            var includePath = string.IsNullOrEmpty(prefix) ? navigation.Name : $"{prefix}.{navigation.Name}";

            if (excluded.Contains(includePath))
                continue;

            query = query.Include(includePath);
            query = IncludeNavigations(query, context, navigation.TargetEntityType.ClrType, includePath, excluded);
        }

        return query;
    }


}