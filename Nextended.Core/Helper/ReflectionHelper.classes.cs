using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Xml;
using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using YamlDotNet.Serialization;

namespace Nextended.Core.Helper;

public interface IStructuredDataObject
{
    string ToString(StructuredDataType dataType);
}
public interface IJObjectParser
{
    JObject Parse(string content);
}


public class JsonJObjectParser : IJObjectParser
{
    public JObject Parse(string content)
    {
        return JObject.Parse(content);
    }
}

public class XmlJObjectParser : IJObjectParser
{
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


public class YamlJObjectParser : IJObjectParser
{
    public JObject Parse(string content)
    {        
        var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
        var yamlObject = deserializer.Deserialize<object>(content);
        string jsonString = JsonConvert.SerializeObject(yamlObject);
        return JObject.Parse(jsonString);
    }
}

public enum StructuredDataType
{
    Json,
    Xml,
    Yaml
}

public static class StructuredDataFormatConverter
{
    public static string ConvertToString(object obj, StructuredDataType dataType)
    {
        switch (dataType)
        {
            case StructuredDataType.Json:
                return JsonConvert.SerializeObject(obj);

            case StructuredDataType.Xml:
                using (var textWriter = new StringWriter())
                {
                    var xmlSerializer = new XmlSerializer(obj.GetType());
                    xmlSerializer.Serialize(textWriter, obj);
                    return textWriter.ToString();
                }

            case StructuredDataType.Yaml:
                var serializer = new SerializerBuilder().Build();
                return serializer.Serialize(obj);

            default:
                throw new NotSupportedException($"The input type {dataType} is not supported.");
        }
    }
}