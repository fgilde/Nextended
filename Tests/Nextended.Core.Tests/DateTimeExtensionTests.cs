using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Extensions;

namespace Nextended.Core.Tests
{
	[TestClass]
	public class DateTimeExtensionTests
	{
		[TestMethod]
		public void Between_DateWithinRange_ReturnsTrue()
		{
			var date = new DateTime(2024, 11, 5);
			var start = new DateTime(2024, 11, 1);
			var end = new DateTime(2024, 11, 10);
			
			Assert.IsTrue(date.Between(start, end));
		}

		[TestMethod]
		public void Between_DateOutsideRange_ReturnsFalse()
		{
			var date = new DateTime(2024, 11, 15);
			var start = new DateTime(2024, 11, 1);
			var end = new DateTime(2024, 11, 10);
			
			Assert.IsFalse(date.Between(start, end));
		}

		[TestMethod]
		public void Between_DateEqualToStart_ReturnsTrue()
		{
			var date = new DateTime(2024, 11, 1);
			var start = new DateTime(2024, 11, 1);
			var end = new DateTime(2024, 11, 10);
			
			Assert.IsTrue(date.Between(start, end));
		}

		[TestMethod]
		public void Between_DateEqualToEnd_ReturnsFalse()
		{
			var date = new DateTime(2024, 11, 10);
			var start = new DateTime(2024, 11, 1);
			var end = new DateTime(2024, 11, 10);
			
			Assert.IsFalse(date.Between(start, end));
		}

		[TestMethod]
		public void IsWeekend_Saturday_ReturnsTrue()
		{
			var saturday = new DateTime(2024, 11, 2); // Saturday
			Assert.IsTrue(saturday.IsWeekend());
		}

		[TestMethod]
		public void IsWeekend_Sunday_ReturnsTrue()
		{
			var sunday = new DateTime(2024, 11, 3); // Sunday
			Assert.IsTrue(sunday.IsWeekend());
		}

		[TestMethod]
		public void IsWeekend_Monday_ReturnsFalse()
		{
			var monday = new DateTime(2024, 11, 4); // Monday
			Assert.IsFalse(monday.IsWeekend());
		}

		[TestMethod]
		public void IsWeekday_Monday_ReturnsTrue()
		{
			var monday = new DateTime(2024, 11, 4); // Monday
			Assert.IsTrue(monday.IsWeekday());
		}

		[TestMethod]
		public void IsWeekday_Saturday_ReturnsFalse()
		{
			var saturday = new DateTime(2024, 11, 2); // Saturday
			Assert.IsFalse(saturday.IsWeekday());
		}

		[TestMethod]
		public void IsMonday_Monday_ReturnsTrue()
		{
			var monday = new DateTime(2024, 11, 4); // Monday
			Assert.IsTrue(monday.IsMonday());
		}

		[TestMethod]
		public void IsTuesday_Tuesday_ReturnsTrue()
		{
			var tuesday = new DateTime(2024, 11, 5); // Tuesday
			Assert.IsTrue(tuesday.IsTuesday());
		}

		[TestMethod]
		public void IsWednesday_Wednesday_ReturnsTrue()
		{
			var wednesday = new DateTime(2024, 11, 6); // Wednesday
			Assert.IsTrue(wednesday.IsWednesday());
		}

		[TestMethod]
		public void IsThursday_Thursday_ReturnsTrue()
		{
			var thursday = new DateTime(2024, 11, 7); // Thursday
			Assert.IsTrue(thursday.IsThursday());
		}

		[TestMethod]
		public void IsFriday_Friday_ReturnsTrue()
		{
			var friday = new DateTime(2024, 11, 8); // Friday
			Assert.IsTrue(friday.IsFriday());
		}

		[TestMethod]
		public void IsSaturday_Saturday_ReturnsTrue()
		{
			var saturday = new DateTime(2024, 11, 2); // Saturday
			Assert.IsTrue(saturday.IsSaturday());
		}

