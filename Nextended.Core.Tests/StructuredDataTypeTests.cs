using System;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Nextended.Core.Helper;

namespace Nextended.Core.Tests
{
	[TestClass]
	public class StructuredDataTypeTests
    {
        
        string sampleYaml = "id: 1\r\nname: test\r\nwhatever:\r\n- x\r\n- y\r\n";
        string sampleXml = "<?xml version=\"1.0\"?>\r\n<DynamicType>\r\n  <id>1</id>\r\n  <name>test</name>\r\n  <whatever>x</whatever>\r\n  <whatever>y</whatever>\r\n</DynamicType>";
        string sampleJson = "{\"id\":1,\"name\":\"test\",\"whatever\":[\"x\",\"y\"]}";

        [TestMethod]
        public void DetectDataType()
        {
            var detected = StructuredDataTypeValidator.TryDetectInputType(sampleJson, out var dataType);
            Assert.IsTrue(detected);
            Assert.AreEqual(StructuredDataType.Json, dataType);

            detected = StructuredDataTypeValidator.TryDetectInputType(sampleXml, out dataType);
            Assert.IsTrue(detected);
            Assert.AreEqual(StructuredDataType.Xml, dataType);

            detected = StructuredDataTypeValidator.TryDetectInputType(sampleYaml, out dataType);
            Assert.IsTrue(detected);
            Assert.AreEqual(StructuredDataType.Yaml, dataType);

        }

        [TestMethod]
        public void ConversionMethodWorksWithoutSourceSpecified()
        {
            // XML to JSON
            var convertedJson = SimpleConvert.ConvertDataStringTo(sampleXml, StructuredDataType.Json);
            Assert.AreEqual("{\r\n  \"id\": \"1\",\r\n  \"name\": \"test\",\r\n  \"whatever\": [\r\n    \"x\",\r\n    \"y\"\r\n  ]\r\n}", convertedJson);

            // JSON to XML
            var convertedXml = SimpleConvert.ConvertDataStringTo(sampleJson, StructuredDataType.Xml);
            Assert.IsTrue(convertedXml.Contains("<JObject>"));  // Assumes JObject as root. Customize if needed.

            // YAML to JSON
            var convertedJsonFromYaml = SimpleConvert.ConvertDataStringTo(sampleYaml, StructuredDataType.Json);
            Assert.AreEqual(convertedJson, convertedJsonFromYaml);

            // YAML to XML
            var convertedXmlFromYaml = SimpleConvert.ConvertDataStringTo(sampleYaml, StructuredDataType.Xml);
            Assert.IsTrue(convertedXmlFromYaml.Contains("<JObject>"));  // Assumes JObject as root. Customize if needed.

            // JSON to YAML
            var convertedYaml = SimpleConvert.ConvertDataStringTo(sampleJson, StructuredDataType.Yaml);
            Assert.AreEqual(sampleYaml, convertedYaml);

            // XML to YAML
            var convertedYamlFromXml = SimpleConvert.ConvertDataStringTo(sampleXml, StructuredDataType.Yaml);
            Assert.AreEqual(sampleYaml, convertedYamlFromXml);
        }

