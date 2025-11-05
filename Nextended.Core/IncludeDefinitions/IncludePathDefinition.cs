using Nextended.Core.Extensions;
using Nextended.Core.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Nextended.Core.Contracts;

namespace Nextended.Core.IncludeDefinitions;

public abstract class IncludePathDefinitionBase<TIncludeDefinition> : IIncludePathDefinition
    where TIncludeDefinition : IncludePathDefinitionBase<TIncludeDefinition>
{
    private readonly HashSet<string> _paths = new(StringComparer.Ordinal);
    private bool _configured; 

    protected virtual void Configure() { }

    public TIncludeDefinition Include<T>(Expression<Func<T, object?>> expression)
        => Include(expression.GetPropertyPath());

    public TIncludeDefinition Include<T>(params Expression<Func<T, object?>>[] expressions)
        => Include(expressions.Select(e => e.GetPropertyPath()).ToArray());

    public TIncludeDefinition Include(string propertyPath)
    {
        _paths.Add(propertyPath);
        return (TIncludeDefinition)this;
    }

    public TIncludeDefinition Include(params string[] paths)
    {
        foreach (var p in paths) _paths.Add(p);
        return (TIncludeDefinition)this;
    }

    public TIncludeDefinition Include(IIncludePathDefinition other)
        => Include(other.GetPaths().ToArray());

    public TIncludeDefinition Include(params IIncludePathDefinition[] other)
        => Include(other.SelectMany(o => o.GetPaths()).Distinct(StringComparer.Ordinal).ToArray());

    
    public TIncludeDefinition IncludeWithPrefix(string prefix, IIncludePathDefinition other)
        => Include(other.GetPaths().Select(p => $"{prefix}.{p}").ToArray());

    public TIncludeDefinition IncludeWithPrefix<TEntity, TChild>(
        Expression<Func<TEntity, TChild?>> navigation,
        IIncludePathDefinition other)
        where TEntity : class
        where TChild : class
        => IncludeWithPrefix(navigation.GetPropertyPath(), other);

    public TIncludeDefinition IncludeWithPrefix<TEntity, TChild>(
        Expression<Func<TEntity, IEnumerable<TChild>>> navigation,
        IIncludePathDefinition other)
        where TEntity : class
        where TChild : class
        => IncludeWithPrefix(navigation.GetPropertyPath(), other);

    
    public TIncludeDefinition IncludeAllWhere(Type type,
        Func<PropertyInfo, bool> condition,
        int maxDepth = 6,
        bool includeCollections = true)
    {
        foreach (var p in PathScanner.GetPaths(type, condition, maxDepth, includeCollections))
            _paths.Add(p);
        return (TIncludeDefinition)this;
    }

    public TIncludeDefinition IncludeAllWhere<T>(
        Func<PropertyInfo, bool> condition,
        int maxDepth = 6,
        bool includeCollections = true) =>
        IncludeAllWhere(typeof(T), condition, maxDepth, includeCollections);


    public TIncludeDefinition IncludeAllVirtual<T>(Func<PropertyInfo, bool>? condition = null, int maxDepth = 6, bool includeCollections = true)
        => IncludeAllVirtual(typeof(T), condition, maxDepth, includeCollections);

    public TIncludeDefinition IncludeAllVirtual(Type type, Func<PropertyInfo, bool>? condition = null, int maxDepth = 6, bool includeCollections = true)
        => IncludeAllWhere(type,
            pi => pi.GetGetMethod(nonPublic: true)?.IsVirtual == true && (condition?.Invoke(pi) ?? true),
            maxDepth,
            includeCollections);

    public IncludePathDefinition Exclude(IIncludePathDefinition other)
    {
        var remove = new HashSet<string>(other.GetPaths(), StringComparer.Ordinal);
        var keep = GetPaths().Where(p => !remove.Contains(p)).ToArray();
        return new IncludePathDefinition().Include(keep);
    }


    public virtual IEnumerable<string> GetPaths()
    {
        if (!_configured)
        {
            Configure();
            _configured = true;
        }
        return _paths.OrderBy(p => p, StringComparer.Ordinal);
    }
}

public class IncludePathDefinition : IncludePathDefinitionBase<IncludePathDefinition> { }
