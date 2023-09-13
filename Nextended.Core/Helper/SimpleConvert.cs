using System;
using System.Xml.Linq;

namespace Nextended.Core.Helper;

public static class SimpleConvert
{
    private static readonly XDeclaration _defaultDeclaration = new("1.0", null, null);
    public static string XmlToJson(string xml) => new XmlJObjectParser().Parse(xml).ToString();

    public static string JsonToXml(string json, string rootObjectName = "")
    {        
        var res = StructuredDataFormatConverter.ConvertToString(new JsonJObjectParser().Parse(json), StructuredDataType.Xml);
        if (!string.IsNullOrEmpty(rootObjectName))        
            res = res.Replace("<JObject>", $"<{rootObjectName}>").Replace("</JObject>", $"</{rootObjectName}>");
        
        return res;
    }

    public static string YamlToJson(string yaml) => new YamlJObjectParser().Parse(yaml).ToString();

    public static string JsonToYaml(string json) => StructuredDataFormatConverter.ConvertToString(new JsonJObjectParser().Parse(json), StructuredDataType.Yaml);

    public static string XmlToYaml(string xml) => StructuredDataFormatConverter.ConvertToString(new XmlJObjectParser().Parse(xml), StructuredDataType.Yaml);

    public static string YamlToXml(string yaml, string rootObjectName = "")
    {
        var res = StructuredDataFormatConverter.ConvertToString(new YamlJObjectParser().Parse(yaml), StructuredDataType.Xml);
        if (!string.IsNullOrEmpty(rootObjectName))
            res = res.Replace("<JObject>", $"<{rootObjectName}>").Replace("</JObject>", $"</{rootObjectName}>");
        return res;
    }
    public static string ConvertDataStringTo(string dataString, StructuredDataType target)
    {
        if (StructuredDataTypeValidator.TryDetectInputType(dataString, out var currentDataType))
            return ConvertDataStringTo(dataString, currentDataType, target);
        throw new ArgumentException("Cannot determine the input format. Please specify it explicitly.");
    }

    public static string ConvertDataStringTo(string dataString, StructuredDataType currentDataType, StructuredDataType targetDataType)
    {
        return currentDataType switch
        {
            StructuredDataType.Json => targetDataType switch
            {
                StructuredDataType.Xml => JsonToXml(dataString),
                StructuredDataType.Yaml => JsonToYaml(dataString),
                StructuredDataType.Json => dataString,
                _ => throw new ArgumentException("Unsupported target data type.")
            },
            StructuredDataType.Xml => targetDataType switch
            {
                StructuredDataType.Json => XmlToJson(dataString),
                StructuredDataType.Yaml => XmlToYaml(dataString),
                StructuredDataType.Xml => dataString,
                _ => throw new ArgumentException("Unsupported target data type.")
            },
            StructuredDataType.Yaml => targetDataType switch
            {
                StructuredDataType.Json => YamlToJson(dataString),
                StructuredDataType.Xml => YamlToXml(dataString),
                StructuredDataType.Yaml => dataString,
                _ => throw new ArgumentException("Unsupported target data type.")
            },
            _ => throw new ArgumentException("Unsupported current data type.")
        };
    }


}