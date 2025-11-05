using Nextended.Core.Extensions;
using Nextended.Core.Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Nextended.Core.Types;

namespace Nextended.Core.Types
{
    /// <summary>
    /// Oberklasse für Geldbeträge
    /// </summary>
    [Serializable]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonJsonMoneyConverter))]
#if !NETSTANDARD
    [System.Text.Json.Serialization.JsonConverter(typeof(SystemJsonMoneyConverter))]
    public sealed class Money : IComparable, IParsable<Money>
#else
    public sealed class Money : IComparable
#endif
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

            if (targetCurrency == Currency)
                return this;

            currencyRateTargetDate = currencyRateTargetDate == default ? DateTime.Today : currencyRateTargetDate;
            var fromDate = currencyRateTargetDate.AddDays(-2);
            var infos = CurrencyExchangeRateImporter.GetCurrencyExchangeRateData(fromDate, currencyRateTargetDate,
                Currency);
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
            var culture = Currency?.Cultures?.FirstOrDefault() ?? CultureInfo.CurrentCulture;

            var nfi = (NumberFormatInfo)culture.NumberFormat.Clone();
            nfi.NumberDecimalDigits = Math.Min(GetScale(amount), DECIMALS);

            var number =Round().ToString("N", nfi);

            if (!string.IsNullOrEmpty(Currency?.Symbol))
                return $"{Currency.Symbol}{number}";

            if (!string.IsNullOrEmpty(Currency?.IsoCode))
                return $"{Currency.IsoCode} {number}";

            return number;
        }

        private static int GetScale(decimal d)
        {
            var bits = decimal.GetBits(d);
            return (bits[3] >> 16) & 0xFF;
        }

        /// <summary>
        /// Versucht, aus einem String einen Geldbetrag zu parsen.
        /// Zuerst wird anhand einer Liste bekannter Währungen nach einem Währungshinweis gesucht.
        /// Wird eine eindeutige Währung gefunden, so wird das Zeichen bzw. der Name entfernt und, falls keine Culture
        /// übergeben wurde, anhand eines (in der Währung hinterlegten) Standard-CultureCode eine Culture erstellt.
        /// Wird kein Währungshinweis gefunden, so wird versucht, direkt den Zahlenwert zu parsen.
        /// </summary>
        /// <param name="s">Beispiel: "€1,026.62", "1,026.62 €" etc.</param>
        /// <param name="culture">
        /// Optional: CultureInfo für das Parsen der Zahl.
        /// Falls null und eine Währung gefunden wurde, wird deren DefaultCulture (z. B. "en-US" oder "de-DE") genutzt.
        /// Andernfalls wird CultureInfo.CurrentCulture verwendet.
        /// </param>
        /// <returns>Ein Money-Objekt</returns>
        /// <exception cref="FormatException">
        /// Wird geworfen, wenn der String entweder mehrdeutige Währungshinweise enthält oder der numerische Teil nicht geparst werden kann.
        /// </exception>
        public static Money Parse(string s, IFormatProvider? culture = null)
        {
            var initialCultureWasProvided = culture != null;
            if (string.IsNullOrWhiteSpace(s))
                throw new ArgumentException("Der Eingabestring darf nicht leer sein.", nameof(s));

            s = s.Trim();

            // Liste bekannter Währungen (dieses Beispiel geht davon aus, dass Currency.All vorhanden ist)
            // Wir filtern hier gleich alle Währungen heraus, die wichtige Eigenschaften (IsoCode, Name, NativeName) haben.
            List<Currency> currencies = Currency.All
                .Where(c => !string.IsNullOrEmpty(c.IsoCode) &&
                            !string.IsNullOrEmpty(c.Name) &&
                            !string.IsNullOrEmpty(c.NativeName))
                .ToList();

            // Suche nach Währungshinweisen im String (unabhängig von der Culture)
            // Wir sammeln alle Währungen, deren Symbol, Name, NativeName oder IsoCode im String vorkommt.
            List<Currency> foundCurrencies = new();
            string sUpper = s.ToUpperInvariant();
            foreach (Currency curr in currencies)
            {
                if ((!string.IsNullOrEmpty(curr.Symbol) && sUpper.Contains(curr.Symbol.ToUpperInvariant())) ||
                    (!string.IsNullOrEmpty(curr.Name) && sUpper.Contains(curr.Name.ToUpperInvariant())) ||
                    (!string.IsNullOrEmpty(curr.NativeName) && sUpper.Contains(curr.NativeName.ToUpperInvariant())) ||
                    (!string.IsNullOrEmpty(curr.IsoCode) && sUpper.Contains(curr.IsoCode.ToUpperInvariant())))
                {
                    foundCurrencies.Add(curr);
                }
            }

            Currency? currencyFound = null;
            if (foundCurrencies.Count >= 1)
            {
                currencyFound = foundCurrencies.First();
                if (currencyFound.Symbol == "$")
                    currencyFound = Currency.USD;

                if (!string.IsNullOrEmpty(currencyFound.Symbol))
                {
                    s = Regex.Replace(s, Regex.Escape(currencyFound.Symbol), "", RegexOptions.IgnoreCase);
                }

                if (!string.IsNullOrEmpty(currencyFound.Name))
                {
                    s = Regex.Replace(s, Regex.Escape(currencyFound.Name), "", RegexOptions.IgnoreCase);
                }

                if (!string.IsNullOrEmpty(currencyFound.NativeName))
                {
                    s = Regex.Replace(s, Regex.Escape(currencyFound.NativeName), "", RegexOptions.IgnoreCase);
                }

                if (!string.IsNullOrEmpty(currencyFound.IsoCode))
                {
                    s = Regex.Replace(s, Regex.Escape(currencyFound.IsoCode), "", RegexOptions.IgnoreCase);
                }

                s = s.Trim();
            }

            culture ??= currencyFound.Cultures?.FirstOrDefault() ?? CultureInfo.CurrentCulture;

            if (!decimal.TryParse(s, NumberStyles.Currency, culture, out decimal value))
            {
                if (currencyFound != null && !initialCultureWasProvided)
                {
                    foreach (var cultureInfo in currencyFound.Cultures)
                    {
                        if (decimal.TryParse(s, NumberStyles.Currency, cultureInfo, out decimal r))
                        {
                            return new Money(r, currencyFound);
                        }
                    }
                }

                throw new FormatException(
                    $"Der numerische Anteil '{s}' konnte nicht mit Culture '{culture}' geparst werden.");
            }

            return new Money(value, currencyFound);
        }

        /// <summary>
        /// Parses a string to a Money object or returns null if the string is not parsable.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static Money? ParseOrNull(string s, IFormatProvider? culture = null) => TryParse(s, culture, out var money) ? money : null;


        public static bool TryParse(string s, out Money money) => TryParse(s, null, out money);

        public static bool TryParse(string s, IFormatProvider? culture,  out Money money)
        {
            try
            {
                money = Parse(s, culture);
                return true;
            }
            catch (Exception)
            {
                money = null;
                return false;
            }
        }

    }
}

#if !NETSTANDARD
public class SystemJsonMoneyConverter : System.Text.Json.Serialization.JsonConverter<Money>
{
    public override Money Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        var s = reader.GetString();
        return Money.Parse(s);
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, Money value, System.Text.Json.JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString()); 
    }
}
#endif

public sealed class NewtonJsonMoneyConverter : Newtonsoft.Json.JsonConverter<Money>
{
    public override void WriteJson(Newtonsoft.Json.JsonWriter writer, Money value, Newtonsoft.Json.JsonSerializer serializer)
    {
        if (value is null) { writer.WriteNull(); return; }
        writer.WriteValue(value.ToString());
    }

    public override Money ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, Money existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
    {
        if (reader.TokenType == Newtonsoft.Json.JsonToken.Null) return null;

        if (reader.TokenType == Newtonsoft.Json.JsonToken.String)
            return Money.Parse((string)reader.Value);

        return reader.TokenType is Newtonsoft.Json.JsonToken.Integer or Newtonsoft.Json.JsonToken.Float ? new Money(Convert.ToDecimal(reader.Value)) : throw new Newtonsoft.Json.JsonSerializationException($"Unexpected token {reader.TokenType} when parsing Money.");
    }
}