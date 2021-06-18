﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nextended.Core.Helper;

namespace Nextended.Core.Extensions
{
    public static class ObjectExtensions
    {
        public static IDictionary<string, string> ToFlatDictionary(this object obj)
        {
            return JsonDictionaryConverter.Flatten(obj);
        }

        public static IDictionary<string, object> ToDictionary(this object obj)
        {
            return JObject.FromObject(obj).ToDictionary();
        }

        public static Lazy<T> AsLazy<T>(this T t) where T : class
        {
            return new Lazy<T>(() => t, LazyThreadSafetyMode.PublicationOnly);
        }

        public static string ToQueryString(this object obj, string firstDelimiter = "")
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

        public static T Clone<T>(this T source) where T : class, new()
        {
            return source switch
            {
                null => null,
                ValueType _ => source,
                ICloneable cloneable => (T) cloneable.Clone(),
                _ => source.MapTo<T>()
            };
        }

        public static IEnumerable<PropertyInfo> GetProperties(this object input, BindingFlags bindingAttr = default)
        {
            return input.GetType().GetProperties(bindingAttr);
        }

        public static bool NotNull(this object input)
        {
            return !input.IsNull();
        }

        public static bool IsNull(this object input)
        {
            return input == null;
        }

        public static Task<T> ToTask<T>(this T input)
        {
            return Task.FromResult(input);
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