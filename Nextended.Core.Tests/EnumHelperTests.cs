using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Helper;

namespace Nextended.Core.Tests
{
	public enum TestEnum
	{
		[System.ComponentModel.Description("First Value")]
		First = 1,
		[System.ComponentModel.Description("Second Value")]
		Second = 2,
		Third = 3
	}

	[Flags]
	public enum TestFlagsEnum
	{
		None = 0,
		Read = 1,
		Write = 2,
		Execute = 4
	}

	[TestClass]
	public class EnumHelperTests
	{
		[TestMethod]
		public void Enum_Values_ReturnsAllValues()
		{
			var values = Enum<TestEnum>.Values.ToList();
			
			Assert.AreEqual(3, values.Count);
			Assert.IsTrue(values.Contains(TestEnum.First));
			Assert.IsTrue(values.Contains(TestEnum.Second));
			Assert.IsTrue(values.Contains(TestEnum.Third));
		}

		[TestMethod]
		public void Enum_ToArray_ReturnsArray()
		{
			var values = Enum<TestEnum>.ToArray();
			
			Assert.AreEqual(3, values.Length);
			Assert.IsInstanceOfType(values, typeof(TestEnum[]));
		}

		[TestMethod]
		public void Enum_GetValues_ReturnsAllValues()
		{
			var values = Enum<TestEnum>.GetValues().ToList();
			
			Assert.AreEqual(3, values.Count);
		}

		[TestMethod]
		public void Enum_GetName_ReturnsCorrectName()
		{
			var name = Enum<TestEnum>.GetName(TestEnum.First);
			
			Assert.AreEqual("First", name);
		}

		[TestMethod]
		public void Enum_Parse_ParsesCorrectly()
		{
			var value = Enum<TestEnum>.Parse("Second");
			
			Assert.AreEqual(TestEnum.Second, value);
		}

		[TestMethod]
		public void Enum_Parse_IgnoreCase_ParsesCorrectly()
		{
			var value = Enum<TestEnum>.Parse("second", true);
			
			Assert.AreEqual(TestEnum.Second, value);
		}

		[TestMethod]
		public void Enum_TryParse_ValidValue_ReturnsValue()
		{
			var value = Enum<TestEnum>.TryParse("Third");
			
			Assert.IsNotNull(value);
			Assert.AreEqual(TestEnum.Third, value.Value);
		}

		[TestMethod]
		public void Enum_TryParse_InvalidValue_ReturnsNull()
		{
			var value = Enum<TestEnum>.TryParse("Invalid");
			
			Assert.IsNull(value);
		}

		[TestMethod]
		public void Enum_TryParse_WithOutParameter_ValidValue_ReturnsTrue()
		{
			var success = Enum<TestEnum>.TryParse("First", out var value);
			
			Assert.IsTrue(success);
			Assert.AreEqual(TestEnum.First, value);
		}

		[TestMethod]
		public void Enum_TryParse_WithOutParameter_InvalidValue_ReturnsFalse()
		{
			var success = Enum<TestEnum>.TryParse("Invalid", out var value);
			
			Assert.IsFalse(success);
			Assert.AreEqual(default(TestEnum), value);
		}

		[TestMethod]
		public void Enum_Except_Array_ExcludesValues()
		{
			var values = Enum<TestEnum>.Except(TestEnum.First, TestEnum.Second).ToList();
			
			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(TestEnum.Third, values[0]);
		}

		[TestMethod]
		public void Enum_Except_Enumerable_ExcludesValues()
		{
			var exclude = new[] { TestEnum.First };
			var values = Enum<TestEnum>.Except(exclude).ToList();
			
			Assert.AreEqual(2, values.Count);
			Assert.IsFalse(values.Contains(TestEnum.First));
		}

		[TestMethod]
		public void Enum_DescriptionFor_WithAttribute_ReturnsDescription()
		{
			var description = Enum<TestEnum>.DescriptionFor(TestEnum.First);
			
			Assert.AreEqual("First Value", description);
		}

		[TestMethod]
		public void Enum_DescriptionFor_WithoutAttribute_ReturnsName()
		{
			var description = Enum<TestEnum>.DescriptionFor(TestEnum.Third);
			
			Assert.AreEqual("Third", description);
		}

