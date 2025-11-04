using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Nextended.Core.Helper;

namespace Nextended.Core.Extensions
{
    public static class ExpressionExtensions
    {
        /// <summary>
        /// http://stackoverflow.com/questions/13705394/how-to-make-a-predicatebuilder-not
        /// </summary>
		public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expr)
        {
            return Expression.Lambda<Func<T, bool>>
                (Expression.Not(Expression.Invoke(expr, expr.Parameters.Cast<Expression>())), expr.Parameters);
        }

        public static IReadOnlyList<MemberInfo> GetMemberInfosPaths<T, TResult>(this Expression<Func<T, TResult>> expr)
        {
			return PropertyPath<T>.Get(expr).ToArray();
		}

        public static MemberExpression? GetMemberExpression<T>(this Expression<Func<T, object>> expression)
        {
            return expression.Body switch
            {
                MemberExpression memberExpression => memberExpression,
                UnaryExpression { Operand: MemberExpression operand } => operand,
                _ => null
            };
        }

        public static MemberExpression? GetMemberExpression(this Expression expression)
        {
            if (expression is LambdaExpression lambda)
            {
                MemberExpression memberExpression;
                if (lambda.Body is UnaryExpression unaryExpression)
                {
                    memberExpression = (MemberExpression)unaryExpression.Operand;
                }
                else memberExpression = (MemberExpression)lambda.Body;

                return memberExpression;
            }
            return null;
        }

        /// <summary>
        /// Gets the member info represented by an expression.
        /// </summary>
        public static MemberInfo? GetMemberInfo(this Expression expression)
        {
            return expression.GetMemberExpression()?.Member;
		}

		/// <summary> 
		/// Helper method to get member name with compile time verification to avoid typo. 
		/// </summary> 
		/// <param name="expr">The lambda expression usually in the form of o => o.member.</param> 
		/// <returns>The name of the property.</returns> 
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Not used in all design time assemblies.")]
		public static string GetMemberName<T>(this Expression<Func<T>> expr)
		{
			var body = expr.Body;
			if (body is MemberExpression || body is UnaryExpression)
			{
				MemberExpression memberExpression = body as MemberExpression ?? (MemberExpression)((UnaryExpression)body).Operand;
				return memberExpression.Member.Name;
			}

			return expr.ToString();
		}


		/// <summary>
		///     Helper method to get member name with compile time verification to avoid typo.
		/// </summary>
		/// <param name="expr">The lambda expression usually in the form of o => o.member.</param>
		/// <returns>The name of the property.</returns>
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Not used in all design time assemblies.")]
		public static string GetMemberName<T, TResult>(this Expression<Func<T, TResult>> expr)
		{
			var body = expr.Body;
			if (body is MemberExpression || body is UnaryExpression)
			{
				MemberExpression memberExpression = body as MemberExpression ?? (MemberExpression)((UnaryExpression)body).Operand;
				return memberExpression.Member.Name;
			}
			//MemberInfo memberInfo = GetMemberInfo(expr);

			return expr.ToString();
		}

		/// <summary>
		/// Gets the property info.
		/// </summary>
		/// <param name="expr">The expr.</param>
		/// <returns></returns>
		public static PropertyInfo GetPropertyInfo(this Expression<Func<object>> expr)
		{
			Expression body = expr.Body;
			MemberExpression memberExpression = body as MemberExpression ?? (MemberExpression)((UnaryExpression)body).Operand;
			return (PropertyInfo)memberExpression.Member;
		}

        public static IDictionary<string, object> ReadParameters(this MethodCallExpression methodCallExpr)
        {
            ParameterInfo[] actionParameters = methodCallExpr.Method.GetParameters();
            MethodInfo dictionaryAdd = ((MethodCallExpression)((Expression<Action<Dictionary<string, object>>>)(d => d.Add(string.Empty, null))).Body).Method;

            var result = new Dictionary<string, object>();
            if (actionParameters.Any())
            {
                var argExpression =
                    Expression.Lambda<Func<Dictionary<string, object>>>(
                        Expression.ListInit(Expression.New(typeof(Dictionary<string, object>)),
                            methodCallExpr.Arguments.Select((a, i) => Expression.ElementInit(dictionaryAdd, Expression.Constant(actionParameters[i].Name), Expression.Convert(a, typeof(object))))));

                var parameterValueGetter = argExpression.Compile();
                result = parameterValueGetter();
            }
            return result;
        }
	}
}