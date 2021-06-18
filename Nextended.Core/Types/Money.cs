using System;

namespace Nextended.Core.Types
{
	/// <summary>
	/// Oberklasse für Geldbeträge
	/// </summary>
	[Serializable]
	public sealed class Money : IComparable
	{
		/// <summary>
		/// Nullobject für 0.0
		/// </summary>
		public static readonly Money Zero = new Money(decimal.Zero);

		private readonly decimal amount;
		/// <summary>
		/// Rundungsgenauigkeit von Round()
		/// </summary>
		public const int DECIMALS = 4;

		#region Konstruktoren

		/// <summary>
		/// Konstruktor mit Betrag (nimmt Standart-Währung)
		/// </summary>
		/// <param name="d"></param>
		public Money(decimal d)
		{
			amount = d;
		}
		#endregion

		#region Implizite Konvertierungen
		/// <summary>
		/// Implizite Konvertierung, durch die folgendes möglich ist:
		/// <code>
		/// int i = 42;
		/// Money money = i;
		/// </code>
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public static implicit operator Money(int i)
		{
			if (i == 0)
				return Zero;
			return new Money(i);
		}

		/// <summary>
		/// Implizite Konvertierung, durch die folgendes möglich ist:
		/// <code>
		/// Money money = 42.23m;
		/// </code>
		/// </summary>
		/// <param name="d"></param>
		/// <returns></returns>
		public static implicit operator Money(decimal d)
		{
			if (d == 0)
				return Zero;
			return new Money(d);
		}

		/// <summary>
		/// Implizite Konvertierung, durch die folgendes möglich ist:
		/// <code>
		/// Money money = 42.23;
		/// </code>
		/// </summary>
		/// <param name="d"></param>
		/// <returns></returns>
		public static implicit operator Money(double d)
		{
			if (d == 0)
				return Zero;
			return new Money((decimal)d);
		}

		/// <summary>
		/// Konvertiert Money -> decimal
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		public static implicit operator decimal(Money m)
		{
			return m.amount;
		}
		#endregion

		#region Explizite Konvertierungen
		/// <summary>
		/// Konvertiert Money -> double
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		public static explicit operator double(Money m)
		{
			return (double)m.amount;
		}
		#endregion

		#region Operatoren Plus und Minus
		/// <summary>
		/// + Operator zum Addieren
		/// </summary>
		/// <param name="m1"></param>
		/// <param name="m2"></param>
		/// <returns></returns>
		public static Money Add(Money m1, Money m2)
		{
			return new Money(m1.amount + m2.amount);
		}

		/// <summary>
		/// + Operator zum Addieren
		/// </summary>
		/// <param name="m1"></param>
		/// <param name="m2"></param>
		/// <returns></returns>
		public static Money operator +(Money m1, Money m2)
		{
			return Add(m1, m2);
		}

		/// <summary>
		/// - Negations-Operator
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		public static Money Negate(Money m)
		{
			return new Money(-m.amount);
		}

		/// <summary>
		/// - Negations-Operator
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		public static Money operator -(Money m)
		{
			return Negate(m);
		}

		/// <summary>
		/// - Operator zum Subtrahieren
		/// </summary>
		/// <param name="m1"></param>
		/// <param name="m2"></param>
		/// <returns></returns>
		public static Money Subtract(Money m1, Money m2)
		{
			return new Money(m1.amount - m2.amount);
		}

		/// <summary>
		/// - Operator zum Subtrahieren
		/// </summary>
		/// <param name="m1"></param>
		/// <param name="m2"></param>
		/// <returns></returns>
		public static Money operator -(Money m1, Money m2)
		{
			return Subtract(m1, m2);
		}
		#endregion

		#region Operator Multiplikation
		/// <summary>
		/// Multiplikation : Money mit decimal
		/// </summary>
		/// <param name="m"></param>
		/// <param name="d"></param>
		/// <returns></returns>
		public static Money Multiply(Money m, decimal d)
		{
			return new Money(m.amount * d);
		}

		/// <summary>
		/// Multiplikation : Money mit decimal
		/// </summary>
		/// <param name="m"></param>
		/// <param name="d"></param>
		/// <returns></returns>
		public static Money operator *(Money m, decimal d)
		{
			return Multiply(m, d);
		}

