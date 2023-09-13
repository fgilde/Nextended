using System.Xml.Linq;

namespace Nextended.Core.Helper;

public static class SimpleConvert
{
    private static readonly XDeclaration _defaultDeclaration = new("1.0", null, null);
    public static string XmlToJson(string xml) => new XmlJObjectParser().Parse(xml).ToString();

    public static string JsonToXml(string json, string objectName = "")
    {        
        var res = StructuredDataFormatConverter.ConvertToString(new JsonJObjectParser().Parse(json), StructuredDataType.Xml);
        if (!string.IsNullOrEmpty(objectName))        
            res = res.Replace("<JObject>", $"<{objectName}>").Replace("</JObject>", $"</{objectName}>");
        
        return res;
    }

    public static string YamlToJson(string yaml) => new YamlJObjectParser().Parse(yaml).ToString();

    public static string JsonToYaml(string json) => StructuredDataFormatConverter.ConvertToString(new JsonJObjectParser().Parse(json), StructuredDataType.Yaml);

    public static string XmlToYaml(string xml) => StructuredDataFormatConverter.ConvertToString(new XmlJObjectParser().Parse(xml), StructuredDataType.Yaml);

    public static string YamlToXml(string yaml, string objectName = "")
    {
        var res = StructuredDataFormatConverter.ConvertToString(new YamlJObjectParser().Parse(yaml), StructuredDataType.Xml);
        if (!string.IsNullOrEmpty(objectName))
            res = res.Replace("<JObject>", $"<{objectName}>").Replace("</JObject>", $"</{objectName}>");
        return res;
    }
}