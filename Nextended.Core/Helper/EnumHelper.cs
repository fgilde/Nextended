using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Nextended.Core.Extensions;

namespace Nextended.Core.Helper
{

    /// <summary>
    /// Enum extensions
    /// </summary>
    public static class Enum<T> where T : struct
    {
        public static string DescriptionFor(T value)
        {
            var type = value.GetType();
            if (!type.IsEnum)
            {
                throw new ArgumentException("Value must be of Enum type.");
            }

            var fi = type.GetField(value.ToString());
            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }

        /// <summary>
        /// Retrieve all values of an enum as a <see cref="ISet{T}"/>.
        /// </summary>
        /// <exception cref="ArgumentException">The requested type <typeparam name="T"/> is not an enumerated type</exception>
        public static IEnumerable<T> Values => Enum.GetValues(typeof(T)).Cast<T>();

        /// <summary>
        /// Retrieve all values of an enum except the values in <paramref name="sequence"/>.
        /// </summary>
        /// <param name="sequence"></param>
        public static IEnumerable<T> Except(IEnumerable<T> sequence)
        {
            return Values.Except(sequence);
        }

        /// <summary>
        /// Retrieve all values of an enum except the values in <paramref name="sequence"/>.
        /// </summary>
        /// <param name="sequence"></param>
        public static IEnumerable<T> Except(params T[] sequence)
        {
            return Values.Except(sequence);
        }

        /// <summary>
        /// Retrieve all values of an enum as array.
        /// </summary>
        /// <returns></returns>
        public static T[] ToArray()
        {
            return Values.ToArray();
        }

        /// <summary>
        /// Converts den <paramref name="name"/> to enum
        /// </summary>
        public static T Parse(string name, bool ignoreCase = false)
        {
            return (T)Enum.Parse(typeof(T), name, ignoreCase);
        }

        /// <summary>
        /// Converts den <paramref name="name"/> to enum
        /// </summary>
        /// <returns>Enumerationskonstante</returns>
        public static T? TryParse(string name, bool ignoreCase = false)
        {
            return !string.IsNullOrEmpty(name) && TryParse(name, out var value, ignoreCase) ? (T?)value : null;
        }

        /// <summary>
        /// Converts den <paramref name="name"/> to enum
        /// </summary>
        public static bool TryParse(string name, out T value, bool ignoreCase = false)
        {
            try
            {
                value = Parse(name, ignoreCase);
                return true;
            }
            catch (ArgumentException)
            {
                value = default(T);
                return false;
            }
        }

        /// <summary>
        /// Values for enum
        /// </summary>
        public static IEnumerable<T> GetValues()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        /// <summary>
        /// Name for enum value
        /// </summary>
        public static string GetName(T value)
        {
            return Enum.GetName(typeof(T), value);
        }

        /// <summary>
        /// Converts all values in enum to <typeparamref name="TOutput"/>. 
        /// </summary>
        public static IEnumerable<TOutput> ConvertAll<TOutput>()
        {
            try
            {
                return GetValues().Select(e => (TOutput)Convert.ChangeType(e, typeof(TOutput)));
            }
            catch (Exception)
            {
                return Enumerable.Empty<TOutput>();
            }
        }

        public static bool TryGetEnumValue(int value, out T? enumValue)
        {
            enumValue = Enum.GetValues(typeof(T)).Cast<T?>().SingleOrDefault(e => (int)Convert.ChangeType(e, typeof(int)) == value);
            return enumValue.HasValue;
        }

        public static IDictionary<T, string> GetDictionary()
        {
            return GetValues().ToDictionary(e => e, GetName);
        }

        public static Dictionary<T, IEnumerable<TAttribute>> GetDictionaryAttributes<TAttribute>(bool inherit = false) where TAttribute : Attribute
        {
            return Enum.GetValues(typeof(T)).OfType<T>()
                       .ToDictionary(e => e, e => GetAttributes<TAttribute>(e, inherit));
        }

        public static IEnumerable<TAttribute> GetAttributes<TAttribute>(T value, bool inherit) where TAttribute : Attribute
        {
            return inherit
                       ? value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(TAttribute), false)
                              .OfType<TAttribute>()
                       : value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(TAttribute), false)
                              .Where(a => a.GetType().IsAssignableFrom(typeof(TAttribute))).OfType<TAttribute>();
        }

        public static IEnumerable<TAttribute> GetAttributes<TAttribute>(T value) where TAttribute : Attribute
        {
            return GetAttributes<TAttribute>(value, false);
        }
    }

    public static class EnumExtensions
    {
        public static T ToEnum<T>(this string input) where T : struct
        {
            return input.MapTo<T>();
        }

        public static T ToEnum<T>(this int intValue) where T : Enum
        {
            return intValue.MapTo<T>();
        }
    }

}