		[TestMethod]
		public void Enum_ConvertAll_ConvertsToInt()
		{
			var values = Enum<TestEnum>.ConvertAll<int>().ToList();
			
			Assert.AreEqual(3, values.Count);
			Assert.IsTrue(values.Contains(1));
			Assert.IsTrue(values.Contains(2));
			Assert.IsTrue(values.Contains(3));
		}

		[TestMethod]
		public void Enum_TryGetEnumValue_ValidValue_ReturnsTrue()
		{
			var success = Enum<TestEnum>.TryGetEnumValue(2, out var value);
			
			Assert.IsTrue(success);
			Assert.AreEqual(TestEnum.Second, value);
		}

		[TestMethod]
		public void Enum_TryGetEnumValue_InvalidValue_ReturnsFalse()
		{
			var success = Enum<TestEnum>.TryGetEnumValue(99, out var value);
			
			Assert.IsFalse(success);
			Assert.IsNull(value);
		}

		[TestMethod]
		public void Enum_GetDictionary_ReturnsCorrectDictionary()
		{
			var dict = Enum<TestEnum>.GetDictionary();
			
			Assert.AreEqual(3, dict.Count);
			Assert.AreEqual("First", dict[TestEnum.First]);
			Assert.AreEqual("Second", dict[TestEnum.Second]);
			Assert.AreEqual("Third", dict[TestEnum.Third]);
		}

		[TestMethod]
		public void Enum_GetAttributes_ReturnsAttributes()
		{
			var attributes = Enum<TestEnum>.GetAttributes<DescriptionAttribute>(TestEnum.First).ToList();
			
			Assert.AreEqual(1, attributes.Count);
			Assert.AreEqual("First Value", attributes[0].Description);
		}

		[TestMethod]
		public void Enum_GetAttributes_NoAttributes_ReturnsEmpty()
		{
			var attributes = Enum<TestEnum>.GetAttributes<DescriptionAttribute>(TestEnum.Third).ToList();
			
			Assert.AreEqual(0, attributes.Count);
		}

		[TestMethod]
		public void EnumExtensions_ToDescriptionString_WithAttribute_ReturnsDescription()
		{
			var description = TestEnum.First.ToDescriptionString();
			
			Assert.AreEqual("First Value", description);
		}

		[TestMethod]
		public void EnumExtensions_ToDescriptionString_WithoutAttribute_ReturnsName()
		{
			var description = TestEnum.Third.ToDescriptionString();
			
			Assert.AreEqual("Third", description);
		}

		[TestMethod]
		public void EnumExtensions_GetCustomAttributes_ReturnsAttributes()
		{
			var attributes = TestEnum.First.GetCustomAttributes<DescriptionAttribute>(false);
			
			Assert.AreEqual(1, attributes.Length);
			Assert.AreEqual("First Value", attributes[0].Description);
		}

		[TestMethod]
		public void EnumExtensions_ToDictionary_Generic_ReturnsCorrectDictionary()
		{
			var dict = EnumExtensions.ToDictionary<TestEnum>();
			
			Assert.AreEqual(3, dict.Count);
			Assert.AreEqual(1, dict["First"]);
			Assert.AreEqual(2, dict["Second"]);
			Assert.AreEqual(3, dict["Third"]);
		}

		[TestMethod]
		public void EnumExtensions_ToDictionary_Type_ReturnsCorrectDictionary()
		{
			var dict = EnumExtensions.ToDictionary(typeof(TestEnum));
			
			Assert.AreEqual(3, dict.Count);
			Assert.IsTrue(dict.ContainsKey("First"));
			Assert.IsTrue(dict.ContainsKey("Second"));
			Assert.IsTrue(dict.ContainsKey("Third"));
		}

		[TestMethod]
		public void EnumExtensions_ToDictionary_WithCustomConverters_Works()
		{
			var dict = EnumExtensions.ToDictionary(
				typeof(TestEnum),
				e => e.ToDescriptionString(),
				o => $"Value_{o}");
			
			Assert.IsTrue(dict.Count > 0);
		}
	}
}
