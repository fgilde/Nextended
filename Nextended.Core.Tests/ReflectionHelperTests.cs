using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Helper;

namespace Nextended.Core.Tests
{
	[TestClass]
	public class ReflectionHelperTests
    {

		[TestMethod]
		public void DynamicTypeSerializationJsonWorks()
		{
            string json = "{\"id\": 1,\"name\": \"test\",\"whatever\": [\"x\", \"y\"]}";
			var obj = ReflectionHelper.CreateTypeAndDeserialize(json);
			var strJson = obj.ToString(StructuredDataType.Json);
			var str = obj.ToString();
            Assert.AreEqual(strJson, str);
            Assert.AreEqual("{\"id\":1,\"name\":\"test\",\"whatever\":[\"x\",\"y\"]}", str);
        }

        [TestMethod]
        public void DynamicTypeSerializationJsonToXmlWorks()
        {
            string json = "{\"id\": 1,\"name\": \"test\",\"whatever\": [\"x\", \"y\"]}";
            var obj = ReflectionHelper.CreateTypeAndDeserialize(json, "DynamicType");
            var strXml = obj.ToString(StructuredDataType.Xml);
            var str = obj.ToString();
            Assert.AreEqual(strXml, "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<DynamicType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <id>1</id>\r\n  <name>test</name>\r\n  <whatever>\r\n    <string>x</string>\r\n    <string>y</string>\r\n  </whatever>\r\n</DynamicType>");
            Assert.AreEqual("{\"id\":1,\"name\":\"test\",\"whatever\":[\"x\",\"y\"]}", str);
        }

        [TestMethod]
        public void DynamicTypeSerializationJsonToYamlWorks()
        {
            string json = "{\"id\": 1,\"name\": \"test\",\"whatever\": [\"x\", \"y\"]}";
            var obj = ReflectionHelper.CreateTypeAndDeserialize(json, "DynamicType");
            var strYaml = obj.ToString(StructuredDataType.Yaml);
            var str = obj.ToString();
            Assert.AreEqual(strYaml, "id: 1\r\nname: test\r\nwhatever:\r\n- x\r\n- y\r\n");
            Assert.AreEqual("{\"id\":1,\"name\":\"test\",\"whatever\":[\"x\",\"y\"]}", str);

            var strXml = obj.ToString(StructuredDataType.Xml);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<DynamicType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <id>1</id>\r\n  <name>test</name>\r\n  <whatever>\r\n    <string>x</string>\r\n    <string>y</string>\r\n  </whatever>\r\n</DynamicType>", strXml);
        }

        //[TestMethod]
        //public void DynamicTypeSerializationXmlWorks()
        //{
        //    string xml = "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
        //                 "<DynamicType>" +
        //                 "  <id>1</id>" +
        //                 "  <name>test</name>" +
        //                 "  <whatever>" +
        //                 "    <string>x</string>" +
        //                 "    <string>y</string>" +
        //                 "  </whatever>" +
        //                 "</DynamicType>";

        //    var obj = ReflectionHelper.CreateTypeAndDeserialize(xml, StructuredDataType.Xml, "DynamicType");
        //    var strJson = obj.ToString(StructuredDataType.Json);
        //    obj = ReflectionHelper.CreateTypeAndDeserialize(strJson, StructuredDataType.Xml, "DynamicType");
        //    // Assert.AreEqual("{\"id\":\"1\",\"name\":\"test\",\"whatever\":{\"string\":[\"x\",\"y\"]}}", strJson);

        //    var strXml = obj.ToString(StructuredDataType.Xml);
        //    var str = obj.ToString();
        //    Assert.AreEqual(str, strXml);
        //}

        //[TestMethod]
        //public void DynamicTypeSerializationXmlToYamlWorks()
        //{
        //    string xml = "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
        //                 "<DynamicType>" +
        //                 "  <id>1</id>" +
        //                 "  <name>test</name>" +
        //                 "  <whatever>" +
        //                 "    <string>x</string>" +
        //                 "    <string>y</string>" +
        //                 "  </whatever>" +
        //                 "</DynamicType>";

        //    var obj = ReflectionHelper.CreateTypeAndDeserialize(xml, StructuredDataType.Xml, "DynamicType");
        //    var strYaml = obj.ToString(StructuredDataType.Yaml);
        //    var str = obj.ToString();
        //    Assert.AreEqual("id: 1\r\nname: test\r\nwhatever:\r\n- x\r\n- y", strYaml);
        //    Assert.AreEqual(xml, str);
        //}

        [TestMethod]
        public void DynamicTypeSerializationYamlWorks()
        {
            string yaml = "id: 1" +
                          "\r\nname: test" +
                          "\r\nwhatever:" +
                          "\r\n- x" +
                          "\r\n- y";

            var obj = ReflectionHelper.CreateTypeAndDeserialize(yaml, StructuredDataType.Yaml);
            var strYaml = obj.ToString(StructuredDataType.Yaml);
            var str = obj.ToString();
            Assert.AreEqual(str, strYaml);
            var strJson = obj.ToString(StructuredDataType.Json);
            Assert.AreEqual("{\"id\":\"1\",\"name\":\"test\",\"whatever\":[\"x\",\"y\"]}", strJson);
        }

        [TestMethod]
        public void DynamicTypeSerializationYamlToXmlWorks()
        {
            string yaml = "id: 1" +
                          "\r\nname: test" +
                          "\r\nwhatever:" +
                          "\r\n- x" +
                          "\r\n- y";

            var obj = ReflectionHelper.CreateTypeAndDeserialize(yaml, StructuredDataType.Yaml, "DynamicType");
            var strXml = obj.ToString(StructuredDataType.Xml);
            var str = obj.ToString();
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<DynamicType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <id>1</id>\r\n  <name>test</name>\r\n  <whatever>\r\n    <string>x</string>\r\n    <string>y</string>\r\n  </whatever>\r\n</DynamicType>", strXml);
            Assert.AreEqual("id: 1\r\nname: test\r\nwhatever:\r\n- x\r\n- y\r\n", str);
        }


    }
}