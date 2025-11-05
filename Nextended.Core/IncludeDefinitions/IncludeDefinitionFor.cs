using Nextended.Core.Extensions;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Nextended.Core.IncludeDefinitions;

public class IncludeDefinitionFor<TEntity>: IncludePathDefinitionBase<IncludeDefinitionFor<TEntity>>
{
    public IncludeDefinitionFor<TEntity> Include(Expression<Func<TEntity, object?>> expression)
        => base.Include(expression.GetPropertyPath());

    public IncludeDefinitionFor<TEntity> Include(params Expression<Func<TEntity, object?>>[] expressions)
        => Include(expressions.Select(e => e.GetPropertyPath()).ToArray());

    public IncludeDefinitionFor<TEntity> IncludeAllWhere(
        Func<PropertyInfo, bool> condition,
        int maxDepth = 6,
        bool includeCollections = true) =>
        IncludeAllWhere<TEntity>(condition, maxDepth, includeCollections);

    public IncludeDefinitionFor<TEntity> IncludeAllVirtual(Func<PropertyInfo, bool>? condition = null, int maxDepth = 6, bool includeCollections = true)
        => IncludeAllVirtual<TEntity>(condition, maxDepth, includeCollections);
}
