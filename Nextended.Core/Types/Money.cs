using System;
using System.Globalization;
using System.Linq;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;

namespace Nextended.Core.Types
{
	/// <summary>
	/// Oberklasse für Geldbeträge
	/// </summary>
	[Serializable]
	public sealed class Money : IComparable
	{

        /// <summary>
        /// Betrag
        /// </summary>
        public decimal Amount => amount;

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
		public Money(decimal d, Currency currency = null)
		{
			amount = d;
            Currency = currency;
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
            var money = m2.EnsureSameCurrencyAs(m1);
            return new Money(m1.amount + money.amount, m1.Currency);
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
			return new Money(-m.amount, m.Currency);
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
			return new Money(m1.amount - m2.EnsureSameCurrencyAs(m1).amount, m1.Currency);
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
			return new Money(m.amount * d).SetCurrency(m.Currency);
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
			return (m * d).SetCurrency(m.Currency);
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
			return (m * (decimal)i).SetCurrency(m.Currency);
		}

		/// <summary>
		/// Multiplikation : Money mit int
		/// </summary>
		/// <param name="m"></param>
		/// <param name="i"></param>
		/// <returns></returns>
		public static Money operator *(Money m, int i)
		{
			return Multiply(m, i).SetCurrency(m.Currency);
		}

		/// <summary>
		/// Multiplikation : int mit Money
		/// </summary>
		/// <param name="m"></param>
		/// <param name="i"></param>
		/// <returns></returns>
		public static Money Multiply(int i, Money m)
		{
			return (m * (decimal)i).SetCurrency(m.Currency);
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
			return new Money(Convert.ToDecimal(Convert.ToDouble(m.amount) * d)).SetCurrency(m.Currency);
		}

		/// <summary>
		/// Multiplikation : double mit Money
		/// </summary>
		/// <param name="d"></param>
		/// <param name="m"></param>
		/// <returns></returns>
		public static Money Multiply(double d, Money m)
		{
			return (m * d).SetCurrency(m.Currency);
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
			return new Money(m.amount / d).SetCurrency(m.Currency);
		}


