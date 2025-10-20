using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Nextended.Core.OData;

internal sealed class ODataExpressionVisitor : ExpressionVisitor
{
    private readonly StringBuilder _filterBuilder = new();
    private readonly StringBuilder _selectBuilder = new();
    private readonly StringBuilder _orderByBuilder = new();

    private readonly List<string> _arguments = new();

    public string Result => _filterBuilder.ToString();
    public string Select => _selectBuilder.ToString();
    public string OrderBy => _orderByBuilder.ToString();

    private int _take;
    public string Take => _take <= 0 ? "" : _take.ToString();

    private int _skip;
    public string Skip => _skip <= 0 ? "" : _skip.ToString();

    private string Arguments => string.Join(",", _arguments);

    private static string GetMemberName(Expression node)
    {
        return node switch
        {
            MemberExpression memberExpression => memberExpression.Member.Name,
            ConstantExpression constantExpression when constantExpression.Type == typeof(string) =>
                $"'{constantExpression.Value?.ToString() ?? ""}'",
            ConstantExpression constantExpression => $"{constantExpression.Value?.ToString() ?? ""}",
            _ => ""
        };
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.Convert:
                if (node.Operand is MemberExpression me &&
                    (me.Type == typeof(bool) || Nullable.GetUnderlyingType(me.Type) == typeof(bool)))
                {
                    _filterBuilder.Append(GetMemberName(me)).Append(" eq true");
                    return node;
                }
                Visit(node.Operand);
                return node;

            case ExpressionType.Not:
                _filterBuilder.Append("not ");
                Visit(node.Operand);
                return node;

            default:
                return base.VisitUnary(node);
        }
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.AndAlso:
                var leftAndExpressionVisitor = new ODataExpressionVisitor();
                var rightAndExpressionVisitor = new ODataExpressionVisitor();
                leftAndExpressionVisitor.Visit(node.Left);
                rightAndExpressionVisitor.Visit(node.Right);

                _filterBuilder.Append('(').Append(leftAndExpressionVisitor.Result).Append(" AND ")
                    .Append(rightAndExpressionVisitor.Result).Append(')');
                break;

            case ExpressionType.OrElse:
                var leftOrExpressionVisitor = new ODataExpressionVisitor();
                var rightOrExpressionVisitor = new ODataExpressionVisitor();
                leftOrExpressionVisitor.Visit(node.Left);
                rightOrExpressionVisitor.Visit(node.Right);
                _filterBuilder.Append('(').Append(leftOrExpressionVisitor.Result).Append(" OR ")
                    .Append(rightOrExpressionVisitor.Result).Append(')');
                break;

            default:
                if (ODataOperators.OperatorDictionary.TryGetValue(node.NodeType, out var op))
                    _filterBuilder.Append(GetMemberName(node.Left)).Append(' ').Append(op).Append(' ').Append(GetMemberName(node.Right));
                break;
        }

        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        _arguments.Add(node.Member.Name);

        return base.VisitMember(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var baseQuery = node.Arguments[0];
        switch (node.Method.Name)
        {
            case nameof(Queryable.Where):
                if (node.Arguments.Count >= 2)
                {
                    Visit(baseQuery); 
                    Visit(node.Arguments[1]);
                }
                break;

            case nameof(Enumerable.Contains):
                _filterBuilder.Append(Evaluators.EvaluateContains(node));
                break;

            case nameof(string.StartsWith):
            case nameof(string.EndsWith):
                if (node.Object == null)
                {
                    throw new ArgumentNullException(nameof(node));
                }
                var argValue = GetArgumentValue(baseQuery);
                _filterBuilder.Append($"{node.Method.Name.ToLowerInvariant()}({GetMemberName(node.Object)}, '{argValue}')");
                break;

            case nameof(Enumerable.Skip):
                Visit(baseQuery);
                if (node.Arguments.Count > 1 && node.Arguments[1] is ConstantExpression skipConst && skipConst.Type == typeof(int))
                {
                    _skip = (int)skipConst.Value;
                }
                break;

            case nameof(Enumerable.Take):
                Visit(baseQuery); 
                if (node.Arguments.Count > 1 && node.Arguments[1] is ConstantExpression takeConst && takeConst.Type == typeof(int))
                {
                    _take = (int)takeConst.Value;
                }
                break;

            case nameof(Queryable.Select):
                Visit(baseQuery); 
                if (node.Arguments.Count > 1)
                {
                    var selectVisitor = new ODataExpressionVisitor();
                    selectVisitor.Visit(node.Arguments[1]);
                    _selectBuilder.Append(selectVisitor.Arguments);
                }
                break;

            case nameof(Queryable.OrderBy):
                Visit(baseQuery); 
                if (node.Arguments.Count > 1)
                {
                    var orderVisitor = new ODataExpressionVisitor();
                    orderVisitor.Visit(node.Arguments[1]);
                    _orderByBuilder.Append(orderVisitor.Arguments);
                }
                break;

            case nameof(Queryable.OrderByDescending):
                Visit(baseQuery);
                if (node.Arguments.Count > 1)
                {
                    var orderVisitor = new ODataExpressionVisitor();
                    orderVisitor.Visit(node.Arguments[1]);
                    _orderByBuilder.Append(orderVisitor.Arguments).Append(" desc");
                }
                break;

            default:
                return base.VisitMethodCall(node);
        }

        return node;
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        Visit(node.Body);
        return node;
    }

    private static string GetArgumentValue(Expression argument)
    {
        if (argument is ConstantExpression constantExpression)
        {
            return constantExpression.Value?.ToString() ?? "";
        }

        if (argument is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        
        try
        {
            var lambda = Expression.Lambda(argument);
            var compiled = lambda.Compile();
            var result = compiled.DynamicInvoke();
            return result?.ToString() ?? "";
        }
        catch
        {
            return "";
        }
    }
}
