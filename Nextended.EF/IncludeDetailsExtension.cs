using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Nextended.Core.Contracts;
using Nextended.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Nextended.EF;
  

public static class IncludeDetailsExtensions
{
    public static IEnumerable<string> DistinctPaths(this IIncludePathDefinition def) 
        => def.GetPaths().Distinct();

    public static IIncludableQueryable<TParent, IEnumerable<TNav>> ThenIncludeDetails<TParent, TNav>(
        this IIncludableQueryable<TParent, IEnumerable<TNav>> source,
        Expression<Func<TParent, IEnumerable<TNav>>> navigation,
        IIncludePathDefinition def)
        where TParent : class
        where TNav : class
    {
        var prefix = navigation.GetPropertyPath();
        IQueryable<TParent> q = source;
        foreach (var p in def.DistinctPaths())
            q = q.Include($"{prefix}.{p}");
        return (IIncludableQueryable<TParent, IEnumerable<TNav>>)q;
    }

    public static IIncludableQueryable<TParent, TNav?> ThenIncludeDetails<TParent, TNav>(
        this IIncludableQueryable<TParent, TNav?> source,
        Expression<Func<TParent, TNav?>> navigation,
        IIncludePathDefinition def)
        where TParent : class
        where TNav : class
    {
        var prefix = navigation.GetPropertyPath();
        IQueryable<TParent> q = source;
        foreach (var p in def.DistinctPaths())
            q = q.Include($"{prefix}.{p}");
        return (IIncludableQueryable<TParent, TNav?>)q;
    }

    public static IIncludableQueryable<TParent, TNav?> ThenIncludeDetails<TParent, TNav>(
        this IIncludableQueryable<TParent, TNav?> source,
        string prefix,
        IIncludePathDefinition def)
        where TParent : class
        where TNav : class
    {
        IQueryable<TParent> q = source;
        foreach (var p in def.DistinctPaths())
            q = q.Include($"{prefix}.{p}");
        return (IIncludableQueryable<TParent, TNav?>)q;
    }

    public static IQueryable<TNav> IncludeDetails<TNav>(
        this IQueryable<TNav> query, IIncludePathDefinition def)
        where TNav : class
    {
        foreach (var p in def.DistinctPaths()) query = query.Include(p);
        return query;
    }

    public static IQueryable<TParent> IncludeDetails<TParent, TNav>(
        this IQueryable<TParent> query,
        Expression<Func<TParent, TNav?>> navigation,
        IIncludePathDefinition def)
        where TParent : class where TNav : class
    {
        var prefix = navigation.GetPropertyPath();
        foreach (var p in def.DistinctPaths()) query = query.Include($"{prefix}.{p}");
        return query;
    }

    public static IQueryable<TParent> IncludeDetails<TParent, TNav>(
        this IQueryable<TParent> query,
        Expression<Func<TParent, IEnumerable<TNav>>> navigation,
        IIncludePathDefinition def)
        where TParent : class where TNav : class
    {
        var prefix = navigation.GetPropertyPath();
        foreach (var p in def.DistinctPaths()) query = query.Include($"{prefix}.{p}");
        return query;
    }

}