		/// <summary>
		/// Division : Money mit decimal
		/// </summary>
		/// <param name="m"></param>
		/// <param name="d"></param>
		/// <returns></returns>
		public static Money operator /(Money m, decimal d)
		{
			return Divide(m, d).SetCurrency(m.Currency);
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
			return new Money(Convert.ToDecimal(Convert.ToDouble(m.amount) / d)).SetCurrency(m.Currency);
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
        /// Currency for this
        /// </summary>
        public Currency Currency { get; set; }

        public Money SetCurrency(Currency currency) => this.SetProperties(m => m.Currency = currency);

        public Money ConvertCurrency(Currency targetCurrency, DateTime currencyRateTargetDate = default)
        {
			if (targetCurrency == null || Currency == null)
			{
                //throw new ArgumentNullException(Currency == null ? nameof(Currency) : nameof(targetCurrency));
                // TODO: Currently we cant throw an exception because of calculation (5 Euro +2) is 7 euro but 2 didnt have a currency
                return SetCurrency(null);
            }
			if(targetCurrency == Currency)
                return this;

            currencyRateTargetDate = currencyRateTargetDate == default ? DateTime.Today : currencyRateTargetDate;
            var fromDate = currencyRateTargetDate.AddDays(-2);
            var infos = CurrencyExchangeRateImporter.GetCurrencyExchangeRateData(fromDate, currencyRateTargetDate, Currency);
            var rateInfo = infos.FirstOrDefault(information => information.Currency == targetCurrency);
			
            return new Money(amount * rateInfo.Rate).SetCurrency(targetCurrency);
        }

        public Money EnsureSameCurrencyAs(Money other)
        {
            return Currency != other.Currency ? ConvertCurrency(other.Currency) : this;
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
		public bool IsZero => amount == Zero.amount;

        /// <summary>
		/// Positiver Betrag
		/// </summary>
		public bool IsPositive => amount >= Zero.amount;

        /// <summary>
		/// Negativer Betrag
		/// </summary>
		public bool IsNegative => amount < Zero.amount;

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


        /// <summary>
        /// Parst einen String, der einen Geldbetrag mit eventuell angehängtem oder vorangestelltem Währungssymbol enthält.
        /// Die CultureInfo beeinflusst dabei ausschließlich das Parsing des Zahlenwertes (Trennzeichen, etc.).
        /// </summary>
        /// <param name="s">Der Eingabestring, z. B. "€1,026.62" oder "1,026.62 €".</param>
        /// <param name="culture">Die CultureInfo, die für das Parsen der Zahl genutzt werden soll.
        /// Ist null, wird CultureInfo.CurrentCulture verwendet.</param>
        /// <returns>Ein Money-Objekt mit Value und CurrencySymbol.</returns>
        public static Money Parse(string s, CultureInfo? culture = null)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            culture ??= CultureInfo.CurrentCulture;
            s = s.Trim();
            string currencySymbol = "";

            // Prüfen, ob das Währungssymbol am Anfang steht.
            if (s.Length > 0 && !IsNumericCharacter(s[0], culture))
            {
                int i = 0;
                while (i < s.Length && !IsNumericCharacter(s[i], culture))
                {
                    currencySymbol += s[i];
                    i++;
                }
                s = s.Substring(i).Trim();
            }

            // Falls noch kein Währungssymbol gefunden wurde, prüfen wir, ob eines am Ende steht.
            if (s.Length > 0 && !IsNumericCharacter(s[s.Length - 1], culture))
            {
                int j = s.Length - 1;
                string endSymbol = "";
                while (j >= 0 && !IsNumericCharacter(s[j], culture))
                {
                    // Da wir rückwärts iterieren, fügen wir vorne an, um die ursprüngliche Reihenfolge zu erhalten.
                    endSymbol = s[j] + endSymbol;
                    j--;
                }
                // Falls oben schon ein Symbol gefunden wurde, könntest du optional prüfen, ob es stimmt.
                if (string.IsNullOrEmpty(currencySymbol))
                {
                    currencySymbol = endSymbol;
                }
                s = s.Substring(0, j + 1).Trim();
            }

            // Jetzt bleibt s nur noch der Zahlen-Teil übrig.
            // Wir erlauben hier Tausendertrennzeichen, Dezimalpunkt, Vorzeichen und auch negative Zahlen in Klammern.
            decimal value;
            if (!decimal.TryParse(s, NumberStyles.Number | NumberStyles.AllowParentheses, culture, out value))
            {
                throw new FormatException($"Ungültiges Zahlenformat: {s}");
            }

            return new Money(value, Currency.Find(currencySymbol));
        }

        /// <summary>
        /// Prüft, ob ein Zeichen als Teil einer Zahl im Kontext der gegebenen Culture interpretiert werden könnte.
        /// Hierzu zählen Ziffern, Plus-/Minuszeichen, Klammern sowie das Dezimal- und Tausendertrennzeichen.
        /// </summary>
        private static bool IsNumericCharacter(char c, CultureInfo culture)
        {
            // Ziffern (0-9)
            if (char.IsDigit(c))
                return true;
            // Plus- und Minuszeichen
            if (c == '+' || c == '-')
                return true;
            // Klammern für negative Zahlen in Klammern
            if (c == '(' || c == ')')
                return true;
            // Dezimaltrennzeichen (in den meisten Kulturen ein einzelnes Zeichen)
            if (culture.NumberFormat.NumberDecimalSeparator.Length > 0 && c == culture.NumberFormat.NumberDecimalSeparator[0])
                return true;
            // Tausendertrennzeichen (auch hier wird in der Regel ein einzelnes Zeichen verwendet)
            if (!string.IsNullOrEmpty(culture.NumberFormat.NumberGroupSeparator) &&
                culture.NumberFormat.NumberGroupSeparator.Length > 0 &&
                c == culture.NumberFormat.NumberGroupSeparator[0])
                return true;

            return false;
        }

    }
}