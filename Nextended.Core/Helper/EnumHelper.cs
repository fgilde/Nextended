using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Nextended.Core.Extensions;

namespace Nextended.Core.Helper
{

    /// <summary>
    /// Erweiterung für Enumerationen
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
        /// Konvertiert den <paramref name="name"/> in eine Enumerationskonstante. 
        /// </summary>
        /// <param name="name">Zeichenkette die konvertiert werden soll</param>
        /// <param name="ignoreCase">Groß- und Kleinschreibung berücksichtigen?</param>
        /// <returns>Enumerationskonstante</returns>
        public static T Parse(string name, bool ignoreCase = false)
        {
            return (T)Enum.Parse(typeof(T), name, ignoreCase);
        }

        /// <summary>
        /// Konvertiert den <paramref name="name"/> in eine Enumerationskonstante. 
        /// </summary>
        /// <param name="name">Zeichenkette die konvertiert werden soll</param>
        /// <param name="ignoreCase">Groß- und Kleinschreibung berücksichtigen?</param>
        /// <returns>Enumerationskonstante</returns>
        public static T? TryParse(string name, bool ignoreCase = false)
        {
            T value;
            return !string.IsNullOrEmpty(name) && TryParse(name, out value, ignoreCase) ? (T?)value : null;
        }

        /// <summary>
        /// Konvertiert den <paramref name="name"/> in eine Enumerationskonstante <paramref name="value"/>. 
        /// </summary>
        /// <param name="name">Zeichenkette die konvertiert werden soll</param>
        /// <param name="value">Enumerationskonstante</param>
        /// <param name="ignoreCase">Groß- und Kleinschreibung berücksichtigen?</param>
        /// <returns>Konvertierung erfolgreich?</returns>
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
        /// Liefert die Werte der Enumeration
        /// </summary>
        /// <returns>Liste der Enumerationswerte</returns>
        public static IEnumerable<T> GetValues()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        /// <summary>
        /// Liefert den Namen der Enumerationskonstante 
        /// </summary>
        /// <param name="value">Enumerationskonstante</param>
        /// <returns>Name der Enumerationskonstanten</returns>
        public static string GetName(T value)
        {
            return Enum.GetName(typeof(T), value);
        }

        /// <summary>
        ///  Konvertiert alle Werte der Enumeration in <typeparamref name="TOutput"/>. 
        /// </summary>
        /// <typeparam name="TOutput">Typ in den alle Werte der Enumeration konvertiert werden sollen</typeparam>
        /// <returns>Liste von <typeparamref name="TOutput"/></returns>
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

        /// <summary>
        /// Wandelt <paramref name="value"/> in einen Enum-Wert um, oder NULL wenn nicht möglich
        /// </summary>
        public static bool TryGetEnumValue(int value, out T? enumValue)
        {
            enumValue = Enum.GetValues(typeof(T)).Cast<T?>().SingleOrDefault(e => (int)Convert.ChangeType(e, typeof(int)) == value);
            return enumValue.HasValue;
        }

        /// <summary>
        /// Erstellt ein Dictionary mit den Werten der Enumeration und dessen Namen
        /// </summary>
        /// <returns>Dictionary mit den Werten der Enumeration und dessen Namen</returns>
        public static IDictionary<T, string> GetDictionary()
        {
            return GetValues().ToDictionary(e => e, GetName);
        }

        /// <summary>
        /// Erstellt ein Dictionary mit den Werten der Enumeration und dessen Attribute des Typs <typeparamref name="TAttribute"/>
        /// </summary>
        /// <param name="inherit">Vererbungskette berücksichtigen?</param>
        /// <typeparam name="TAttribute">Typ des zu suchenden Attributs</typeparam>
        /// <returns>Dictionary mit den Werten der Enumeration und dessen Attribute des Typs <typeparamref name="TAttribute"/></returns>
        public static Dictionary<T, IEnumerable<TAttribute>> GetDictionaryAttributes<TAttribute>(bool inherit = false) where TAttribute : Attribute
        {
            return Enum.GetValues(typeof(T)).OfType<T>()
                       .ToDictionary(e => e, e => GetAttributes<TAttribute>(e, inherit));
        }

        /// <summary>
        /// Liefert eine Liste der Attribute des Typs <typeparamref name="TAttribute"/> des Enumerationswertes <paramref name="value"/>
        /// </summary>
        /// <param name="value">Wert der Enumeration</param>
        /// <param name="inherit">Vererbungskette berücksichtigen?</param>
        /// <typeparam name="TAttribute">Typ des zu suchenden Attributs</typeparam>
        /// <returns>Liste von Attributen des Typs <typeparamref name="TAttribute"/> für den Enumerationswert <paramref name="value"/></returns>
        public static IEnumerable<TAttribute> GetAttributes<TAttribute>(T value, bool inherit) where TAttribute : Attribute
        {
            return inherit
                       ? value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(TAttribute), false)
                              .OfType<TAttribute>()
                       : value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(TAttribute), false)
                              .Where(a => a.GetType().IsAssignableFrom(typeof(TAttribute))).OfType<TAttribute>();
        }

        /// <summary>
        /// Liefert eine Liste der Attribute des Typs <typeparamref name="TAttribute"/> des Enumerationswertes <paramref name="value"/>
        /// </summary>
        /// <param name="value">Wert der Enumeration</param>
        /// <typeparam name="TAttribute">Typ des zu suchenden Attributs</typeparam>
        /// <returns>Liste von Attributen des Typs <typeparamref name="TAttribute"/> für den Enumerationswert <paramref name="value"/></returns>
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