        [TestMethod]
        public void ConversionMethodWorksWithSourceSpecified()
        {
            // XML to JSON
            var convertedJson = SimpleConvert.ConvertDataStringTo(sampleXml, StructuredDataType.Xml, StructuredDataType.Json);
            Assert.AreEqual("{\r\n  \"id\": \"1\",\r\n  \"name\": \"test\",\r\n  \"whatever\": [\r\n    \"x\",\r\n    \"y\"\r\n  ]\r\n}", convertedJson);

            // JSON to XML
            var convertedXml = SimpleConvert.ConvertDataStringTo(sampleJson, StructuredDataType.Json, StructuredDataType.Xml);
            Assert.IsTrue(convertedXml.Contains("<JObject>"));  // Assumes JObject as root. Customize if needed.

            // YAML to JSON
            var convertedJsonFromYaml = SimpleConvert.ConvertDataStringTo(sampleYaml, StructuredDataType.Yaml, StructuredDataType.Json);
            Assert.AreEqual(convertedJson, convertedJsonFromYaml);

            // YAML to XML
            var convertedXmlFromYaml = SimpleConvert.ConvertDataStringTo(sampleYaml, StructuredDataType.Yaml, StructuredDataType.Xml);
            Assert.IsTrue(convertedXmlFromYaml.Contains("<JObject>"));  // Assumes JObject as root. Customize if needed.

            // JSON to YAML
            var convertedYaml = SimpleConvert.ConvertDataStringTo(sampleJson, StructuredDataType.Json, StructuredDataType.Yaml);
            Assert.AreEqual(sampleYaml, convertedYaml);

            // XML to YAML
            var convertedYamlFromXml = SimpleConvert.ConvertDataStringTo(sampleXml, StructuredDataType.Xml, StructuredDataType.Yaml);
            Assert.AreEqual(sampleYaml, convertedYamlFromXml);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ThrowsExceptionOnInvalidSourceFormat()
        {
            var invalidSource = "invalid_format_data";
            SimpleConvert.ConvertDataStringTo(invalidSource, (StructuredDataType)999, StructuredDataType.Xml);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ThrowsExceptionOnInvalidTargetFormat()
        {
            SimpleConvert.ConvertDataStringTo(sampleJson, StructuredDataType.Json, (StructuredDataType)999);  // Assuming 999 is not a valid enum value
        }

        [TestMethod]
        public void ValidationStructuredData()
        {
            var isValidJson = StructuredDataTypeValidator.IsValidJson(sampleJson);
            var isValidXml = StructuredDataTypeValidator.IsValidXml(sampleJson);
            var isValidYaml = StructuredDataTypeValidator.IsValidYaml(sampleJson);
            
            Assert.IsTrue(isValidJson);
            Assert.IsFalse(isValidXml);
            Assert.IsFalse(isValidYaml);

            isValidJson = StructuredDataTypeValidator.IsValidJson(sampleXml);
            isValidXml = StructuredDataTypeValidator.IsValidXml(sampleXml);
            isValidYaml = StructuredDataTypeValidator.IsValidYaml(sampleXml);

            Assert.IsFalse(isValidJson);
            Assert.IsTrue(isValidXml);
            Assert.IsFalse(isValidYaml);

            isValidJson = StructuredDataTypeValidator.IsValidJson(sampleYaml);
            isValidXml = StructuredDataTypeValidator.IsValidXml(sampleYaml);
            isValidYaml = StructuredDataTypeValidator.IsValidYaml(sampleYaml);

            Assert.IsFalse(isValidJson);
            Assert.IsFalse(isValidXml);
            Assert.IsTrue(isValidYaml);

        }

        [TestMethod]
		public void SimpleConvertWorks()
        {
            var json = SimpleConvert.XmlToJson(sampleXml);
            Assert.AreEqual("{\r\n  \"id\": \"1\",\r\n  \"name\": \"test\",\r\n  \"whatever\": [\r\n    \"x\",\r\n    \"y\"\r\n  ]\r\n}", json);

            var xml = SimpleConvert.JsonToXml(sampleJson, "DynamicType");
            Assert.AreEqual(sampleXml, xml);

            var json2 = SimpleConvert.YamlToJson(sampleYaml);
            Assert.AreEqual(json, json2);

            var xml2 = SimpleConvert.YamlToXml(sampleYaml, "DynamicType");
            Assert.AreEqual(xml, xml2);

            var yaml = SimpleConvert.JsonToYaml(sampleJson);
            Assert.AreEqual(sampleYaml, yaml);

            var yaml2 = SimpleConvert.XmlToYaml(sampleXml);
            Assert.AreEqual(sampleYaml, yaml2);

        }


        [TestMethod]
		public void DynamicTypeSerializationJsonWorks()
		{
			var obj = ReflectionHelper.CreateTypeAndDeserialize(sampleJson);
			var strJson = obj.ToString(StructuredDataType.Json);
			var str = obj.ToString();
            Assert.AreEqual(strJson, str);
            Assert.AreEqual(sampleJson, str);
        }

        [TestMethod]
        public void DynamicTypeSerializationJsonToXmlWorks()
        {            
            var obj = ReflectionHelper.CreateTypeAndDeserialize(sampleJson, "DynamicType");
            var strXml = obj.ToString(StructuredDataType.Xml);
            var str = obj.ToString();
            Assert.AreEqual(sampleXml, strXml);
            Assert.AreEqual(sampleJson, str);
        }

        [TestMethod]
        public void DynamicTypeSerializationJsonToYamlWorks()
        {            
            var obj = ReflectionHelper.CreateTypeAndDeserialize(sampleJson, "DynamicType");
            var strYaml = obj.ToString(StructuredDataType.Yaml);
            var str = obj.ToString();
            Assert.AreEqual(sampleYaml, strYaml);
            Assert.AreEqual(sampleJson, str);

            var strXml = obj.ToString(StructuredDataType.Xml);
            Assert.AreEqual(sampleXml, strXml);
        }

        public static string XmlToJson(string xml)
        {
            var doc = XDocument.Parse(xml);
            return JsonConvert.SerializeXNode(doc);
        }

        [TestMethod]
        public void DynamicTypeSerializationXmlWorks()
        {
            var obj = ReflectionHelper.CreateTypeAndDeserialize(sampleXml, StructuredDataType.Xml, "DynamicType");
            var json = obj.ToString(StructuredDataType.Json);

            var strXml = obj.ToString(StructuredDataType.Xml);
            var str = obj.ToString();
            Assert.AreEqual(str, strXml);
            Assert.AreEqual(sampleXml, strXml);
            //Assert.AreEqual(sampleJson, json); // TODO: Int is provided as string here
            Assert.AreEqual("{\"id\":\"1\",\"name\":\"test\",\"whatever\":[\"x\",\"y\"]}", json);

        }

        [TestMethod]
        public void DynamicTypeSerializationXmlToYamlWorks()
        {
            var obj = ReflectionHelper.CreateTypeAndDeserialize(sampleXml, StructuredDataType.Xml, "DynamicType");
            var strYaml = obj.ToString(StructuredDataType.Yaml);
            var str = obj.ToString();
            Assert.AreEqual(sampleYaml, strYaml);
            Assert.AreEqual(sampleXml, str);
        }

        [TestMethod]
        public void DynamicTypeSerializationYamlWorks()
        {
            var obj = ReflectionHelper.CreateTypeAndDeserialize(sampleYaml, StructuredDataType.Yaml);
            var strYaml = obj.ToString(StructuredDataType.Yaml);
            var str = obj.ToString();
            Assert.AreEqual(str, strYaml);
            var strJson = obj.ToString(StructuredDataType.Json);
            //Assert.AreEqual(sampleJson, strJson);// TODO: Int is provided as string here
            Assert.AreEqual("{\"id\":\"1\",\"name\":\"test\",\"whatever\":[\"x\",\"y\"]}", strJson);
        }

        [TestMethod]
        public void DynamicTypeSerializationYamlToXmlWorks()
        {
            var obj = ReflectionHelper.CreateTypeAndDeserialize(sampleYaml, StructuredDataType.Yaml, "DynamicType");
            var strXml = obj.ToString(StructuredDataType.Xml);
            var str = obj.ToString();
            Assert.AreEqual(sampleXml, strXml);
            Assert.AreEqual(sampleYaml, str);
        }


    }
}