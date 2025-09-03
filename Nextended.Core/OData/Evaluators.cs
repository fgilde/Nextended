using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Nextended.Core.OData;

internal static class Evaluators
{
    public static string EvaluateContains(MethodCallExpression node)
    {
        var listExpr = node.Object;
        if (listExpr == null)
        {
            throw new ArgumentNullException(nameof(listExpr));
        }

        var itemExpr = node.Arguments[0];
        var s = new StringBuilder($"search.in(");

        if (itemExpr is MemberExpression memberExpression)
        {
            s.Append($"{memberExpression.Member.Name},");
        }

        ICollection<string> list = null;

        try
        {
            if (listExpr is ConstantExpression constantExpression)
            {
                list = constantExpression.Value as ICollection<string>;
            }
            else if (listExpr is MemberExpression memberExpr)
            {
                if (memberExpr.Expression is ConstantExpression memberConstExpr)
                {
                    var container = memberConstExpr.Value;
                    var field = memberExpr.Member;

                    if (field is System.Reflection.FieldInfo fieldInfo)
                    {
                        list = fieldInfo.GetValue(container) as ICollection<string>;
                    }
                    else if (field is System.Reflection.PropertyInfo propertyInfo)
                    {
                        list = propertyInfo.GetValue(container) as ICollection<string>;
                    }
                }
            }

            if (list == null)
            {
                var lambda = Expression.Lambda(listExpr);
                var compiled = lambda.Compile();
                var result = compiled.DynamicInvoke();
                list = result as ICollection<string>;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot evaluate list expression: {ex.Message}", ex);
        }

        if (list == null)
        {
            throw new ArgumentNullException(nameof(list), "List cannot be null");
        }

        s.Append('\'').Append(string.Join(",", list)).Append('\'');
        s.Append(')');
        return s.ToString();
    }
}