﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nextended.Core.Helper
{
    /// <summary>
    /// Provides static helpers to deal with flat dictionaries and works
    /// as official JsonConverter class to keep objects and arrays after converting
    /// </summary>
    public class JsonDictionaryConverter : JsonConverter
    {
#if !NETSTANDARD2_0
        public static JObject DictionaryToJObject(IDictionary<string, object> dictionary)
        {
            var result = new JObject();
            foreach (var (key, value) in dictionary)
            {
                if (value is IDictionary<string, object> dict)
                    result.Add(key, DictionaryToJObject(dict));
                else if (value is IEnumerable<object> list)
                    result.Add(key, new JArray(list));
                else
                    result.Add(key, JToken.FromObject(value));
            }

            return result;
        }
#else
        public static JObject DictionaryToJObject(IDictionary<string, object> dictionary)
        {
            var result = new JObject();

            foreach (var kvp in dictionary)
            {
                var key = kvp.Key;
                var value = kvp.Value;

                if (value is IDictionary<string, object> dict)
                {
                    // Rekursiver Aufruf, falls verschachtelte Dictionary-Strukturen vorliegen
                    result.Add(key, DictionaryToJObject(dict));
                }
                else if (value is IEnumerable<object> list)
                {
                    // Falls eine Liste (z. B. List<object>) übergeben wird
                    result.Add(key, new JArray(list));
                }
                else
                {
                    // Alle anderen Werte
                    result.Add(key, JToken.FromObject(value));
                }
            }

            return result;
        }
#endif


        /// <summary>
        /// Creates a flat dictionary for an object
        /// </summary>
        /// <param name="obj">Object to flatten</param>
        /// <returns> Flat dictionary with path as key and value as string </returns>
        public static Dictionary<string, string> Flatten(object obj, string separator = ".")
        {
            return Flatten(JObject.FromObject(obj), separator);
        }

        /// <summary>
        /// Creates a flat dictionary for a jObject
        /// </summary>
        /// <param name="jsonObject">JObject to flatten</param>
        /// <returns> Flat dictionary with path as key and value as string </returns>
        public static Dictionary<string, string> Flatten(JObject jsonObject, string separator = ".")
        {
            if (separator == ".")
            {
                IEnumerable<JToken> jTokens = jsonObject.Descendants().Where(p => !p.Any());
                Dictionary<string, string> results = jTokens.Aggregate(new Dictionary<string, string>(),
                    (properties, jToken) =>
                    {
                        properties.Add(jToken.Path, jToken.ToString());
                        return properties;
                    });
                return results;
            }

            var result = new Dictionary<string, string>();
            FlattenToken(jsonObject, result, "", separator);
            return result;

        }

        private static void FlattenToken(JToken token, Dictionary<string, string> result, string currentPath, string separator)
        {
            if (token is JValue value)
            {
                result[currentPath] = value.ToString();
            }
            else if (token is JObject obj)
            {
                foreach (var property in obj.Properties())
                {
                    var path = string.IsNullOrEmpty(currentPath) ? property.Name : currentPath + separator + property.Name;
                    FlattenToken(property.Value, result, path, separator);
                }
            }
            else if (token is JArray array)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    var path = $"{currentPath}{separator}{i}";
                    FlattenToken(array[i], result, path, separator);
                }
            }
        }

        /// <summary>
        /// Converts a jObject to a hashable dictionary 
        /// </summary>
        /// <param name="jObject">JObject to convert</param>
        /// <returns> Hierarchical dictionary with key and object values </returns>
        public static IDictionary<string, object> ConvertToDictionary(JObject jObject)
        {
            return jObject.ToObject<IDictionary<string, object>>(new JsonSerializer { Converters = { new JsonDictionaryConverter() } });
        }

        /// <summary>
        /// Converts a flat dictionary to an unflat one
        /// </summary>
        /// <param name="flatDictionary">Flat dictionary to convert</param>
        /// <returns> Hierarchical dictionary with key and object values </returns>
        public static IDictionary<string, object> ConvertToUnflattenDictionary(IDictionary<string, string> flatDictionary)
        {
            return ConvertToDictionary(Unflatten(flatDictionary));
        }

        /// <summary>
        ///  Creates a JObject by a given flat dictionary
        /// </summary>
        /// <param name="keyValues">Flat dictionary</param>
        /// <returns> Hierarchical JObject </returns>
        public static JObject Unflatten(IDictionary<string, string> keyValues)
        {
            JContainer result = null;
            JsonMergeSettings setting = new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Merge };
            foreach (var pathValue in keyValues)
            {
                if (result == null)
                {
                    result = UnflattenSingle(pathValue);
                }
                else
                {
                    result.Merge(UnflattenSingle(pathValue), setting);
                }
            }
            return result as JObject;
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType) { return typeof(IDictionary<string, object>).IsAssignableFrom(objectType); }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            WriteValue(writer, value);
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return ReadValue(reader);
        }

        private void WriteValue(JsonWriter writer, object value)
        {
            var t = JToken.FromObject(value);
            switch (t.Type)
            {
                case JTokenType.Object:
                    WriteObject(writer, value);
                    break;
                case JTokenType.Array:
                    WriteArray(writer, value);
                    break;
                default:
                    writer.WriteValue(value);
                    break;
            }
        }

        private void WriteObject(JsonWriter writer, object value)
        {
            writer.WriteStartObject();
            if (value is IDictionary<string, object> obj)
            {
                foreach (var kvp in obj)
                {
                    writer.WritePropertyName(kvp.Key);
                    WriteValue(writer, kvp.Value);
                }
            }
            writer.WriteEndObject();
        }

        private void WriteArray(JsonWriter writer, object value)
        {
            writer.WriteStartArray();
            var array = value as IEnumerable<object> ?? Enumerable.Empty<object>();
            foreach (var o in array)
            {
                WriteValue(writer, o);
            }
            writer.WriteEndArray();
        }

        private object ReadValue(JsonReader reader)
        {
            while (reader.TokenType == JsonToken.Comment)
            {
                if (!reader.Read()) throw new JsonSerializationException("Unexpected Token when converting IDictionary<string, object>");
            }

            switch (reader.TokenType)
            {
                case JsonToken.StartObject:
                    return ReadObject(reader);
                case JsonToken.StartArray:
                    return this.ReadArray(reader);
                case JsonToken.Integer:
                case JsonToken.Float:
                case JsonToken.String:
                case JsonToken.Boolean:
                case JsonToken.Undefined:
                case JsonToken.Null:
                case JsonToken.Date:
                case JsonToken.Bytes:
                    return reader.Value;
                default:
                    throw new JsonSerializationException
                        ($"Unexpected token when converting IDictionary<string, object>: {reader.TokenType}");
            }
        }

        private object ReadArray(JsonReader reader)
        {
            IList<object> list = new List<object>();

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.Comment:
                        break;
                    default:
                        var v = ReadValue(reader);

                        list.Add(v);
                        break;
                    case JsonToken.EndArray:
                        return list;
                }
            }

            throw new JsonSerializationException("Unexpected end when reading IDictionary<string, object>");
        }

        private object ReadObject(JsonReader reader)
        {
            var obj = new Dictionary<string, object>();

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                        var propertyName = reader.Value.ToString();

                        if (!reader.Read())
                        {
                            throw new JsonSerializationException("Unexpected end when reading IDictionary<string, object>");
                        }

                        var v = ReadValue(reader);

                        obj[propertyName] = v;
                        break;
                    case JsonToken.Comment:
                        break;
                    case JsonToken.EndObject:
                        return obj;
                }
            }

            throw new JsonSerializationException("Unexpected end when reading IDictionary<string, object>");
        }

        private static JContainer UnflattenSingle(KeyValuePair<string, string> keyValue)
        {
            string path = keyValue.Key;
            string value = keyValue.Value;
            var pathSegments = SplitPath(path);

            JContainer lastItem = null;
            //build from leaf to root
            foreach (var pathSegment in pathSegments.Reverse())
            {
                var type = GetJsonTokenType(pathSegment);
                switch (type)
                {
                    case JTokenType.Object:
                        var obj = new JObject();
                        if (null == lastItem)
                        {
                            obj.Add(pathSegment, value);
                        }
                        else
                        {
                            obj.Add(pathSegment, lastItem);
                        }
                        lastItem = obj;
                        break;
                    case JTokenType.Array:
                        var array = new JArray();
                        int index = GetArrayIndex(pathSegment);
                        array = FillEmpty(array, index);
                        if (lastItem == null)
                        {
                            array[index] = value;
                        }
                        else
                        {
                            array[index] = lastItem;
                        }
                        lastItem = array;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return lastItem;
        }

        public static IList<string> SplitPath(string path)
        {
            IList<string> result = new List<string>();
            Regex reg = new Regex(@"(?!\.)([^. ^\[\]]+)|(?!\[)(\d+)(?=\])");
            foreach (Match match in reg.Matches(path))
            {
                result.Add(match.Value);
            }
            return result;
        }

        private static JArray FillEmpty(JArray array, int index)
        {
            for (int i = 0; i <= index; i++)
            {
                array.Add(null);
            }
            return array;
        }

        private static JTokenType GetJsonTokenType(string pathSegment)
        {
            return int.TryParse(pathSegment, out _) ? JTokenType.Array : JTokenType.Object;
        }

        private static int GetArrayIndex(string pathSegment)
        {
            if (int.TryParse(pathSegment, out var result))
            {
                return result;
            }
            throw new Exception("Unable to parse array index: " + pathSegment);
        }
    }
}