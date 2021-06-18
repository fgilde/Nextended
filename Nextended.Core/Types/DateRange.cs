using System;
using System.Runtime.Serialization;

namespace Nextended.Core.Types
{
	/// <summary>
	/// Zeitbereich zwischen zwei Dates
	/// </summary>
	[DataContract]
	public class DateRange
	{
		/// <summary>
		/// Konstruktor
		/// </summary>
		/// <param name="startDate">Beginn</param>
		/// <param name="endDate">Ende</param>
		public DateRange(Date startDate, Date endDate)
		{
			if (startDate != null && endDate != null && startDate > endDate)
				throw new ArgumentException($"The start date '{startDate}' of a time step '{endDate}' must not be before the end date.");
			StartDate = startDate;
			EndDate = endDate;
		}

		/// <summary>
		/// Beginn
		/// </summary>
		[DataMember]
		public Date StartDate { get; set; }

		/// <summary>
		/// Ende
		/// </summary>
		[DataMember]
		public Date EndDate { get; set; }

		/// <summary>
		/// Gibt an, ob Datum innerhalb dieses Range liegt.
		/// </summary>
		/// <param name="date">Datum</param>
		/// <returns>true wenn Datum im Bereich liegt</returns>
		public bool IsInRange(Date date)
		{
            if (StartDate == null && EndDate == null)
				return true;
			if (StartDate == null)
				return date <= EndDate;
			if (EndDate == null)
				return StartDate <= date;

			return StartDate <= date && date <= EndDate;
		}

		/// <summary>
		/// Formatierung 
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"{StartDate} - {EndDate}";
		}
	}
}