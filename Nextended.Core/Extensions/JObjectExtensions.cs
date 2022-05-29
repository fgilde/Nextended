using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Nextended.Core.Helper;

namespace Nextended.Core.Extensions
{
    public static class JObjectExtensions
    {
        public static IEnumerable<JToken> CaseSelectPropertyValues(this JToken token, string name)
        {
            var obj = token as JObject;
            if (obj == null)
                yield break;
            foreach (var property in obj.Properties())
            {
                if (name == null)
                    yield return property.Value;
                else if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    yield return property.Value;
            }
        }

        public static IEnumerable<JToken> CaseSelectPropertyValues(this IEnumerable<JToken> tokens, string name)
        {
            if (tokens == null)
                throw new ArgumentNullException();
            return tokens.SelectMany(t => t.CaseSelectPropertyValues(name));
        }

        public static IDictionary<string, string> ToFlatDictionary(this JObject jObject)
        {
            return JsonDictionaryConverter.Flatten(jObject);
        }

        public static IDictionary<string, object> ToDictionary(this JObject jObject)
        {
            return JsonDictionaryConverter.ConvertToDictionary(jObject);
        }
    }
}