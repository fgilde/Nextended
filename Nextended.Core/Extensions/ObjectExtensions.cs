using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nextended.Core.DeepClone;
using Nextended.Core.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Nextended.Core.Extensions
{
    public static class ObjectExtensions
    {

        /// <summary>
        /// Used to simplify and beautify casting an object to a type.
        /// </summary>
        /// <typeparam name="T">Type to be casted</typeparam>
        /// <param name="obj">Object to cast</param>
        /// <returns>Casted object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T As<T>(this object obj) where T : class => (T)obj;



        /// <summary>Check if an item is in a list.</summary>
        /// <param name="item">Item to check</param>
        /// <param name="list">List of items</param>
        /// <typeparam name="T">Type of the items</typeparam>
        public static bool IsIn<T>(this T item, params T[] list)
        {
            return (new HashSet<T>(list)).Contains<T>(item);
        }

        /// <summary>Check if an item is in the given enumerable.</summary>
        /// <param name="item">Item to check</param>
        /// <param name="items">Items</param>
        /// <typeparam name="T">Type of the items</typeparam>
        public static bool IsIn<T>(this T item, IEnumerable<T> items) => items.Contains<T>(item);

        /// <summary>
        /// Can be used to conditionally perform a function
        /// on an object and return the modified or the original object.
        /// It is useful for chained calls.
        /// </summary>
        /// <param name="obj">An object</param>
        /// <param name="condition">A condition</param>
        /// <param name="func">A function that is executed only if the condition is <code>true</code></param>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <returns>
        /// Returns the modified object (by the <paramref name="func" /> if the <paramref name="condition" /> is <code>true</code>)
        /// or the original object if the <paramref name="condition" /> is <code>false</code>
        /// </returns>
        public static T If<T>(this T obj, bool condition, Func<T, T> func) => condition ? func(obj) : obj;

        public static T If<T>(this T obj, Func<bool> condition, Func<T, T> func) => If(obj, condition(), func);

        /// <summary>
        /// Can be used to conditionally perform an action
        /// on an object and return the original object.
        /// It is useful for chained calls on the object.
        /// </summary>
        /// <param name="obj">An object</param>
        /// <param name="condition">A condition</param>
        /// <param name="action">An action that is executed only if the condition is <code>true</code></param>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <returns>Returns the original object.</returns>
        public static T If<T>(this T obj, bool condition, Action<T> action)
        {
            if (condition)
                action(obj);
            return obj;
        }

        public static T If<T>(this T obj, Func<bool> condition, Action<T> action) => If(obj, condition(), action);



        public static T[] AllOf<T>(this object instance, ReflectReadSettings? settings = null)
        {
            return ReflectionHelper.FindAllValuesOf<T>(instance, settings);
        }

        public static IDictionary<string, string> ToFlatDictionary(this object obj, string separator = ".")
        {
            return JsonDictionaryConverter.Flatten(obj, separator);
        }

        public static IDictionary<string, object> ToDictionary(this object obj)
        {
            return JObject.FromObject(obj).ToDictionary();
        }

        public static Lazy<T> AsLazy<T>(this T t) where T : class
        {
            return new Lazy<T>(() => t, LazyThreadSafetyMode.PublicationOnly);
        }

        public static string ToUrlQueryString(this object obj, string firstDelimiter = "")
        {
            var properties = from p in obj.GetType().GetProperties()
                let value = GetValue(obj, p)
                where value != null
                select $"{GetKeyName(p)}={System.Web.HttpUtility.UrlEncode(value.ToString())}";

            return $"{firstDelimiter}{string.Join("&", properties)}";
        }

        public static string ToNullSafeString(this object input, string defaultIfNull = "")
        {
            return input == null ? defaultIfNull : input.ToString();
        }

        public static T Clone<T>(this T source, ClonerSettings settings) where T : class => source.CloneDeep(settings);
        public static T Clone<T>(this T source, FieldType fieldType) where T : class => source.CloneDeep(fieldType);
        public static T Clone<T>(this T source, bool useFastDeepClone = true) where T : class, new()
        {
            return source switch
            {
                null => null,
                ValueType _ => source,
                ICloneable cloneable => (T) cloneable.Clone(),
                _ => useFastDeepClone ? source.CloneDeep() : source.MapTo<T>()
            };
        }

        public static IEnumerable<PropertyInfo> GetProperties(this object input, BindingFlags bindingAttr = default)
        {
            return input.GetType().GetProperties(bindingAttr);
        }

        public static bool NotNull(this object? input)
        {
            return !input.IsNull();
        }

        public static bool IsNull(this object? input)
        {
            return input == null;
        }

        public static Task<T> ToTask<T>(this T input)
        {
            return Task.FromResult(input);
        }
        public static T SetProperties<T>(this T instance, params Action<T>[] actions)
        {
            foreach (var action in actions)
                action(instance);
            return instance;
        }

        public static T ExposeField<T>(this object instance, string fieldName)
        {
            return Check.TryCatch<T, Exception>(() =>
            {
                BindingFlags flags = BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy;
                PropertyInfo info = instance.GetType().GetProperty(fieldName, flags);
                T res;
                if (info != null)
                    res = (T)info.GetValue(instance);
                else
                    res = (T)instance.GetType().GetField(fieldName, flags)?.GetValue(instance);
                if (res == null)
                {
                    var fieldInfo = instance.GetType()
                        .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    res = fieldInfo?.GetValue(instance) is T ? (T) fieldInfo?.GetValue(instance) : default;
                }
                return res;
            });
        }

        private static string GetKeyName(MemberInfo p)
        {
            return (p.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? p.Name).ToLower();
        }

        private static object GetValue(object obj, PropertyInfo p)
        {
            var res = p.GetValue(obj, null);
            if (res == null || p.PropertyType.IsValueType || p.PropertyType.IsAssignableFrom(typeof(string)))
            {
                return res;
            }
            return JsonConvert.SerializeObject(res);
        }
    }
}