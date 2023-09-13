using Newtonsoft.Json;
using System.IO;
using System.Xml;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Nextended.Core.Helper;

public class StructuredDataTypeValidator
{
    public static StructuredDataType? DetectInputType(string content)
    {
        return TryDetectInputType(content, out var detectedType) ? detectedType : (StructuredDataType?)null;
    }

    public static bool TryDetectInputType(string content, out StructuredDataType detectedType)
    {
        var res = DetectStructuredDataType(content);
        detectedType = res ?? default;
        return res.HasValue;
    }

    private static StructuredDataType? DetectStructuredDataType(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var firstChar = content.TrimStart()[0];
        return firstChar switch
        {
            '{' => StructuredDataType.Json,
            '[' => StructuredDataType.Json,
            '<' => StructuredDataType.Xml,
            _ => content.Contains(":") ? StructuredDataType.Yaml : (StructuredDataType?) null
        };
    }

    public static bool IsValidData(string data, StructuredDataType dataType)
    {
        return dataType switch
        {
            StructuredDataType.Json => IsValidJson(data),
            StructuredDataType.Xml => IsValidXml(data),
            StructuredDataType.Yaml => IsValidYaml(data),
            _ => false
        };
    }

    public static bool IsValidJson(string data)
    {
        try
        {
            var obj = JsonConvert.DeserializeObject(data);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool IsValidXml(string data)
    {
        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(data);
            return true;
        }
        catch (XmlException)
        {
            return false;
        }
    }

    public static bool IsValidYaml(string data)
    {
        try
        {
            var yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(data));
            return !IsValidJson(data) && !IsValidXml(data);
        }
        catch (YamlException)
        {
            return false;
        }
    }
}
