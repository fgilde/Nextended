using System;
using System.Linq;
using System.Linq.Expressions;

namespace Nextended.EF;


public static class QueryableExtensions
{
    public static IQueryable<T> WhereContains<T>(
           this IQueryable<T> source,
           string search,
           params Expression<Func<T, string?>>[] propertySelectors)
    {
        if (string.IsNullOrWhiteSpace(search))
            return source;

        var searchLower = search.ToLower();

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? orExpression = (from selector in propertySelectors
                                    select ReplaceParameter(selector.Body, selector.Parameters[0], parameter) into body
                                    let notNull = Expression.NotEqual(body, Expression.Constant(null, typeof(string)))
                                    let toLower = Expression.Call(body!, nameof(string.ToLower), Type.EmptyTypes)
                                    let contains = Expression.Call(toLower, nameof(string.Contains), Type.EmptyTypes, Expression.Constant(searchLower))
                                    select Expression.AndAlso(notNull, contains)).Aggregate<BinaryExpression?, Expression?>(
            null,
            (current, condition) => current == null
                                        ? condition
                                        : Expression.OrElse(current, condition)
        );

        if (orExpression == null)
            return source;

        var lambda = Expression.Lambda<Func<T, bool>>(orExpression, parameter);
        return source.Where(lambda);
    }

    private static Expression ReplaceParameter(
        Expression body,
        ParameterExpression oldParameter,
        ParameterExpression newParameter)
    {
        return new ParameterReplacer(oldParameter, newParameter).Visit(body)!;
    }

    private class ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == oldParameter ? newParameter : base.VisitParameter(node);
        }
    }
}