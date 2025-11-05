using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Nextended.Core.Contracts;
using Nextended.Core.Extensions;

namespace Nextended.EF;
  

public static class IncludeDetailsExtensions
{
    public static IQueryable<TNav> IncludeDetails<TNav>(
        this IQueryable<TNav> query, IIncludePathDefinition def)
        where TNav : class
    {
        foreach (var p in def.GetPaths()) query = query.Include(p);
        return query;
    }

    public static IQueryable<TParent> IncludeDetails<TParent, TNav>(
        this IQueryable<TParent> query,
        Expression<Func<TParent, TNav?>> navigation,
        IIncludePathDefinition def)
        where TParent : class where TNav : class
    {
        var prefix = navigation.GetPropertyPath();
        foreach (var p in def.GetPaths()) query = query.Include($"{prefix}.{p}");
        return query;
    }

    public static IQueryable<TParent> IncludeDetails<TParent, TNav>(
        this IQueryable<TParent> query,
        Expression<Func<TParent, IEnumerable<TNav>>> navigation,
        IIncludePathDefinition def)
        where TParent : class where TNav : class
    {
        var prefix = navigation.GetPropertyPath();
        foreach (var p in def.GetPaths()) query = query.Include($"{prefix}.{p}");
        return query;
    }

}