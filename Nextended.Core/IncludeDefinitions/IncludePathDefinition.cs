using System;
using Nextended.Core.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;

namespace Nextended.Core.IncludeDefinitions;

public abstract class IncludePathDefinitionBase<TIncludeDefinition> : IIncludePathDefinition
    where TIncludeDefinition : IncludePathDefinitionBase<TIncludeDefinition>
{
    private HashSet<string> _paths = new();

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
        paths.ToList().ForEach(p => _paths.Add(p));
        return (TIncludeDefinition)this;
    }

    public TIncludeDefinition IncludeWithPrefix(string prefix, IIncludePathDefinition other)
        => Include(other.GetPaths().Select(p => $"{prefix}.{p}").ToArray());

    public TIncludeDefinition Include(IIncludePathDefinition other)
        => Include(other.GetPaths().Select(p => $"{p}").ToArray());

    public TIncludeDefinition Include(params IIncludePathDefinition[] other)
        => Include(other.SelectMany(o => o.GetPaths()).Distinct(StringComparer.Ordinal).ToArray());

    public TIncludeDefinition IncludeAllWhere(Type type,
        Func<PropertyInfo, bool> condition,
        int maxDepth = 6,
        bool includeCollections = true)
    {
        foreach (var p in PathScanner.GetPaths(type, condition, maxDepth, includeCollections))
            Include(p);
        return (TIncludeDefinition)this;
    }

    public TIncludeDefinition IncludeAllWhere<T>(
        Func<PropertyInfo, bool> condition,
        int maxDepth = 6,
        bool includeCollections = true) =>
        IncludeAllWhere(typeof(T), condition, maxDepth, includeCollections);

    public TIncludeDefinition IncludeAllVirtual<T>(int maxDepth = 6, bool includeCollections = true)
        => IncludeAllVirtual(typeof(T), maxDepth, includeCollections);

    public TIncludeDefinition IncludeAllVirtual(Type type, int maxDepth = 6, bool includeCollections = true)
        => IncludeAllWhere(type,
            pi => pi.GetGetMethod(nonPublic: true)?.IsVirtual == true,
            maxDepth,
            includeCollections);

    public virtual IEnumerable<string> GetPaths()
    {
        Configure();
        return _paths;
    }
}

public class IncludePathDefinition : IncludePathDefinitionBase<IncludePathDefinition>
{
}