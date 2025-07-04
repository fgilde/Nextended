using System;
using System.Collections.Generic;
using System.Globalization;
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
        public static string ToPascalCase(this string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var parts = Regex.Matches(input, @"[A-Z]?[a-z]+|[0-9]+|[A-Z]+(?![a-z])")
                .Cast<Match>()
                .Select(m => m.Value)
                .ToList();

            if (parts.Count == 0)
                parts = Regex.Split(input, @"[_\-\s]+").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

            for (int i = 0; i < parts.Count; i++)
                parts[i] = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(parts[i].ToLower());

            return string.Join("", parts);
        }

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

#if !NETSTANDARD2_0
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
#else
        public static string ToEllipsis(this string input, int maxChars, char ellipseChar = '.', bool keepLength = false)
        {
            // 1) Sonderfälle: leer oder kürzer/gleich maxChars
            if (string.IsNullOrEmpty(input))
                return input;

            if (input.Length <= maxChars)
                return input;
        
            // 2) Eigentliche Logik
            int numberOfCharactersToTake = maxChars - 3;
            if (numberOfCharactersToTake < 0)
                numberOfCharactersToTake = 0; // Edge-Case: maxChars < 3

            if (!keepLength)
            {
                // Variante A: Finaler String hat Länge maxChars
                // - Kopiere erste 'numberOfCharactersToTake' Zeichen
                // - Fülle die letzten 3 Zeichen mit ellipseChar
                var result = new char[maxChars];
            
                // Kopieren (aber nur so viele Zeichen, wie input hat)
                for (int i = 0; i < numberOfCharactersToTake && i < input.Length; i++)
                {
                    result[i] = input[i];
                }

                // Letzten 3 Zeichen mit ellipseChar füllen
                for (int i = numberOfCharactersToTake; i < maxChars; i++)
                {
                    result[i] = ellipseChar;
                }

                return new string(result);
            }
            else
            {
                // Variante B: Finaler String hat Originallänge (input.Length)
                // - Kopiere das gesamte input
                // - Überschreibe ab Index 'maxChars' alle Zeichen mit ellipseChar
                var result = input.ToCharArray();

                for (int i = maxChars; i < result.Length; i++)
                {
                    result[i] = ellipseChar;
                }

                return new string(result);
            }
        }
#endif

        private static int FindOverlap(string str, string affix, Func<string, string, bool> match, Func<string, int, string> extract)
        {
            int maxOverlap = Math.Min(str.Length, affix.Length);
            for (int i = maxOverlap; i > 0; i--)
            {
                if (match(str, extract(affix, i)))
                    return i;
            }
            return 0;
        }

        public static string EnsureEndsWith(this string str, string ending)
        {
            if (str == null) return ending ?? "";
            var overlap = FindOverlap(str, ending,
                (s, sub) => s.EndsWith(sub),
                (affix, i) => affix.Substring(0, i));
            return $"{str}{ending.Substring(overlap)}";
        }

        public static string EnsureStartsWith(this string str, string prefix)
        {
            if (str == null) return prefix ?? "";
            int overlap = FindOverlap(str, prefix,
                (s, sub) => s.StartsWith(sub),
                (affix, i) => affix.Substring(prefix.Length - i));
            return prefix.Substring(0, prefix.Length - overlap) + str;
        }

        public static string EnsureEndsWith(this string str, char toEndWith)
            => str.EnsureEndsWith(toEndWith.ToString());

        public static string EnsureStartsWith(this string str, char toStartWith)
            => str.EnsureStartsWith(toStartWith.ToString());



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