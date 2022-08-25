using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Nextended.Core.Helper;

public static class PropertyPath<TSource>
{
    public static IReadOnlyList<MemberInfo> Get<TResult>(Expression<Func<TSource, TResult>> expression)
    {
        var visitor = new PropertyVisitor();
        visitor.Visit(expression.Body);
        visitor.Path.Reverse();
        return visitor.Path;
    }

    private class PropertyVisitor : ExpressionVisitor
    {
        internal readonly List<MemberInfo> Path = new List<MemberInfo>();

        protected override Expression VisitMember(MemberExpression node)
        {
            if (!(node.Member is PropertyInfo) && !(node.Member is FieldInfo))
            {
                throw new ArgumentException("The path can only contain properties or fields", nameof(node));
            }

            Path.Add(node.Member);
            return base.VisitMember(node);
        }
    }
}