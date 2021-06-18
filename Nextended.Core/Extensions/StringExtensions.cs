using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Nextended.Core.Extensions
{
    public enum ContainsType
    {
        /// <summary>
        /// Ein zeichen muss enthalten sein
        /// </summary>
        Any,

        /// <summary>
        /// Alle zeichen m�ssen enthalten sein
        /// </summary>
        All,
    }

    public static class StringExtensions
    {
        public static bool IsGuid(this string input) => Guid.TryParse(input, out Guid result);

        public static string TakeCharacters(this string input, int count)
        {
            return input.IsNullOrEmpty() ? input : new string(input.Take(count).ToArray());
        }

        public static string ToEllipsis(this string input, int maxChars)
        {
            var numberOfCharactersToTake = maxChars - 3;
            return input.IsNullOrEmpty() ? input :
                input.Length <= maxChars ? input :
                new string(input.Take(numberOfCharactersToTake).ToArray()) + "...";
        }

        public static string EnsureEndsWith(this string str, char toEndWith)
        {
            return EnsureEndsWith(str, toEndWith.ToString());
        }

		public static string EnsureEndsWith(this string str, string toEndWith)
        {
            if (!str.EndsWith(toEndWith))
                str += toEndWith;
            return str;
        }

        public static string EnsureStartsWith(this string str, char toStartWith)
        {
            return EnsureEndsWith(str, toStartWith.ToString());
        }

        public static string EnsureStartsWith(this string str, string toStartWith)
        {
            if (!str.StartsWith(toStartWith))
                str = toStartWith + str;
            return str;
        }

        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        public static bool HasValue(this string input)
        {
            return !input.IsNullOrEmpty();
        }

        public static string Copy(this string input)
        {
            return input == null ? null : string.Copy(input);
        }

        public static string FormatWith(this string format, params object[] args)
        {
            return string.Format(format, args);
        }

        public static string JoinWith<T>(this IEnumerable<T> values, string separator)
        {
            return string.Join(separator, values.EmptyIfNull());
        }

        public static bool ContainsAny(this string input, params string[] contains)
        {
            return input.Contains(ContainsType.Any, contains);
        }

        public static bool ContainsAll(this string input, params string[] contains)
        {
            return input.Contains(ContainsType.All, contains);
        }

        public static string Capitalize(this string input) => ToUpper(input, true);
        public static string Uncapitalize(this string input) => ToLower(input, true);

        public static string ToLower(this string input, bool firstCharOnly)
        {
            if (!firstCharOnly) return input.ToLower();
            string temp = input.Substring(0, 1);
            return temp.ToLower() + input.Remove(0, 1);
        }

        public static string ToUpper(this string input, bool firstCharOnly)
        {
            if (!firstCharOnly) return input.ToUpper();
            string temp = input.Substring(0, 1);
            return temp.ToUpper() + input.Remove(0, 1);
        }

        public static bool Contains(this string s, ContainsType type, params char[] chars)
        {
            return type == ContainsType.Any ? chars.Any(s.Contains) : chars.All(s.Contains);
        }


        public static bool Contains(this string s, params string[] values)
        {
            return s.Contains(ContainsType.Any, values);
        }


        public static bool Contains(this string s, ContainsType type, params string[] values)
        {
            return type == ContainsType.Any ? values.Any(s.Contains) : values.All(s.Contains);
        }


        public static bool Contains(this string s, params char[] chars)
        {
            return s.Contains(ContainsType.Any, chars);
        }

        public static string EncodeBase64(this string plainText) => Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText)).Replace("/", "-");


        public static string DecodeBase64(this string base64EncodedData) => Encoding.UTF8.GetString(Convert.FromBase64String(base64EncodedData.Replace("-", "/")));

        public static void ThrowIfNullOrEmpty(this string input, string paramName)
        {
            if (input.IsNullOrEmpty())
            {
                throw new ArgumentException("Argument '{0}' is null or empty and shouldn't be.".FormatWith(paramName),
                    paramName);
            }
        }

        public static bool IsEmailAddress(this string input)
        {
            var match = Regex.Match(input,
                @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z",
                RegexOptions.IgnoreCase);
            return match.Success;
        }

        public static string[] SplitByUpperCase(this string str)
        {
           return Regex.Split(str, @"(?<!^)(?=[A-Z])");
        }

    }
}