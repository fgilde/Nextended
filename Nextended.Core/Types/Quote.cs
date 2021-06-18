using System;
using System.Diagnostics;
using System.Globalization;

namespace Nextended.Core.Types
{
	/// <summary>
	/// Quoten
	/// </summary>
	[Serializable]
	[DebuggerDisplay("Quote = {amount*100}%")]
	public sealed class Quote : IComparable, IFormattable
	{
		private readonly double amount;

		private const double Epsilon = 0.0000000000001;


		/// <summary>
		/// Konstruktor
		/// </summary>
		/// <param name="value"></param>
		public Quote(double value)
		{
			amount = value;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="q"></param>
		/// <returns></returns>
		public static implicit operator double(Quote q)
		{
			return q.amount;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		public static implicit operator Quote(double m)
		{
			if (m == 0.0d)
				return Zero;
			if (m == 1.0d)
				return One;
			return new Quote(m);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public static implicit operator Quote(int i)
		{
			if (i == 0)
				return Zero;
			if (i == 1)
				return One;
			return new Quote(i);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="q"></param>
		/// <returns></returns>
		public static explicit operator decimal(Quote q)
		{
			return (decimal)q.amount;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="q"></param>
		/// <returns></returns>
		public static explicit operator int(Quote q)
		{
			return (int)q.amount;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="q1"></param>
		/// <param name="q2"></param>
		/// <returns></returns>
		public static bool operator <=(Quote q1, Quote q2)
		{
			return Compare(q1, q2) <= 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="q1"></param>
		/// <param name="q2"></param>
		/// <returns></returns>
		public static bool operator <(Quote q1, Quote q2)
		{
			return Compare(q1, q2) < 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="q1"></param>
		/// <param name="q2"></param>
		/// <returns></returns>
		public static bool operator >(Quote q1, Quote q2)
		{
			return Compare(q1, q2) > 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="q1"></param>
		/// <param name="q2"></param>
		/// <returns></returns>
		public static bool operator >=(Quote q1, Quote q2)
		{
			return Compare(q1, q2) >= 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="q1"></param>
		/// <param name="q2"></param>
		/// <returns></returns>
		public static bool operator ==(Quote q1, Quote q2)
		{
			return Compare(q1, q2) == 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="q1"></param>
		/// <param name="q2"></param>
		/// <returns></returns>
		public static bool operator !=(Quote q1, Quote q2)
		{
			return Compare(q1, q2) != 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="q"></param>
		/// <returns></returns>
		public static Quote operator -(Quote q)
		{
			return -q.amount;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			return (CompareTo(obj) == 0);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return amount.GetHashCode();
		}

		/// <summary>
		/// 
		/// </summary>/// <param name="q1"></param>
		/// <param name="q2"></param>
		/// <returns></returns>
		public static int Compare(Quote q1, Quote q2)
		{
			if (object.Equals(q1, null) && Equals(q2, null))
				return 0;
			if (Equals(q1, null))
				return -1;
			if (Equals(q2, null))
				return 1;

			if (ReferenceEquals(q1, q2))
				return 0;

			if (Math.Abs(q1.amount - q2.amount) < Epsilon)
				return 0;

			return q1.amount.CompareTo(q2.amount);
		}

		#region IComparable Member

		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public int CompareTo(object obj)
		{
			if (obj == null)
				return +1;

			if (obj is double)
				return amount.CompareTo(obj);

			var quote = obj as Quote;
			if (quote == null)
				throw new ArgumentException("invalid object", "obj");

			return Compare(this, quote);
		}

		#endregion

		#region IFormattable Member

		/// <summary>
		/// 
		/// </summary>
		/// <param name="format"></param>
		/// <param name="formatProvider"></param>
		/// <returns></returns>
		public string ToString(string format, IFormatProvider formatProvider)
		{
			return amount.ToString(format, formatProvider);
		}


		/// <summary>
		/// Wandlung in einen String
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return amount.ToString();
		}

		#endregion

		/// <summary>
		/// Parst ein Zahl
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static Quote Parse(string s)
		{
			return new Quote(double.Parse(s, CultureInfo.CurrentUICulture));
		}

		/// <summary>
		/// Nullobject für 0.0
		/// </summary>
		public static readonly Quote Zero = new Quote(0.0d);

		/// <summary>
		/// Nullobject für 1.0 = 100%
		/// </summary>
		public static readonly Quote One = new Quote(1.0d);
	}
}