		[TestMethod]
		public void IsSunday_Sunday_ReturnsTrue()
		{
			var sunday = new DateTime(2024, 11, 3); // Sunday
			Assert.IsTrue(sunday.IsSunday());
		}

		[TestMethod]
		public void FirstDayOfMonth_ReturnsFirstDay()
		{
			var date = new DateTime(2024, 11, 15);
			var result = date.FirstDayOfMonth();
			
			Assert.AreEqual(1, result.Day);
			Assert.AreEqual(11, result.Month);
			Assert.AreEqual(2024, result.Year);
		}

		[TestMethod]
		public void LastDayOfMonth_ReturnsLastDay()
		{
			var date = new DateTime(2024, 11, 15);
			var result = date.LastDayOfMonth();
			
			Assert.AreEqual(30, result.Day);
			Assert.AreEqual(11, result.Month);
			Assert.AreEqual(2024, result.Year);
		}

		[TestMethod]
		public void IsFirstDayOfMonth_FirstDay_ReturnsTrue()
		{
			var date = new DateTime(2024, 11, 1);
			Assert.IsTrue(date.IsFirstDayOfMonth());
		}

		[TestMethod]
		public void IsFirstDayOfMonth_NotFirstDay_ReturnsFalse()
		{
			var date = new DateTime(2024, 11, 15);
			Assert.IsFalse(date.IsFirstDayOfMonth());
		}

		[TestMethod]
		public void IsLastDayOfMonth_LastDay_ReturnsTrue()
		{
			var date = new DateTime(2024, 11, 30);
			Assert.IsTrue(date.IsLastDayOfMonth());
		}

		[TestMethod]
		public void IsLastDayOfMonth_NotLastDay_ReturnsFalse()
		{
			var date = new DateTime(2024, 11, 15);
			Assert.IsFalse(date.IsLastDayOfMonth());
		}

		[TestMethod]
		public void AddWeekDays_PositiveDays_SkipsWeekends()
		{
			var friday = new DateTime(2024, 11, 8); // Friday
			var result = friday.AddWeekDays(1); // Should be Monday
			
			Assert.IsTrue(result.IsMonday());
			Assert.AreEqual(11, result.Day);
		}

		[TestMethod]
		public void AddWeekDays_NegativeDays_SkipsWeekends()
		{
			var monday = new DateTime(2024, 11, 4); // Monday
			var result = monday.AddWeekDays(-1); // Should be Friday
			
			Assert.IsTrue(result.IsFriday());
			Assert.AreEqual(1, result.Day);
		}

		[TestMethod]
		public void AddWeekDays_ZeroDays_ReturnsSameDate()
		{
			var date = new DateTime(2024, 11, 5);
			var result = date.AddWeekDays(0);
			
			Assert.AreEqual(date, result);
		}

		[TestMethod]
		public void ToUnixTimeStamp_ReturnsCorrectTimestamp()
		{
			var date = new DateTime(1970, 1, 1, 0, 0, 10); // 10 seconds after Unix epoch
			var timestamp = date.ToUnixTimeStamp();
			
			Assert.AreEqual(10, timestamp);
		}

		[TestMethod]
		public void FromUnixTimeStamp_ReturnsCorrectDateTime()
		{
			var timestamp = 10;
			var date = timestamp.FromUnixTimeStamp();
			
			Assert.AreEqual(new DateTime(1970, 1, 1, 0, 0, 10), date);
		}

		[TestMethod]
		public void FromUnixTimeStampInMilliseconds_ReturnsCorrectDateTime()
		{
			var timestamp = 10000.0; // 10 seconds in milliseconds
			var date = DateTimeExtensions.FromUnixTimeStampInMilliseconds(timestamp);
			
			Assert.AreEqual(new DateTime(1970, 1, 1, 0, 0, 10), date);
		}

