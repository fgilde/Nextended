//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;

//namespace Nextended.Core.Extensions;

//public static class EFExtensions
//{
//    public static string GetSqlQueryText<T>(this T sourceContext, Func<T, IQueryable> queryBuilder) where T : DbContext
//    {
//        Type contextType = sourceContext.GetType();

//        Type optionsBuilderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType);
//        var optionsBuilder = (DbContextOptionsBuilder)Activator.CreateInstance(optionsBuilderType)!;
//        optionsBuilder.UseSqlServer("Server=localhost;Database=not_existing_database;User Id=sa;Password=not-existing-password");

//        var context = (T)Activator.CreateInstance(contextType, optionsBuilder.Options)!;


//        return queryBuilder(context)
//            .ToQueryString();
//    }


//    /// <summary>
//    ///     Returns the count of entities that have been changed in the specified DbContext. (Added, Modified, Deleted)
//    /// </summary>
//    /// <param name="context">The DbContext to retrieve the count from.</param>
//    public static int GetChangedEntryCount(this DbContext context)
//    {
//        return context.ChangeTracker.Entries()
//            .Count(e => e.State != EntityState.Unchanged);
//    }

//    public static IIncludableQueryable<TEntity, TProperty2> Include<TEntity, TProperty1, TProperty2>(
//        this IQueryable<TEntity> queryable,
//        Expression<Func<TEntity, IEnumerable<TProperty1>>> include1,
//        Expression<Func<TProperty1, TProperty2>> include2
//    ) where TEntity : class =>
//        queryable.Include(include1)
//                 .ThenInclude(include2);

//    public static IIncludableQueryable<TEntity, TProperty3> Include<TEntity, TProperty1, TProperty2, TProperty3>(
//        this IQueryable<TEntity> queryable,
//        Expression<Func<TEntity, IEnumerable<TProperty1>>> include1,
//        Expression<Func<TProperty1, TProperty2>> include2,
//        Expression<Func<TProperty2, TProperty3>> include3
//    ) where TEntity : class =>
//        queryable.Include(include1)
//                 .ThenInclude(include2)
//                 .ThenInclude(include3);

//    public static IQueryable<TEntity> MultiInclude<TEntity, TProperty1>(
//        this IQueryable<TEntity> queryable,
//        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, IEnumerable<TProperty1>>> baseInclude,
//        params Expression<Func<TProperty1, object?>>[] includes
//    ) where TEntity : class
//    {
//        return includes.Aggregate(
//            queryable,
//            (current, include) => baseInclude(current)
//               .ThenInclude(include)
//        );
//    }

//    public static IQueryable<TEntity> MultiInclude<TEntity, TProperty1>(
//        this IQueryable<TEntity> queryable,
//        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, TProperty1>> baseInclude,
//        params Expression<Func<TProperty1, object?>>[] includes
//    ) where TEntity : class
//    {
//        return includes.Aggregate(
//            queryable,
//            (current, include) => baseInclude(current)
//               .ThenInclude(include)
//        );
//    }
//}
