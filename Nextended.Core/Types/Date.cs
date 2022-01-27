using System;
using System.Runtime.Serialization;

namespace Nextended.Core.Types
{
	/// <summary>
	/// Ein Datum (ohne Zeit)
	/// </summary>
	[DataContract]
	[Obsolete("Use DateOnly instead")]
	public class Date : IComparable
	{
		/// <summary>
		/// DateTime
		/// </summary>
		private DateTime dateTime;

		/// <summary>
		/// Konstruktor für Date mit DateTime
		/// </summary>
		/// <param name="value"></param>
		public Date(DateTime value)
		{
			DateTime = new DateTime(value.Year, value.Month, value.Day, 0, 0, 0, 0);
		}

		/// <summary>
		/// Konstruktor für Date mit Jahr, Monat und Tag.
		/// </summary>
		/// <param name="year">Das Jahr</param>
		/// <param name="month">Der Monat</param>
		/// <param name="day">Der Tag</param>
		public Date(int year, int month, int day)
		{
            DateTime = new DateTime(year, month, day);
		}

		/// <summary>
		/// Override ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return dateTime.ToShortDateString();
		}

		/// <summary>
		/// DateTime Zugriff
		/// </summary>
		[DataMember]
		public DateTime DateTime
		{
			get => dateTime;
            private set => dateTime = value;
        }
		
		/// <summary>
		/// Date now
		/// </summary>
		public static Date Today => new Date(DateTime.Today);

        /// <summary>
		/// Compare-Methode
		/// </summary>
		/// <param name="obj">Vergleichsobjekt</param>
		/// <returns>Vergleichsergebnis</returns>
		public int CompareTo(object obj)
		{
			switch (obj)
            {
                case null:
                case DBNull _:
                    return +1;
                case DateTime _:
                    return dateTime.CompareTo(obj);
            }

            var date = obj as Date;
			if (date == null)
				throw new ArgumentException("invalid object", "obj");

			return dateTime.CompareTo(date.dateTime);
		}

		/// <summary>
		/// Equals Methode
		/// </summary>
		/// <param name="obj">Vergleichsobjekt</param>
		/// <returns>Vergleichsergebnis</returns>
		public override bool Equals(object obj)
		{
			return (CompareTo(obj) == 0);
		}

		/// <summary>
		/// Get Hashcode muss überschrieben werden.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return dateTime.GetHashCode();
		}

		/// <summary>
		/// Überladen von Gleichheits-Operator "=="
		/// </summary>
		/// <param name="date1">erstes Vergleichsobjekt</param>
		/// <param name="date2">zweites Vergleichsobjekt</param>
		/// <returns>Vergleichsergebnis</returns>
		public static bool operator ==(Date date1, Date date2)
		{
            if ((object) date1 != null) return date1.Equals(date2);
            return date2 == null;
        }

		/// <summary>
		/// Überladen von Gleichheits-Operator "!="
		/// </summary>
		/// <param name="date1">erstes Vergleichsobjekt</param>
		/// <param name="date2">zweites Vergleichsobjekt</param>
		/// <returns>Vergleichsergebnis</returns>
		public static bool operator !=(Date date1, Date date2)
		{
			return !(date1 == date2);
		}

		/// <summary>
		/// Kleiner als Operator
		/// </summary>
		/// <param name="d1"></param>
		/// <param name="d2"></param>
		/// <returns></returns>
		public static bool operator <(Date d1, Date d2)
		{
			if (d1 == null && d2 == null)
				return false;
			if (d1 == null)
				return true;
			return (d1.CompareTo(d2) < 0);
		}

		/// <summary>
		/// Grösser als Operator
		/// </summary>
		/// <param name="d1"></param>
		/// <param name="d2"></param>
		/// <returns></returns>
		public static bool operator >(Date d1, Date d2)
		{
			if ((d1 == null) && (d2 == null))
				return false;
			if (d1 == null)
				return false;
			return (d1.CompareTo(d2) > 0);
		}
		/// <summary>
		/// Kleiner gleich Operator
		/// </summary>
		/// <param name="d1"></param>
		/// <param name="d2"></param>
		/// <returns></returns>
		public static bool operator <=(Date d1, Date d2)
		{
			return ((d1 < d2) || (d1 == d2));
		}

		/// <summary>
		/// Grösser gleich Operator
		/// </summary>
		/// <param name="d1"></param>
		/// <param name="d2"></param>
		/// <returns></returns>
		public static bool operator >=(Date d1, Date d2)
		{
			return ((d1 > d2) || (d1 == d2));
		}

		/// <summary>
		/// Implizite Konvertierung in ein DateTime
		/// </summary>
		public static implicit operator DateTime(Date date)
		{
			return date.DateTime;
		}
		
		/// <summary>
		/// Implizite Konvertierung von einem DateTime
		/// </summary>
		public static implicit operator Date(DateTime dateTime)
		{
			return new Date(dateTime);
		}

		/// <summary>
		/// Berechnet die Anzahl Monate zwischen zwei Datumsangaben
		/// </summary>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		/// <returns></returns>
		public static int GetMonthBetweenDates(Date startDate, Date endDate)
		{
			DateTime startTime = startDate.DateTime;
			DateTime endTime = endDate.DateTime;

			int months = (endTime.Year - startTime.Year) * 12 + (endTime.Month - startTime.Month) + 1;
			return months;
		}

		/// <summary>
		/// Liefert das Jahr
		/// </summary>
		public int Year => dateTime.Year;

        /// <summary>
		/// Liefert den Monat
		/// </summary>
		public int Month => dateTime.Month;

        /// <summary>
		/// Liefert den Tag des Monats
		/// </summary>
		public int Day => dateTime.Day;

        /// <summary>
		/// Addiert die Anzahl der Jahre zu dem Datum
		/// </summary>
		/// <param name="value">Jahre</param>
		public Date AddYears(int value)
		{
			return new Date(dateTime.AddYears(value));
		}

		/// <summary>
		/// Addiert die Anzahl der Monate zu dem Datum
		/// </summary>
		/// <param name="value">Monate</param>
		public Date AddMonths(int value)
		{
			return new Date(dateTime.AddMonths(value));
		}

		/// <summary>
		/// Addiert die Anzahl Tage zu dem Datum
		/// </summary>
		/// <param name="value">Tage</param>
		public Date AddDays(int value)
		{
			return new Date(dateTime.AddDays(value));
		}
	}
}