		/// <summary>
		/// Multiplikation : decimal mit Money
		/// </summary>
		/// <param name="d"></param>
		/// <param name="m"></param>
		/// <returns></returns>
		public static Money Multiply(decimal d, Money m)
		{
			return m * d;
		}

		/// <summary>
		/// Multiplikation : decimal mit Money
		/// </summary>
		/// <param name="d"></param>
		/// <param name="m"></param>
		/// <returns></returns>
		public static Money operator *(decimal d, Money m)
		{
			return Multiply(d, m);
		}

		/// <summary>
		/// Multiplikation : Money mit int
		/// </summary>
		/// <param name="m"></param>
		/// <param name="i"></param>
		/// <returns></returns>
		public static Money Multiply(Money m, int i)
		{
			return m * (decimal)i;
		}

		/// <summary>
		/// Multiplikation : Money mit int
		/// </summary>
		/// <param name="m"></param>
		/// <param name="i"></param>
		/// <returns></returns>
		public static Money operator *(Money m, int i)
		{
			return Multiply(m, i);
		}

		/// <summary>
		/// Multiplikation : int mit Money
		/// </summary>
		/// <param name="m"></param>
		/// <param name="i"></param>
		/// <returns></returns>
		public static Money Multiply(int i, Money m)
		{
			return m * (decimal)i;
		}

		/// <summary>
		/// Multiplikation : int mit Money
		/// </summary>
		/// <param name="m"></param>
		/// <param name="i"></param>
		/// <returns></returns>
		public static Money operator *(int i, Money m)
		{
			return Multiply(i, m);
		}

		/// <summary>
		/// Multiplikation : Money mit double
		/// </summary>
		/// <param name="m"></param>
		/// <param name="d"></param>
		/// <returns></returns>
		public static Money operator *(Money m, double d)
		{
			return new Money(Convert.ToDecimal(Convert.ToDouble(m.amount) * d));
		}

		/// <summary>
		/// Multiplikation : double mit Money
		/// </summary>
		/// <param name="d"></param>
		/// <param name="m"></param>
		/// <returns></returns>
		public static Money Multiply(double d, Money m)
		{
			return m * d;
		}

		/// <summary>
		/// Multiplikation : double mit Money
		/// </summary>
		/// <param name="d"></param>
		/// <param name="m"></param>
		/// <returns></returns>
		public static Money operator *(double d, Money m)
		{
			return Multiply(d, m);
		}

		/// <summary>
		/// Multiplikation von Money mit Money ergibt Exception!
		/// Denn : Wieviel ist 4 Euro mal 3 Euro ? Richtig: 12 Quadrateuro...
		/// </summary>
		/// <param name="m1"></param>
		/// <param name="m2"></param>
		/// <returns></returns>
		public static Money Multiply(Money m1, Money m2)
		{
			throw new InvalidOperationException("It is not pissoble to multiply money with money");
		}

		/// <summary>
		/// Multiplikation von Money mit Money ergibt Exception!
		/// Denn : Wieviel ist 4 Euro mal 3 Euro ? Richtig: 12 Quadrateuro...
		/// </summary>
		/// <param name="m1"></param>
		/// <param name="m2"></param>
		/// <returns></returns>
		public static Money operator *(Money m1, Money m2)
		{
			return Multiply(m1, m2);
		}
		#endregion

		#region Operator Division
		/// <summary>
		/// Division : Money mit decimal
		/// </summary>
		/// <param name="m"></param>
		/// <param name="d"></param>
		/// <returns></returns>
		public static Money Divide(Money m, decimal d)
		{
			return new Money(m.amount / d);
		}


		/// <summary>
		/// Division : Money mit decimal
		/// </summary>
		/// <param name="m"></param>
		/// <param name="d"></param>
		/// <returns></returns>
		public static Money operator /(Money m, decimal d)
		{
			return Divide(m, d);
		}

		/// <summary>
		/// Division : Money mit int
		/// </summary>
		/// <param name="m"></param>
		/// <param name="i"></param>
		/// <returns></returns>
		public static Money operator /(Money m, int i)
		{
			return Divide(m, (decimal)i);
		}

		/// <summary>
		/// Division : Money mit double
		/// </summary>
		/// <param name="m"></param>
		/// <param name="d"></param>
		/// <returns></returns>
		public static Money operator /(Money m, double d)
		{
			return new Money(Convert.ToDecimal(Convert.ToDouble(m.amount) / d));
		}

