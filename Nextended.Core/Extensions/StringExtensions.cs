using System;
using System.Collections.Generic;
using System.IO;
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
        public static bool IsGuid(this string input) => Guid.TryParse(input, out _);

        public static string SkipChars(this string str, params char[] chars)
        {
            return string.Join(string.Empty, str.SkipWhile(chars.Contains));
        }

        public static string TakeCharacters(this string input, int count)
        {
            return input.IsNullOrEmpty() ? input : new string(input.Take(count).ToArray());
        }

        public static string Replace(this string s, IDictionary<string, string> dictionary)
        {
            return dictionary.Aggregate(s, (current, pair) => current.Replace(pair.Key, pair.Value));
        }

        public static string Replace(this string s, IDictionary<char, char> dictionary)
        {
            return dictionary.Aggregate(s, (current, pair) => current.Replace(pair.Key, pair.Value));
        }

        public static string Replace(this string s, string[] valuesToReplace, string newValue)
        {
            return valuesToReplace.Aggregate(s, (current, s1) => current.Replace(s1, newValue));
        }

        public static string ToEllipsis(this string input, int maxChars, char ellipseChar = '.', bool keepLength = false)
        {
            var numberOfCharactersToTake = maxChars - 3;
            return input.IsNullOrEmpty() ? input :
                input.Length <= maxChars ? input :
                string.Create(keepLength ? input.Length : maxChars, keepLength ? input : input.Substring(0,numberOfCharactersToTake), (span, s) =>
                {
                    s.AsSpan().CopyTo(span);
                    span[(keepLength ? maxChars : numberOfCharactersToTake)..].Fill(ellipseChar);
                });
        }

        public static string EnsureEndsWith(this string str, char toEndWith)
        {
            return EnsureEndsWith(str, toEndWith.ToString());
        }

        /// <summary>
        /// Returns <paramref name="str"/> with the minimal concatenation of <paramref name="ending"/> (starting from end) that
        /// results in satisfying .EndsWith(ending).
        /// </summary>
        /// <example>"hel".WithEnding("llo") returns "hello", which is the result of "hel" + "lo".</example>
        public static string EnsureEndsWith(this string str, string ending)
        {
            if (str == null)
                return ending;

            string result = str;

            // Right() is 1-indexed, so include these cases
            // * Append no characters
            // * Append up to N characters, where N is ending length
            for (int i = 0; i <= ending.Length; i++)
            {
                string tmp = result + ending.Right(i);
                if (tmp.EndsWith(ending))
                    return tmp;
            }

            return result;
        }

        /// <summary>Gets the rightmost <paramref name="length" /> characters from a string.</summary>
        /// <param name="value">The string to retrieve the substring from.</param>
        /// <param name="length">The number of characters to retrieve.</param>
        /// <returns>The substring.</returns>
        private static string Right(this string value, int length)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", length, "Length is less than zero");
            }

            return (length < value.Length) ? value.Substring(value.Length - length) : value;
        }

        /// <summary>
        /// Retrieves the first length characters from string. If string is shorter than length, returns string.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string Left(this string text, int length)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            var pathSize = text.Length < length ? text.Length : length;
            var result = text.Substring(0, pathSize);
            return result;
        }

        public static string EnsureStartsWith(this string str, char toStartWith)
        {
            return EnsureStartsWith(str, toStartWith.ToString());
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

        public static IEnumerable<string> GetLines(this string s)
        {
            using var reader = new StringReader(s);
            string line = reader.ReadLine();
            while (line != null)
            {
                yield return line;
                line = reader.ReadLine();
            }
        }

    }
}