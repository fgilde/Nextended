using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Nextended.Core.Extensions;

namespace Nextended.Core.Helper
{
    /// <summary>
    ///     Kann JavaScript bauen
    /// </summary>
    public class JsStringBuilder
    {
        private readonly List<KeyValuePair<string, IDictionary<string, object>>> toGenerate;
        private string nameSpacePrefix;

        /// <summary>
        ///     Prefix
        /// </summary>
        public string NameSpacePrefix
        {
            get => nameSpacePrefix;
            set
            {
                if (value.EndsWith("."))
                    value = value.Substring(0, value.Length - 1);
                nameSpacePrefix = value;
            }
        }

        /// <summary>
        ///     Gibt an ob das Ergebnis minifiziert werden soll
        /// </summary>
        public bool MinifyResult { get; set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:System.Object" /> class.
        /// </summary>
        public JsStringBuilder(bool minfiResult, string namespacePrefix = "")
        {
            NameSpacePrefix = namespacePrefix;
            toGenerate = new List<KeyValuePair<string, IDictionary<string, object>>>();
            MinifyResult = minfiResult;
        }

        /// <summary>
        ///     Appends the function.
        /// </summary>
        public JsStringBuilder Append(Type type)
        {
            return Append(type.Name,
                type.ToDictionary((t, m) => t == typeof(string) || t.IsValueType || t == typeof(Version)));
        }

        /// <summary>
        ///     Appends the function.
        /// </summary>
        public JsStringBuilder Append(string functionName, object obj)
        {
            return Append(functionName, ReflectionHelper.GetProperties(obj));
        }

        /// <summary>
        ///     Appends the function.
        /// </summary>
        public JsStringBuilder Append(string functionName, IDictionary<string, object> dictionary)
        {
            toGenerate.Add(new KeyValuePair<string, IDictionary<string, object>>(functionName, dictionary));
            return this;
        }

        /// <summary>
        ///     Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        ///     A string that represents the current object.
        /// </returns>
        public Task<string> ToJsonAsync()
        {
            return Task.Run(() => ToString());
        }

        /// <summary>
        ///     Appends the function.
        /// </summary>
        public JsStringBuilder AppendFunction(Type type)
        {
            return AppendFunction(type.Name,
                type.ToDictionary((t, m) => t == typeof(string) || t.IsValueType || t == typeof(Version)));
        }

        /// <summary>
        ///     Appends the function.
        /// </summary>
        public JsStringBuilder AppendFunction(string functionName, object obj)
        {
            return AppendFunction(functionName, ReflectionHelper.GetProperties(obj));
        }

        /// <summary>
        ///     Appends the function.
        /// </summary>
        public JsStringBuilder AppendFunction(string functionName, IDictionary<string, object> dictionary)
        {
            toGenerate.Add(new KeyValuePair<string, IDictionary<string, object>>(functionName, dictionary));
            return this;
        }

        /// <summary>
        ///     Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        ///     A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            var bracketCount = 0;
            var prefix = string.Empty;
            if (!string.IsNullOrWhiteSpace(NameSpacePrefix)) // Namespaces für prefix aufbauen
            {
                var agg = string.Empty;
                var splits = NameSpacePrefix.Split('.');
                prefix = splits.Aggregate(prefix, (current, split) =>
                {
                    bracketCount++;
                    var r = current + agg + split + "={" + Environment.NewLine;
                    agg += split + ".";
                    return r;
                });
            }

            var result = prefix + string.Join(string.Empty,
                toGenerate.Select(pair => pair.Value.ToJavaScriptFunction(pair.Key, NameSpacePrefix)));
            result = result.Remove(result.Length - 1, 1); // Letztes Komma der function entfernen
            for (var i = 0; i < bracketCount; i++)
                result += "};";
            //TODO: MINFY
            //if (MinifyResult)
            //	return ContentMinifier.MinifyJavaScript(result);
            return result;
        }

        /// <summary>
        ///     Properties ausschließen
        /// </summary>
        public JsStringBuilder Ignore<T>(params Expression<Func<T>>[] members)
        {
            foreach (var expression in members)
            {
                var tg = toGenerate;
                for (var index = 0; index < tg.Count; index++)
                {
                    var keyValuePair = tg[index];
                    foreach (var member in keyValuePair.Value.ToList())
                        if (member.Key == expression.GetMemberName())
                            toGenerate[index].Value.Remove(member.Key);
                }
            }

            return this;
        }