		[TestMethod]
		public void ToHttpDate_ReturnsRFC1123Format()
		{
			var date = new DateTime(2024, 11, 5, 12, 0, 0, DateTimeKind.Utc);
			var result = date.ToHttpDate();
			
			Assert.IsNotNull(result);
			Assert.IsTrue(result.Contains("Nov") || result.Contains("2024"));
		}

		[TestMethod]
		public void Minutes_ReturnsCorrectMinutesSinceMidnight()
		{
			var date = new DateTime(2024, 11, 5, 2, 30, 0);
			var minutes = date.Minutes();
			
			Assert.AreEqual(150, minutes); // 2 hours * 60 + 30 minutes
		}

		[TestMethod]
		public void FirstOfMonth_ReturnsFirstDay()
		{
			var date = new DateTime(2024, 11, 15);
			var result = date.FirstOfMonth();
			
			Assert.AreEqual(1, result.Day);
			Assert.AreEqual(11, result.Month);
			Assert.AreEqual(2024, result.Year);
		}

		[TestMethod]
		public void LastOfMonth_ReturnsLastDay()
		{
			var date = new DateTime(2024, 2, 15); // February
			var result = date.LastOfMonth();
			
			Assert.AreEqual(29, result.Day); // 2024 is a leap year
			Assert.AreEqual(2, result.Month);
		}

		[TestMethod]
		public void Previous_FindsPreviousDayOfWeek()
		{
			var tuesday = new DateTime(2024, 11, 5); // Tuesday
			var previousMonday = tuesday.Previous(DayOfWeek.Monday);
			
			Assert.IsTrue(previousMonday.IsMonday());
			Assert.IsTrue(previousMonday < tuesday);
		}

		[TestMethod]
		public void Next_FindsNextDayOfWeek()
		{
			var tuesday = new DateTime(2024, 11, 5); // Tuesday
			var nextFriday = tuesday.Next(DayOfWeek.Friday);
			
			Assert.IsTrue(nextFriday.IsFriday());
			Assert.IsTrue(nextFriday > tuesday);
		}

		[TestMethod]
		public void PreviousOrCurrent_SameDay_ReturnsSameDate()
		{
			var tuesday = new DateTime(2024, 11, 5); // Tuesday
			var result = tuesday.PreviousOrCurrent(DayOfWeek.Tuesday);
			
			Assert.AreEqual(tuesday.Date, result);
		}

		[TestMethod]
		public void NextOrCurrent_SameDay_ReturnsSameDate()
		{
			var tuesday = new DateTime(2024, 11, 5); // Tuesday
			var result = tuesday.NextOrCurrent(DayOfWeek.Tuesday);
			
			Assert.AreEqual(tuesday.Date, result);
		}

		[TestMethod]
		public void ToShortISO_ReturnsYYYYMMDD()
		{
			var date = new DateTime(2024, 11, 5);
			var result = date.ToShortISO();
			
			Assert.AreEqual("2024-11-05", result);
		}

		[TestMethod]
		public void ToISO_ReturnsSortableFormat()
		{
			var date = new DateTime(2024, 11, 5, 14, 30, 0);
			var result = date.ToISO();
			
			Assert.IsTrue(result.Contains("2024-11-05"));
			Assert.IsTrue(result.Contains("14:30"));
		}

		[TestMethod]
		public void ToISOz_ReturnsRoundtripFormat()
		{
			var date = new DateTime(2024, 11, 5, 14, 30, 0, DateTimeKind.Utc);
			var result = date.ToISOz();
			
			Assert.IsNotNull(result);
			Assert.IsTrue(result.Contains("2024"));
		}

		[TestMethod]
		public void Min_ReturnsEarlierDate()
		{
			var date1 = new DateTime(2024, 11, 5);
			var date2 = new DateTime(2024, 11, 10);
			
			var result = date1.Min(date2);
			
			Assert.AreEqual(date1, result);
		}

		[TestMethod]
		public void Max_ReturnsLaterDate()
		{
			var date1 = new DateTime(2024, 11, 5);
			var date2 = new DateTime(2024, 11, 10);
			
			var result = date1.Max(date2);
			
			Assert.AreEqual(date2, result);
		}

