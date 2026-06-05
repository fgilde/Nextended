using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Types;

namespace Nextended.Core.Tests
{
	[TestClass]
	public class DateTests
	{
		[TestMethod]
		public void Date_Constructor_WithDateTime_SetsDateWithoutTime()
		{
			var dateTime = new DateTime(2024, 11, 5, 14, 30, 45);
			var date = new Date(dateTime);
			
			Assert.AreEqual(2024, date.Year);
			Assert.AreEqual(11, date.Month);
			Assert.AreEqual(5, date.Day);
			Assert.AreEqual(new DateTime(2024, 11, 5, 0, 0, 0), date.DateTime);
		}

		[TestMethod]
		public void Date_Constructor_WithYearMonthDay_CreatesCorrectDate()
		{
			var date = new Date(2024, 11, 5);
			
			Assert.AreEqual(2024, date.Year);
			Assert.AreEqual(11, date.Month);
			Assert.AreEqual(5, date.Day);
		}

		[TestMethod]
		public void Date_Today_ReturnsCurrentDate()
		{
			var today = Date.Today;
			var expected = DateTime.Today;
			
			Assert.AreEqual(expected.Year, today.Year);
			Assert.AreEqual(expected.Month, today.Month);
			Assert.AreEqual(expected.Day, today.Day);
		}

		[TestMethod]
		public void Date_ToString_ReturnsFormattedString()
		{
			var date = new Date(2024, 11, 5);
			var result = date.ToString();
			
			Assert.IsNotNull(result);
			Assert.IsTrue(result.Contains("11") || result.Contains("5") || result.Contains("2024"));
		}

		[TestMethod]
		public void Date_Equals_SameDates_ReturnsTrue()
		{
			var date1 = new Date(2024, 11, 5);
			var date2 = new Date(2024, 11, 5);
			
			Assert.IsTrue(date1.Equals(date2));
			Assert.IsTrue(date1 == date2);
			Assert.IsFalse(date1 != date2);
		}

		[TestMethod]
		public void Date_Equals_DifferentDates_ReturnsFalse()
		{
			var date1 = new Date(2024, 11, 5);
			var date2 = new Date(2024, 11, 6);
			
			Assert.IsFalse(date1.Equals(date2));
			Assert.IsFalse(date1 == date2);
			Assert.IsTrue(date1 != date2);
		}

		[TestMethod]
		public void Date_CompareTo_EarlierDate_ReturnsNegative()
		{
			var earlier = new Date(2024, 11, 5);
			var later = new Date(2024, 11, 6);
			
			Assert.IsTrue(earlier.CompareTo(later) < 0);
		}

		[TestMethod]
		public void Date_CompareTo_LaterDate_ReturnsPositive()
		{
			var earlier = new Date(2024, 11, 5);
			var later = new Date(2024, 11, 6);
			
			Assert.IsTrue(later.CompareTo(earlier) > 0);
		}

		[TestMethod]
		public void Date_CompareTo_SameDate_ReturnsZero()
		{
			var date1 = new Date(2024, 11, 5);
			var date2 = new Date(2024, 11, 5);
			
			Assert.AreEqual(0, date1.CompareTo(date2));
		}

		[TestMethod]
		public void Date_CompareTo_Null_ReturnsPositive()
		{
			var date = new Date(2024, 11, 5);
			
			Assert.IsTrue(date.CompareTo(null) > 0);
		}

		[TestMethod]
		public void Date_LessThan_Operator_Works()
		{
			var earlier = new Date(2024, 11, 5);
			var later = new Date(2024, 11, 6);
			
			Assert.IsTrue(earlier < later);
			Assert.IsFalse(later < earlier);
			Assert.IsFalse(earlier < earlier);
		}

		[TestMethod]
		public void Date_GreaterThan_Operator_Works()
		{
			var earlier = new Date(2024, 11, 5);
			var later = new Date(2024, 11, 6);
			
			Assert.IsTrue(later > earlier);
			Assert.IsFalse(earlier > later);
			Assert.IsFalse(earlier > earlier);
		}

