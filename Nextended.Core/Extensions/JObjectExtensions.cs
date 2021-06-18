using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Nextended.Core.Helper;

namespace Nextended.Core.Extensions
{
    public static class JObjectExtensions
    {
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