        public JsStringBuilder AppendStaticMembers<T>(Func<T, object> selector = null)
        {
            return AppendStaticMembers<T, T>(selector);
        }

        public JsStringBuilder AppendStaticMembers<TOwner, TMember>(Func<TMember, object> selector = null)
        {
            return AppendStaticMembers(typeof(TOwner), typeof(TMember),
                selector == null ? null : new Func<object, object>(o => selector((TMember) o)));
        }

        public JsStringBuilder AppendStaticMembers(Type owerClassType, Type memberType = null,
            Func<object, object> selector = null)
        {
            return AppendObjectMembers(owerClassType, owerClassType.Name, memberType, selector);
        }

        public JsStringBuilder AppendObjectMembers(object o, string name = "", Type memberType = null,
            Func<object, object> selector = null)
        {
            var owerClassType = o is Type type ? type : o.GetType();
            var owner = o is Type ? null : o;
            memberType ??= owerClassType;

            var r = owerClassType.GetFields(BindingFlags.Static | BindingFlags.Public)
                .Where(info => info.FieldType == memberType)
                .ToDictionary(fi => fi.Name,
                    info => selector != null ? selector(info.GetValue(owner)) : info.GetValue(owner));
            return Append(name ?? owerClassType.Name, r);
        }
    }

    /// <summary>
    ///     JavaScriptBuilderExtensions
    /// </summary>
    internal static class JavaScriptBuilderExtensions
    {
        /// <summary>
        ///     Erzeugt aus einem <see cref="IDictionary{TKey,TValue}" /> einen <see cref="String" /> der eine JavaScript-Funktion
        ///     abbildet
        /// </summary>
        /// <param name="dictionary">Schlüssel-Wert Paare der Javascript-Funktion</param>
        /// <param name="functionName">Name der Javascript-Funktion</param>
        /// <param name="namespacePrefix">Namensraum (z.B CP.App um CP.App.Function zu bekommen )</param>
        internal static string ToJavaScriptFunction(this IDictionary<string, object> dictionary, string functionName,
            string namespacePrefix)
        {
            Check.Requires<ArgumentException>(!string.IsNullOrWhiteSpace(functionName));
            //if (!string.IsNullOrWhiteSpace(namespacePrefix) && !namespacePrefix.EndsWith("."))
            //	namespacePrefix += ".";
            var builder = new StringBuilder();
            builder.AppendFormat("{0}:{{", functionName);
            //builder.AppendFormat("function {0}.{1}()", Constants.JsFunctionPrefix, functionName);
            builder.AppendLine();
            dictionary.Apply(e => builder.AppendJsValue(string.Empty, e.Key, e.Value));
            builder.Length -= 3; // Letztes Komma des Members entfernen
            builder.Append("},");
            return builder.ToString();
        }

        private static void AppendJsValue(this StringBuilder builder, string functionName, string key, object value)
        {
            try
            {
                if (value != null)
                {
                    var format = "{0}{1}:'{2}',";
                    int i;
                    double d;
                    if (value is Version)
                    {
                        format = "{0}{1}:'{2}',";
                    }
                    else if (int.TryParse(value.ToString(), out i) || double.TryParse(value.ToString(), out d) ||
                             value is bool)
                    {
                        format = "{0}{1}:{2},";
                    }
                    else if (value is string || value is Guid || value is Uri || value is DateTime || value is TimeSpan)
                    {
                        if (value.ToString().Contains(@"\"))
                            value = value.ToString().Replace(@"\", @"\\");
                        if (value.ToString().Contains("'"))
                            value = value.ToString().Replace("'", "\\'");
                    }
                    else if (!value.GetType().IsValueType)
                    {
                        format = "{0}{1}:'{2}',";
                        value = value.JsonSerialize();
                        if (value.ToString().Contains(@"\"))
                            value = value.ToString().Replace(@"\", @"\\");
                        if (value.ToString().Contains("'"))
                            value = value.ToString().Replace("'", "\\'");
                    }

                    var valueString = value.ToString();
                    if (value is bool)
                        valueString = valueString.ToLower();
                    builder.AppendLine(string.Format(format, functionName, key, valueString));
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}