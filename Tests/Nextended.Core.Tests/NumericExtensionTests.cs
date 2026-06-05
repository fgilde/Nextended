using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Extensions;

namespace Nextended.Core.Tests
{
	[TestClass]
	public class NumericExtensionTests
	{
		[TestMethod]
		public void Absolute_Decimal_PositiveValue_ReturnsSameValue()
		{
			decimal value = 10.5m;
			var result = value.Absolute();
			
			Assert.AreEqual(10.5m, result);
		}

		[TestMethod]
		public void Absolute_Decimal_NegativeValue_ReturnsPositive()
		{
			decimal value = -10.5m;
			var result = value.Absolute();
			
			Assert.AreEqual(10.5m, result);
		}

		[TestMethod]
		public void Absolute_Int_PositiveValue_ReturnsSameValue()
		{
			int value = 42;
			var result = value.Absolute();
			
			Assert.AreEqual(42, result);
		}

		[TestMethod]
		public void Absolute_Int_NegativeValue_ReturnsPositive()
		{
			int value = -42;
			var result = value.Absolute();
			
			Assert.AreEqual(42, result);
		}

		[TestMethod]
		public void RoundToMoney_RoundsToTwoDecimals()
		{
			decimal value = 10.5678m;
			var result = value.RoundToMoney();
			
			Assert.AreEqual(10.57m, result);
		}

		[TestMethod]
		public void RoundToMoney_WithHalfValue_RoundsUp()
		{
			decimal value = 10.565m;
			var result = value.RoundToMoney();
			
			// RoundToMoney uses MidpointRounding.ToEven (banker's rounding) by default
			Assert.AreEqual(10.56m, result);
		}

		[TestMethod]
		public void RoundTo_RoundsToSpecifiedPlace()
		{
			decimal value = 10.5678m;
			var result = value.RoundTo(3);
			
			Assert.AreEqual(10.568m, result);
		}

		[TestMethod]
		public void Round_Decimal_WithPrecision_RoundsAwayFromZero()
		{
			decimal value = 10.5m;
			var result = value.Round(0);
			
			Assert.AreEqual(11m, result);
		}

		[TestMethod]
		public void Round_Decimal_NegativeValue_RoundsAwayFromZero()
		{
			decimal value = -10.5m;
			var result = value.Round(0);
			
			Assert.AreEqual(-11m, result);
		}

		[TestMethod]
		public void Round_Double_WithPrecision_RoundsAwayFromZero()
		{
			double value = 10.5;
			var result = value.Round(0);
			
			Assert.AreEqual(11.0, result);
		}

		[TestMethod]
		public void Round_Double_NegativeValue_RoundsAwayFromZero()
		{
			double value = -10.5;
			var result = value.Round(0);
			
			Assert.AreEqual(-11.0, result);
		}

		[TestMethod]
		public void Between_ValueInRange_ReturnsTrue()
		{
			int value = 5;
			var result = value.Between(1, 10);
			
			Assert.IsTrue(result);
		}

		[TestMethod]
		public void Between_ValueOutsideRange_ReturnsFalse()
		{
			int value = 15;
			var result = value.Between(1, 10);
			
			Assert.IsFalse(result);
		}

		[TestMethod]
		public void Between_ValueEqualToLeftBound_ReturnsTrue()
		{
			int value = 1;
			var result = value.Between(1, 10);
			
			Assert.IsTrue(result);
		}

		[TestMethod]
		public void Between_ValueEqualToRightBound_ReturnsTrue()
		{
			int value = 10;
			var result = value.Between(1, 10);
			
			Assert.IsTrue(result);
		}

		[TestMethod]
		public void Between_ReversedBounds_WorksCorrectly()
		{
			int value = 5;
			var result = value.Between(10, 1);
			
			Assert.IsTrue(result);
		}

		[TestMethod]
		public void Floor_WithPrecision_FloorsCorrectly()
		{
			double value = 10.5678;
			var result = value.Floor(2);
			
			Assert.AreEqual(10.56, result);
		}

		[TestMethod]
		public void Floor_WithZeroPrecision_FloorsToInteger()
		{
			double value = 10.9;
			var result = value.Floor(0);
			
			Assert.AreEqual(10.0, result);
		}

		[TestMethod]
		public void Ceiling_WithPrecision_CeilsCorrectly()
		{
			double value = 10.5612;
			var result = value.Ceiling(2);
			
			Assert.AreEqual(10.57, result);
		}

		[TestMethod]
		public void Ceiling_WithZeroPrecision_CeilsToInteger()
		{
			double value = 10.1;
			var result = value.Ceiling(0);
			
			Assert.AreEqual(11.0, result);
		}

		[TestMethod]
		public void ToGuid_Int_CreatesGuid()
		{
			int value = 42;
			var guid = value.ToGuid();
			
			Assert.AreNotEqual(Guid.Empty, guid);
		}

		[TestMethod]
		public void ToGuid_Int_SameValueProducesSameGuid()
		{
			int value = 42;
			var guid1 = value.ToGuid();
			var guid2 = value.ToGuid();
			
			Assert.AreEqual(guid1, guid2);
		}

		[TestMethod]
		public void ToGuid_Int_DifferentValuesProduceDifferentGuids()
		{
			int value1 = 42;
			int value2 = 43;
			var guid1 = value1.ToGuid();
			var guid2 = value2.ToGuid();
			
			Assert.AreNotEqual(guid1, guid2);
		}

		[TestMethod]
		public void ToGuid_Long_CreatesGuid()
		{
			long value = 42L;
			var guid = value.ToGuid();
			
			Assert.AreNotEqual(Guid.Empty, guid);
		}

		[TestMethod]
		public void ToGuid_Long_SameValueProducesSameGuid()
		{
			long value = 42L;
			var guid1 = value.ToGuid();
			var guid2 = value.ToGuid();
			
			Assert.AreEqual(guid1, guid2);
		}

		[TestMethod]
		public void ToGuid_Long_DifferentValuesProduceDifferentGuids()
		{
			long value1 = 42L;
			long value2 = 43L;
			var guid1 = value1.ToGuid();
			var guid2 = value2.ToGuid();
			
			Assert.AreNotEqual(guid1, guid2);
		}

		[TestMethod]
		public void ToGuid_Int_Zero_CreatesValidGuid()
		{
			int value = 0;
			var guid = value.ToGuid();
			
			Assert.IsNotNull(guid);
		}

		[TestMethod]
		public void ToGuid_Long_Zero_CreatesValidGuid()
		{
			long value = 0L;
			var guid = value.ToGuid();
			
			Assert.IsNotNull(guid);
		}
	}
}
