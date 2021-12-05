using System;
using System.Diagnostics;
using Nextended.Core.Types;

namespace Nextended.Core.Helper
{
	/// <summary>
	/// Währungsimport information einer Währung
	/// </summary>
	[DebuggerDisplay("{Currency} Rate: {Rate} Date: {Date.Day}.{Date.Month}.{Date.Year}")]
	public class CurrencyImportInformation
	{
		/// <summary>
		/// Datum, zu dem dieser Kurs galt
		/// </summary>
		public Date Date { get; }

		/// <summary>
		/// Datum, zu dem dieser Kurs galt
		/// </summary>
		public DateRange DateRange { get; }

		/// <summary>
		/// Die Währung, um die es sich handelt
		/// </summary>
		public Currency Currency { get; }

		/// <summary>
		/// Die Währung, auf die sich der Kurs bezieht
		/// </summary>
		public Currency SourceCurrency { get; }

		/// <summary>
		/// Der Kurs der Währung (Multiplizierung mit diesem Kurs ergibt Currency als ergebnis)
		/// </summary>
		public decimal Rate	 { get; set; }

		/// <summary>
		/// Der Kurs der Währung (Multiplizierung mit diesem Kurs ergibt SourceCurrency als ergebnis)
		/// </summary>
		public decimal SourceRate => 1/Rate;

        /// <summary>
		/// Initializes a new instance of the <see cref="CurrencyImportInformation"/> class.
		/// </summary>
		public CurrencyImportInformation(DateTime dateTime, decimal rate, Currency currency, Currency sourceCurrency)
		{
			Date = new Date(dateTime);
			Rate = rate;
			Currency = currency;
			SourceCurrency = sourceCurrency;
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="CurrencyImportInformation"/> class.
		/// </summary>
		public CurrencyImportInformation(DateRange range, decimal rate, Currency currency, Currency sourceCurrency)
		{
			DateRange = range;
			Rate = rate;
			Currency = currency;
			SourceCurrency = sourceCurrency;
		}

	}
}