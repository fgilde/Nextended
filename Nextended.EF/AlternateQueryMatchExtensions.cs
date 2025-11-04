using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Nextended.Core.Extensions;

namespace Nextended.EF
{
    public static class AlternateQueryMatchExtensions
    {
        public static IQueryable<T> WhereKeyMatches<T>(
            this IQueryable<T> query,
            string[]? propertyNames,
            string key) where T : class
        {
            if (propertyNames == null || propertyNames.Length == 0 || string.IsNullOrEmpty(key))
                return query.Where(_ => false);

            var parameter = Expression.Parameter(typeof(T), "e");
            Expression orBody = Expression.Constant(false);

            foreach (var propertyName in propertyNames.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var prop = typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (prop == null) continue;

                var expr = EqualsExpression(parameter, prop, key);
                if (expr != null)
                    orBody = Expression.OrElse(orBody, expr);
            }

            var lambda = Expression.Lambda<Func<T, bool>>(orBody, parameter);
            return query.Where(lambda);
        }
        
        public static IQueryable<T> WhereKeyMatches<T>(
            this IQueryable<T> query,
            DbContext? context,
            string key) where T : class
        {
            if (context == null)
                return query.WhereIdFallback(key);

            var entityType = context.Model.FindEntityType(typeof(T));
            return entityType != null
                ? query.WhereKeyMatches(entityType, key)
                : query.WhereIdFallback(key);
        }

        public static IQueryable<T> WhereKeyMatches<T>(
            this IQueryable<T> query,
            IEntityType? entityType,
            string key) where T : class
        {
            if (entityType == null)
                return query.WhereIdFallback(key);

            var names = CollectKeyPropertyNames(entityType);
            return names.Count == 0
                ? query.WhereIdFallback(key)
                : query.WhereKeyMatches(names.ToArray(), key);
        }

        public static IQueryable<T> WhereKeyMatches<T>(
            this IQueryable<T> query,
            IKey[]? keys,
            string key) where T : class
        {
            if (keys == null || keys.Length == 0)
                return query.WhereIdFallback(key);

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var k in keys)
            {
                if (k.Properties.Count == 1)
                {
                    var p = k.Properties[0];
                    if (IsSupportedType(p.ClrType))
                        names.Add(p.Name);
                }
            }

            return names.Count == 0
                ? query.WhereIdFallback(key)
                : query.WhereKeyMatches(names.ToArray(), key);
        }

        public static IQueryable<T> WhereKeyMatches<T>(
            this IQueryable<T> query,
            Expression<Func<T, object>>[]? propertySelectors,
            string key) where T : class
        {
            if (propertySelectors == null || propertySelectors.Length == 0)
                return query.Where(_ => false);

            var names = propertySelectors
                .Select(e => e.GetMemberExpression())
                .Where(m => m != null)
                .Select(m => m!.Member.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return query.WhereKeyMatches(names, key);
        }



        private static Expression? EqualsExpression(ParameterExpression parameter, PropertyInfo prop, string key)
        {
            var propAccess = Expression.Property(parameter, prop);
            var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            if (propType == typeof(string))
                return Expression.Equal(propAccess, Expression.Constant(key, prop.PropertyType));

            if (propType == typeof(Guid))
            {
                if (Guid.TryParse(key, out var g))
                    return Expression.Equal(propAccess, Expression.Constant(g, prop.PropertyType));
                return null;
            }

            if (propType == typeof(int))
            {
                if (int.TryParse(key, out var i))
                    return Expression.Equal(propAccess, Expression.Constant(i, prop.PropertyType));
                return null;
            }

            return null;
        }

        private static List<string> CollectKeyPropertyNames(IEntityType entityType)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var pk = entityType.FindPrimaryKey();
            if (pk != null && pk.Properties.Count == 1)
            {
                var p = pk.Properties[0];
                if (IsSupportedType(p.ClrType))
                    names.Add(p.Name);
            }

            foreach (var ak in entityType.GetKeys())
            {
                if (ak.IsPrimaryKey()) continue;
                if (ak.Properties.Count != 1) continue;

                var p = ak.Properties[0];
                if (IsSupportedType(p.ClrType))
                    names.Add(p.Name);
            }

            return names.ToList();
        }

        private static bool IsSupportedType(Type type)
        {
            var t = Nullable.GetUnderlyingType(type) ?? type;
            return t == typeof(string) || t == typeof(Guid) || t == typeof(int);
        }

        private static IQueryable<T> WhereIdFallback<T>(this IQueryable<T> query, string key) where T : class
        {
            var candidateNames = new List<string> { "Id", typeof(T).Name + "Id" };
            return query.WhereKeyMatches(candidateNames.ToArray(), key);
        }
    }
}