		[TestMethod]
		public void Date_LessThanOrEqual_Operator_Works()
		{
			var earlier = new Date(2024, 11, 5);
			var later = new Date(2024, 11, 6);
			var same = new Date(2024, 11, 5);
			
			Assert.IsTrue(earlier <= later);
			Assert.IsTrue(earlier <= same);
			Assert.IsFalse(later <= earlier);
		}

		[TestMethod]
		public void Date_GreaterThanOrEqual_Operator_Works()
		{
			var earlier = new Date(2024, 11, 5);
			var later = new Date(2024, 11, 6);
			var same = new Date(2024, 11, 5);
			
			Assert.IsTrue(later >= earlier);
			Assert.IsTrue(earlier >= same);
			Assert.IsFalse(earlier >= later);
		}

		[TestMethod]
		public void Date_GetHashCode_SameDates_ReturnsSameHashCode()
		{
			var date1 = new Date(2024, 11, 5);
			var date2 = new Date(2024, 11, 5);
			
			Assert.AreEqual(date1.GetHashCode(), date2.GetHashCode());
		}

		[TestMethod]
		public void Date_ImplicitConversion_ToDateTime_Works()
		{
			var date = new Date(2024, 11, 5);
			DateTime dateTime = date;
			
			Assert.AreEqual(2024, dateTime.Year);
			Assert.AreEqual(11, dateTime.Month);
			Assert.AreEqual(5, dateTime.Day);
		}

		[TestMethod]
		public void Date_ImplicitConversion_FromDateTime_Works()
		{
			var dateTime = new DateTime(2024, 11, 5, 14, 30, 0);
			Date date = dateTime;
			
			Assert.AreEqual(2024, date.Year);
			Assert.AreEqual(11, date.Month);
			Assert.AreEqual(5, date.Day);
			Assert.AreEqual(0, date.DateTime.Hour);
		}

		[TestMethod]
		public void Date_AddYears_ReturnsCorrectDate()
		{
			var date = new Date(2024, 11, 5);
			var result = date.AddYears(2);
			
			Assert.AreEqual(2026, result.Year);
			Assert.AreEqual(11, result.Month);
			Assert.AreEqual(5, result.Day);
		}

		[TestMethod]
		public void Date_AddMonths_ReturnsCorrectDate()
		{
			var date = new Date(2024, 11, 5);
			var result = date.AddMonths(3);
			
			Assert.AreEqual(2025, result.Year);
			Assert.AreEqual(2, result.Month);
			Assert.AreEqual(5, result.Day);
		}

		[TestMethod]
		public void Date_AddDays_ReturnsCorrectDate()
		{
			var date = new Date(2024, 11, 5);
			var result = date.AddDays(10);
			
			Assert.AreEqual(2024, result.Year);
			Assert.AreEqual(11, result.Month);
			Assert.AreEqual(15, result.Day);
		}

		[TestMethod]
		public void Date_AddDays_Negative_ReturnsCorrectDate()
		{
			var date = new Date(2024, 11, 5);
			var result = date.AddDays(-5);
			
			Assert.AreEqual(2024, result.Year);
			Assert.AreEqual(10, result.Month);
			Assert.AreEqual(31, result.Day);
		}

		[TestMethod]
		public void Date_GetMonthBetweenDates_ReturnsCorrectCount()
		{
			var start = new Date(2024, 1, 15);
			var end = new Date(2024, 6, 20);
			
			var months = Date.GetMonthBetweenDates(start, end);
			
			Assert.AreEqual(6, months);
		}

		[TestMethod]
		public void Date_GetMonthBetweenDates_AcrossYears_ReturnsCorrectCount()
		{
			var start = new Date(2023, 10, 1);
			var end = new Date(2024, 3, 1);
			
			var months = Date.GetMonthBetweenDates(start, end);
			
			Assert.AreEqual(6, months);
		}

		[TestMethod]
		public void Date_Operators_WithNull_WorkCorrectly()
		{
			Date date = new Date(2024, 11, 5);
			Date nullDate = null;
			
			Assert.IsFalse(date == nullDate);
			Assert.IsTrue(date != nullDate);
			Assert.IsFalse(nullDate == date);
			Assert.IsTrue(nullDate != date);
		}
	}
}