		[TestMethod]
		public void TimeSpan_Max_ReturnsLongerDuration()
		{
			var ts1 = TimeSpan.FromHours(1);
			var ts2 = TimeSpan.FromHours(2);
			
			var result = DateTimeExtensions.Max(ts1, ts2);
			
			Assert.AreEqual(ts2, result);
		}

		[TestMethod]
		public void TimeSpan_Min_ReturnsShorterDuration()
		{
			var ts1 = TimeSpan.FromHours(1);
			var ts2 = TimeSpan.FromHours(2);
			
			var result = DateTimeExtensions.Min(ts1, ts2);
			
			Assert.AreEqual(ts1, result);
		}

		[TestMethod]
		public void TimeSpan_Multiply_WithDecimal_ReturnsMultiplied()
		{
			var ts = TimeSpan.FromHours(2);
			var result = ts.Multiply(2.5m);
			
			Assert.AreEqual(TimeSpan.FromHours(5), result);
		}

		[TestMethod]
		public void TimeSpan_Multiply_WithLong_ReturnsMultiplied()
		{
			var ts = TimeSpan.FromHours(2);
			var result = ts.Multiply(3L);
			
			Assert.AreEqual(TimeSpan.FromHours(6), result);
		}

		[TestMethod]
		public void TimeSpan_DividedBy_ReturnsDivided()
		{
			var ts = TimeSpan.FromHours(10);
			var result = ts.DividedBy(2);
			
			Assert.AreEqual(TimeSpan.FromHours(5), result);
		}

		[TestMethod]
		public void TimeSpan_IsPositive_PositiveTimeSpan_ReturnsTrue()
		{
			var ts = TimeSpan.FromHours(1);
			Assert.IsTrue(ts.IsPositive());
		}

		[TestMethod]
		public void TimeSpan_IsPositive_NegativeTimeSpan_ReturnsFalse()
		{
			var ts = TimeSpan.FromHours(-1);
			Assert.IsFalse(ts.IsPositive());
		}

		[TestMethod]
		public void TimeSpan_IsNegative_NegativeTimeSpan_ReturnsTrue()
		{
			var ts = TimeSpan.FromHours(-1);
			Assert.IsTrue(ts.IsNegative());
		}

		[TestMethod]
		public void TimeSpan_IsPositiveOrZero_Zero_ReturnsTrue()
		{
			var ts = TimeSpan.Zero;
			Assert.IsTrue(ts.IsPositiveOrZero());
		}

		[TestMethod]
		public void TimeSpan_IsNegativeOrZero_Zero_ReturnsTrue()
		{
			var ts = TimeSpan.Zero;
			Assert.IsTrue(ts.IsNegativeOrZero());
		}

		[TestMethod]
		public void AddDaysSafe_WithinRange_AddsCorrectly()
		{
			var date = new DateTime(2024, 11, 5);
			var result = date.AddDaysSafe(10);
			
			Assert.AreEqual(new DateTime(2024, 11, 15), result);
		}

		[TestMethod]
		public void AddDaysSafe_ExceedsMaxValue_ReturnsMaxValue()
		{
			var date = DateTime.MaxValue.AddDays(-1);
			var result = date.AddDaysSafe(10);
			
			Assert.AreEqual(DateTime.MaxValue, result);
		}

		[TestMethod]
		public void AddDaysSafe_ExceedsMinValue_ReturnsMinValue()
		{
			var date = DateTime.MinValue.AddDays(1);
			var result = date.AddDaysSafe(-10);
			
			Assert.AreEqual(DateTime.MinValue, result);
		}

		[TestMethod]
		public void FindWeekNumber_ReturnsValidWeekNumber()
		{
			var date = new DateTime(2024, 11, 5);
			var weekNumber = date.FindWeekNumber();
			
			Assert.IsTrue(weekNumber >= 1 && weekNumber <= 53);
		}
	}
}
