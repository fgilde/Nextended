using System;
using System.Linq;
using System.Linq.Expressions;

namespace Nextended.Core.OData;

public static class ODataExtensions
{
    public static ODataQueryModel ToODataModel<TSource>(this IQueryable<TSource> source)
    {
        ODataExpressionVisitor oDataExpressionVisitor = new ODataExpressionVisitor();
        oDataExpressionVisitor.Visit(source.Expression);
        return new ODataQueryModel()
        {
            Filter = oDataExpressionVisitor.Result,
            Select = oDataExpressionVisitor.Select,
            OrderBy = oDataExpressionVisitor.OrderBy,
            Skip = oDataExpressionVisitor.Skip,
            Take = oDataExpressionVisitor.Take,
        };
    }
    
    public static string ToSelectString<TSource>(this IQueryable<TSource> source)
    {
        ODataExpressionVisitor oDataExpressionVisitor = new ODataExpressionVisitor();
        oDataExpressionVisitor.Visit(source.Expression);
        return oDataExpressionVisitor.Select;
    }
    
    public static string ToFilterString<TSource>(this IQueryable<TSource> source)
    {
        return source.Expression.ToFilterString();
    }

    public static string ToFilterString<TSource>(this Expression<Func<TSource, bool>> expression)
    {
        ODataExpressionVisitor oDataExpressionVisitor = new ODataExpressionVisitor();
        oDataExpressionVisitor.Visit(expression);
        return oDataExpressionVisitor.Result;
    }

    public static string ToFilterString(this Expression expression)
    {
        ODataExpressionVisitor oDataExpressionVisitor = new ODataExpressionVisitor();
        oDataExpressionVisitor.Visit(expression);
        return oDataExpressionVisitor.Result;
    }
}