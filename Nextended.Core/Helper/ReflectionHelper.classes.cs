using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Xml;
using System;
using System.Linq;
using YamlDotNet.Serialization;
using System.Xml.Linq;
using System.Collections.Generic;
using Nextended.Core.Extensions;
using System.Dynamic;

namespace Nextended.Core.Helper;

/// <summary>
/// Represents an object that can be converted to structured data formats.
/// </summary>
public interface IStructuredDataObject
{
    /// <summary>
    /// Converts the object to a string representation of the specified structured data type.
    /// </summary>
    /// <param name="dataType">The target structured data type format.</param>
    /// <returns>A string representation in the specified format.</returns>
    string ToString(StructuredDataType dataType);
}

/// <summary>
/// Interface for parsing structured data content into JObject instances.
/// </summary>
public interface IJObjectParser
{
    /// <summary>
    /// Parses the content string into a JObject.
    /// </summary>
    /// <param name="content">The content to parse.</param>
    /// <returns>A JObject representing the parsed content.</returns>
    JObject Parse(string content);
}


/// <summary>
/// Parser for JSON content that converts it to JObject instances.
/// </summary>
public class JsonJObjectParser : IJObjectParser
{
    /// <summary>
    /// Parses JSON content into a JObject.
    /// </summary>
    /// <param name="content">The JSON content to parse.</param>
    /// <returns>A JObject representing the parsed JSON.</returns>
    public JObject Parse(string content) => JObject.Parse(content);
}

/// <summary>
/// Parser for XML content that converts it to JObject instances.
/// </summary>
public class XmlJObjectParser : IJObjectParser
{
    /// <summary>
    /// Parses XML content into a JObject.
    /// </summary>
    /// <param name="content">The XML content to parse.</param>
    /// <returns>A JObject representing the parsed XML.</returns>
    public JObject Parse(string content)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(content);

        // Assuming the root node after the XML declaration is the one you want
        XmlNode primaryNode = xmlDoc.DocumentElement;

        if (primaryNode == null)
            throw new InvalidOperationException("The XML content does not contain a valid primary element.");

        // Serialize only the primary node
        string jsonString = JsonConvert.SerializeXmlNode(primaryNode);
        JObject outerObject = JObject.Parse(jsonString);

        // Extract the inner content
        JProperty primaryProperty = outerObject.Properties().FirstOrDefault();
        if (primaryProperty != null)
        {
            JToken innerContent = primaryProperty.Value;
            if (innerContent is JObject)
                return (JObject)innerContent;
        }

        throw new InvalidOperationException("Unable to extract inner content from the XML.");
    }
}


/// <summary>
/// Parser for YAML content that converts it to JObject instances.
/// </summary>
public class YamlJObjectParser : IJObjectParser
{
    /// <summary>
    /// Parses YAML content into a JObject.
    /// </summary>
    /// <param name="content">The YAML content to parse.</param>
    /// <returns>A JObject representing the parsed YAML.</returns>
    public JObject Parse(string content)
    {        
        var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
        var yamlObject = deserializer.Deserialize<object>(content);
        string jsonString = JsonConvert.SerializeObject(yamlObject);
        return JObject.Parse(jsonString);
    }
}

/// <summary>
/// Enumeration of supported structured data types.
/// </summary>
public enum StructuredDataType
{
    /// <summary>
    /// JavaScript Object Notation format.
    /// </summary>
    Json,
    
    /// <summary>
    /// Extensible Markup Language format.
    /// </summary>
    Xml,
    
    /// <summary>
    /// YAML Ain't Markup Language format.
    /// </summary>
    Yaml
}

public static class StructuredDataFormatConverter
{
    private static readonly XDeclaration _defaultDeclaration = new("1.0", null, null);
    public static string ConvertToString(object obj, StructuredDataType dataType)
    {
        switch (dataType)
        {
            case StructuredDataType.Json:
                return JsonConvert.SerializeObject(obj);
            case StructuredDataType.Xml:
                var convertToString = $"{{ \"{obj.GetType().Name}\": {ConvertToString(obj, StructuredDataType.Json)} }}";
                var doc = JsonConvert.DeserializeXNode(convertToString)!;
                var declaration = doc.Declaration ?? _defaultDeclaration;
                return $"{declaration}{Environment.NewLine}{doc}";
 
            case StructuredDataType.Yaml:
                var serializer = new SerializerBuilder().Build();

                return obj is JObject jObject
                    ? serializer.Serialize(JsonConvert.DeserializeObject<ExpandoObject>(jObject.ToString()))
                    : serializer.Serialize(obj);

            default:
                throw new NotSupportedException($"The input type {dataType} is not supported.");
        }
    }
}