		/// <summary>
		/// Division von Money mit Money.
		/// </summary>
		/// <param name="m1"></param>
		/// <param name="m2"></param>
		/// <returns></returns>
		public static Quote operator /(Money m1, Money m2)
		{
			return new Quote((double)(m1.amount / m2.amount));
		}

		#endregion

		#region Vergleichsfunktionen
		/// <summary>
		/// Vergleicht mit dem angegebenen Objekt obj.
		/// 1) obj == null -> 1
		/// 2) obj nicht vom Type Money -> ArgumentException
		/// 3) sonst -> Money.Compare dieser und der angegebenen Money-Klasse.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public int CompareTo(object obj)
		{
            if (obj == null)
				return +1;

			if (obj is decimal)
				return amount.CompareTo(obj);

			var money = obj as Money;
			if (money == null)
				throw new ArgumentException("invalid object", "obj");

			return Compare(this, money);
		}

		/// <summary>
		/// Vergleicht zwei Money-Werte
		/// 1) beide null -> 0
		/// 2) m1 null -> -1
		/// 3) m2 null ->  1
		/// 4) sonst -> decimal.Compare
		/// </summary>
		/// <param name="m1"></param>
		/// <param name="m2"></param>
		/// <returns></returns>
		public static int Compare(Money m1, Money m2)
		{
			if (Equals(m1, null) && Equals(m2, null))
				return 0;
			if (Equals(m1, null))
				return -1;
			if (Equals(m2, null))
				return 1;

			if (ReferenceEquals(m1, m2))
				return 0;

			return m1.amount.CompareTo(m2.amount);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="m1"></param>
		/// <param name="m2"></param>
		/// <returns></returns>
		public static bool operator <(Money m1, Money m2)
		{
			return Compare(m1, m2) < 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="m1"></param>
		/// <param name="m2"></param>
		/// <returns></returns>
		public static bool operator >(Money m1, Money m2)
		{
			return Compare(m1, m2) > 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="m1"></param>
		/// <param name="m2"></param>
		/// <returns></returns>
		public static bool operator ==(Money m1, Money m2)
		{
			return Compare(m1, m2) == 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="m1"></param>
		/// <param name="m2"></param>
		/// <returns></returns>
		public static bool operator >=(Money m1, Money m2)
		{
			return Compare(m1, m2) >= 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="m1"></param>
		/// <param name="m2"></param>
		/// <returns></returns>
		public static bool operator <=(Money m1, Money m2)
		{
			return Compare(m1, m2) <= 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="m1"></param>
		/// <param name="m2"></param>
		/// <returns></returns>
		public static bool operator !=(Money m1, Money m2)
		{
			return Compare(m1, m2) != 0;
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
		#endregion


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return amount.GetHashCode();
		}

		/// <summary>
		/// Rundet diesen Money auf die angegeben Anzahl an Nachkommastellen.
		/// </summary>
		/// <returns></returns>
		public decimal Round()
		{
			var decimalFactor = (decimal)Math.Pow(10, DECIMALS);
			// c# Codebook algorithm
			return Decimal.Truncate((amount * decimalFactor + (0.5M * Math.Sign(amount)))) / decimalFactor;
		}

		/// <summary>
		/// Ist 0 
		/// </summary>
		public bool IsZero
		{
			get { return amount == Zero.amount; }
		}

		/// <summary>
		/// Positiver Betrag
		/// </summary>
		public bool IsPositive
		{
			get { return amount >= Zero.amount; }
		}

		/// <summary>
		/// Negativer Betrag
		/// </summary>
		public bool IsNegative
		{
			get
			{
				return amount < Zero.amount;
			}
		}

		/// <summary>
		/// Gibt an, ob die Anzahl der Nachkommastellen gültig ist
		/// </summary>
		/// <returns></returns>
		public bool HasValidNumberOfDecimalPlaces()
		{
			return BitConverter.GetBytes(decimal.GetBits(amount)[3])[2] <= DECIMALS;
		}

		/// <summary>
		/// Gibt diesen Geldbetrag als Zahl ohne Währung formatiert zurück.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			// TODO: Eigene Art definieren, wie Geldbeträge (TB/HG 2005-01-28)
			// ohne Währungssymbol formatiert angezeigt werden sollen.
			// Wir haben jetzt keine CultureInfo mehr.

			decimal roundedValue = Round();
			return roundedValue.ToString("N");
		}

	}
}