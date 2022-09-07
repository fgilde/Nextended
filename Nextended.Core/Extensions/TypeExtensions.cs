using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nextended.Core.Helper;
using Nextended.Core.Types;

namespace Nextended.Core.Extensions
{
    public static class TypeExtensions
    {
      

        /// <summary>
        /// Gibt an ob der Typ eine BaseId ist
        /// </summary>
        public static bool IsBaseId(this Type modelType)
        {
            return GetBaseIdBaseType(modelType) != null;
        }

        /// <summary>
        /// Gibt an ob der Typ eine BaseId ist
        /// </summary>
        public static Type GetBaseIdBaseType(this Type modelType)
        {
            Type baseType = modelType.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                if (baseType.IsGenericType
                    && baseType.GetGenericTypeDefinition() == typeof(BaseId<,>))
                    return baseType;
                baseType = baseType.BaseType;
            }

            return null;
        }

        public static PropertyInfo GetPropertyIgnoreCase(this Type type, string propertyName)
        {
            var typeList = new List<Type> { type };

            if (type.IsInterface)
                typeList.AddRange(type.GetInterfaces());

            return typeList
                .Select(interfaceType => interfaceType.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance))
                .FirstOrDefault(property => property != null);
        }

        public static bool IsNullableEnum(this Type t)
            => Nullable.GetUnderlyingType(t) is { IsEnum: true };

        public static bool IsNullableType(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static bool IsNullable(this Type type)
        {
            Check.NotNull(() => type);
            return !type.IsValueType || IsNullableType(type);
        }

        public static bool IsNullableOf<T>(this Type type)
        {
            return IsNullableType(type) && type.GetGenericArguments().FirstOrDefault() == typeof(T);
        }

        public static bool IsString(this Type input)
        {
            return input == typeof(string);
        }

        public static bool IsDecimal(this Type input)
        {
            return input == typeof(decimal);
        }

        public static bool IsInt(this Type input)
        {
            return input == typeof(int);
        }

        public static bool IsDateTime(this Type input)
        {
            return input == typeof(DateTime);
        }

        public static bool IsBool(this Type input)
        {
            return input == typeof(bool);
        }

        public static bool IsNullableDecimal(this Type input)
        {
            return input == typeof(decimal?);
        }

        public static bool IsNullableInt(this Type input)
        {
            return input == typeof(int?);
        }

        public static bool IsNullableDateTime(this Type input)
        {
            return input == typeof(DateTime?);
        }

        public static bool IsNullableBool(this Type input)
        {
            return input == typeof(bool?);
        }

        public static bool IsType(this Type input)
        {
            return input == typeof(Type);
        }

        public static bool IsIEnumerable(this Type input)
        {
            return typeof(IEnumerable).IsAssignableFrom(input);
        }

        public static bool IsEnumerableOrArray(this Type type)
        {
            return typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string) || type.IsArray;
        }

        public static bool IsIList(this Type input)
        {
            return typeof(IList).IsAssignableFrom(input);
        }

        public static bool IsNotString(this Type input)
        {
            return input != typeof(string);
        }

        public static bool IsNotDecimal(this Type input)
        {
            return input != typeof(decimal);
        }

        public static bool IsNotInt(this Type input)
        {
            return input != typeof(int);
        }

        public static bool IsNotDateTime(this Type input)
        {
            return input != typeof(DateTime);
        }

        public static bool IsNotBool(this Type input)
        {
            return input != typeof(bool);
        }

        public static bool IsNotNullableDecimal(this Type input)
        {
            return input != typeof(decimal?);
        }

        public static bool IsNotNullableInt(this Type input)
        {
            return input != typeof(int?);
        }

        public static bool IsNotNullableDateTime(this Type input)
        {
            return input != typeof(DateTime?);
        }

        public static bool IsNotNullableBool(this Type input)
        {
            return input != typeof(bool?);
        }

        public static bool IsNotType(this Type input)
        {
            return input != typeof(Type);
        }

        public static bool IsNotIEnumerable(this Type input)
        {
            return !typeof(IEnumerable).IsAssignableFrom(input);
        }

        public static bool IsNotIList(this Type input)
        {
            return !typeof(IList).IsAssignableFrom(input);
        }

        public static object CreateInstance(this Type input)
        {
            return ReflectionHelper.CreateInstance(input);
        }

        public static T CreateInstance<T>(this Type input)
        {
            return ReflectionHelper.CreateInstance<T>();
        }

        public static T CreateInstance<T>(this Type input, params object[] args)
        {
            return (T)Activator.CreateInstance(input, args);
        }

        public static bool IsSubclassOfInterfaceOf<TInterface>(this Type toCheck) => IsSubclassOfInterfaceOf(toCheck, typeof(TInterface));
        public static bool IsSubclassOfInterfaceOf(this Type toCheck, Type interfaceType)
        {
            var interfaces = toCheck.GetInterfaces();

            foreach (var interfaceOfCheck in interfaces)
            {
                if (interfaceOfCheck.GetTypeInfo().IsGenericType)
                {
                    if (interfaceOfCheck.GetGenericTypeDefinition() == interfaceType)
                        return true;
                    
                }
                else if (interfaceOfCheck == interfaceType)
                    return true;
            }

            return false;
        }
    }
}