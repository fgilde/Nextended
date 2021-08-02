using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Nextended.Core.Extensions
{
    public static class MemberInfoExtensions
    {
        public static IEnumerable<T> GetAttributes<T>(this MemberInfo member, bool inherit)
        {
            return Attribute.GetCustomAttributes(member, inherit).OfType<T>();
        }

        public static TResult ReadFromAttribute<TResult, TAttribute>(this MemberInfo info, Func<TAttribute, TResult> readerFunc,
            TResult fallbackValue = default(TResult)) where TAttribute : Attribute
        {
            var attribute = info.GetCustomAttribute<TAttribute>();
            return attribute != null ? readerFunc(attribute) : fallbackValue;
        }

        public static bool HasAttribute<TAttribute>(this MethodInfo method) where TAttribute : Attribute
        {
            return method.GetCustomAttributes(typeof(TAttribute), false).Any();
        }

        //http://stackoverflow.com/questions/36032555/compare-propertyinfo-name-to-an-existing-property-in-a-safe-way
        public static bool IsEqual<T>(this PropertyInfo prop, Expression<Func<T, object>> propertyExpression)
        {
            var memberExpression = propertyExpression.Body as MemberExpression;

            if (memberExpression == null)
            {
                //This will handle Nullable<T> properties.

                if (propertyExpression.Body is UnaryExpression unaryExpression)
                {
                    memberExpression = unaryExpression.Operand as MemberExpression;
                }

                if (memberExpression == null)
                {
                    throw new ArgumentException("Expression is not a MemberExpression", "propertyExpression");
                }
            }
            return memberExpression.Member.Name == prop.Name;
        }
    }
}