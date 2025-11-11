using Nextended.Core.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Nextended.Core.Extensions
{
	/// <summary>
	/// DateTimeExtensions
	/// </summary>
	public static class DateTimeExtensions
	{

        public static int MonthsBetween(this DateTime anchor, DateTime v)
            => (v.Year - anchor.Year) * 12 + (v.Month - anchor.Month);

        public static int MonthsBetween(this DateOnly anchor, DateOnly v)
            => (v.Year - anchor.Year) * 12 + (v.Month - anchor.Month);

        public static bool Between(this DateTime dateTime, DateTime startDate, DateTime endDate)
        {
            return dateTime >= startDate && dateTime < endDate;
        }
		public static int ToUnixTimeStamp(this DateTime dateTime)
		{
			DateTime unixStartDate = new DateTime(1970, 1, 1);
			TimeSpan timeSpan = new TimeSpan(dateTime.Ticks - unixStartDate.Ticks);
			return (Convert.ToInt32(timeSpan.TotalSeconds)); // Das Delta als Gesammtzahl der Sekunden ist der Timestamp
		}

		public static DateTime FromUnixTimeStamp(this int timestamp)
		{
			DateTime unixStartDate = new DateTime(1970, 1, 1);
			return unixStartDate.AddSeconds(timestamp); // den Timestamp addieren
		}

		public static DateTime FromUnixTimeStampInMilliseconds(double timestamp)
		{
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dateTime = dateTime.AddMilliseconds(timestamp);
			return dateTime;
		}

        public static bool IsWeekend(this DateTime input)
        {
            return input.DayOfWeek == DayOfWeek.Saturday || input.DayOfWeek == DayOfWeek.Sunday;
        }

        public static bool IsWeekday(this DateTime input)
        {
            return !input.IsWeekend();
        }

        public static bool IsMonday(this DateTime input)
        {
            return input.DayOfWeek == DayOfWeek.Monday;
        }

        public static bool IsTuesday(this DateTime input)
        {
            return input.DayOfWeek == DayOfWeek.Tuesday;
        }

        public static bool IsWednesday(this DateTime input)
        {
            return input.DayOfWeek == DayOfWeek.Wednesday;
        }

        public static bool IsThursday(this DateTime input)
        {
            return input.DayOfWeek == DayOfWeek.Thursday;
        }

        public static bool IsFriday(this DateTime input)
        {
            return input.DayOfWeek == DayOfWeek.Friday;
        }

        public static bool IsSaturday(this DateTime input)
        {
            return input.DayOfWeek == DayOfWeek.Saturday;
        }

        public static bool IsSunday(this DateTime input)
        {
            return input.DayOfWeek == DayOfWeek.Sunday;
        }

        public static DateTime LastDayOfMonth(this DateTime input)
        {
            return input.FirstDayOfMonth().AddMonths(1).AddDays(-1);
        }

        public static DateTime FirstDayOfMonth(this DateTime input)
        {
            return new DateTime(input.Year, input.Month, 1);
        }

        public static bool IsLastDayOfMonth(this DateTime input)
        {
            return input.Date == input.LastDayOfMonth();
        }

        public static bool IsFirstDayOfMonth(this DateTime input)
        {
            return input.Day == 1;
        }

        public static DateTime AddWeekDays(this DateTime date, int weekDays)
        {
            var direction = weekDays < 0 ? -1 : 1;
            var newDate = date;
            while (weekDays != 0)
            {
                newDate = newDate.AddDays(direction);
                if (newDate.IsWeekday())
                {
                    weekDays -= direction;
                }
            }

            return newDate;
        }

        //Format RFC1123 correspondant à ce qui est renvoyé par une requête HTTP dans le Header Last-Modified
        public static string ToHttpDate(this DateTime d)
        {
            return d.ToString("R");
        }

        public static int Minutes(this DateTime d)
        {
            return d.Minute + d.Hour * 60;
        }

        public static bool IsFirstOfMonth(DateTime d)
        {
            return d == FirstOfMonth(d);
        }

        public static DateTime FirstOfMonth(this DateTime d)
        {
            return new DateTime(d.Year, d.Month, 1);
        }

        public static bool IsLastOfMonth(DateTime d)
        {
            return d == LastOfMonth(d);
        }
        public static DateTime LastOfMonth(this DateTime d)
        {
            return new DateTime(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month));
        }

        public static DateTime Previous(this DateTime d, DayOfWeek day) { return LookFor(d.AddDaysSafe(-1), day, -1); }
        public static DateTime Next(this DateTime d, DayOfWeek day) { return LookFor(d.AddDaysSafe(1), day, 1); }
        public static DateTime PreviousOrCurrent(this DateTime d, DayOfWeek day) { return LookFor(d, day, -1); }
        public static DateTime NextOrCurrent(this DateTime d, DayOfWeek day) { return LookFor(d, day, 1); }
        static DateTime LookFor(DateTime start, DayOfWeek day, int step)
        {
            if (step % 7 == 0) throw new ArgumentException("Step should be less than 7");

            var d = start.Date;
            while (d.DayOfWeek != day)
            {
                d = d.AddDays(step);
            }
            return d;
        }

        public static string ToShortISO(this DateTime d)
        {
            return d.ToString("yyyy-MM-dd");
        }

        public static string ToISO(this DateTime d)
        {
            return d.ToString("s");
        }

        public static string ToISOz(this DateTime d)
        {
            return d.ToString("o");
        }

        public static int FindWeekNumber(this DateTime d)
        {
            return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(d, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        public static string ToShortDateTimeString(this DateTime d, CultureInfo cultureInfo)
        {
            return d.ToString("d", cultureInfo) + " " + d.ToString("t", cultureInfo);
        }

        public static DateTime Min(this DateTime a, DateTime b) { return a > b ? b : a; }
        public static DateTime Max(this DateTime a, DateTime b) { return a > b ? a : b; }

        public static TimeSpan Max(TimeSpan t1, TimeSpan t2)
        {
            return t1.Ticks > t2.Ticks ? t1 : t2;
        }

        public static TimeSpan Min(TimeSpan t1, TimeSpan t2)
        {
            return t1.Ticks < t2.Ticks ? t1 : t2;
        }

        public static TimeSpan Multiply(this TimeSpan t1, decimal factor)
        {
            return new TimeSpan((long)(t1.Ticks * factor));
        }

        public static TimeSpan Multiply(this TimeSpan t1, long factor)
        {
            return new TimeSpan(t1.Ticks * factor);
        }

        public static TimeSpan DividedBy(this TimeSpan t1, int divider)
        {
            return new TimeSpan(t1.Ticks / divider);
        }

        public static TimeSpan Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, TimeSpan> selector)
        {
            return new TimeSpan(source.Sum(t => selector(t).Ticks));
        }

        public static bool IsPositive(this TimeSpan t1) { return t1.Ticks > 0; }
        public static bool IsPositiveOrZero(this TimeSpan t1) { return t1.Ticks >= 0; }
        public static bool IsNegativeOrZero(this TimeSpan t1) { return t1.Ticks <= 0; }
        public static bool IsNegative(this TimeSpan t1) { return t1.Ticks < 0; }

        public static DateTime AddDaysSafe(this DateTime date, double value)
        {
            if ((DateTime.MaxValue - date).TotalDays > value)
                return (date - DateTime.MinValue).TotalDays > -value ? date.AddDays(value) : DateTime.MinValue;
            return DateTime.MaxValue;
        }
    }
}