using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Nextended.ResponseFilters.Reflection;

/// <summary>
/// Resolves a <see cref="Expression{TDelegate}"/> like <c>x =&gt; x.Foo.Bar</c> to the leaf <see cref="PropertyInfo"/>.
/// Only single-member paths are supported on the immediate parameter; nested paths are not (use ForEach/sub-filters for that).
/// </summary>
internal static class PropertySelector
{
    public static PropertyInfo Resolve<T, TProp>(Expression<Func<T, TProp>> selector)
    {
        if (selector is null) throw new ArgumentNullException(nameof(selector));

        var body = selector.Body;

        // Strip Convert (e.g. for nullable->object conversions in selectors)
        if (body is UnaryExpression { NodeType: ExpressionType.Convert } unary)
        {
            body = unary.Operand;
        }

        if (body is not MemberExpression member)
        {
            throw new ArgumentException(
                $"Selector must be a simple property access like 'x => x.Property'. Got: {selector}",
                nameof(selector));
        }

        if (member.Expression is not ParameterExpression)
        {
            throw new ArgumentException(
                $"Selector must reference the lambda parameter directly (no nested paths). Got: {selector}. Use ForEach(...) or a sub-filter for nested types.",
                nameof(selector));
        }

        if (member.Member is not PropertyInfo prop)
        {
            throw new ArgumentException(
                $"Selector must reference a property, not a field/method. Got: {selector}",
                nameof(selector));
        }

        return prop;
    }
}
