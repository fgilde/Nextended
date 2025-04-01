using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml;
using Nextended.Core.Types;
using Nextended.Core.Types.Ranges;

namespace Nextended.Core.Helper;

public static class CurrencyExchangeRateImporter
{
    private static readonly Dictionary<string, List<CurrencyImportInformation>> cache = new();

    public static IEnumerable<CurrencyImportInformation> GetCurrencyExchangeRateData(DateTime fromDate,
        DateTime toDate,
        Currency sourceRateCurrency = null, bool returnAverageRate = false)
    {
        sourceRateCurrency ??= Currency.Euro;
        var cacheKey = $"{fromDate}-to-{toDate}-as-{sourceRateCurrency.IsoCode}-{returnAverageRate}";
        if (cache.ContainsKey(cacheKey))
            return cache[cacheKey];
        var res = GetCurrencyExchangeRateDataCore(fromDate, toDate, sourceRateCurrency, returnAverageRate).ToList();
        cache.Add(cacheKey, res);
        return res;
    }

    /// <summary>
    ///     Holt die Wechselkurse für den übergebenen Zeitbreich von der Europäischen Zentralbank ab
    /// </summary>
    /// <param name="fromDate">Startdatum</param>
    /// <param name="toDate">Enddatum</param>
    /// <param name="sourceRateCurrency">Währung, auf der sich der Kurs bezieht (Konzernwährung im CP.Cons fall)</param>
    /// <param name="returnAverageRate">
    ///     Wenn true, wird ein ergebnis zurückgegeben, in dem jede Währung nur einmal enthalten
    ///     ist, und als kurs der Durchschnittswert
    /// </param>
    /// <returns></returns>
    private static IEnumerable<CurrencyImportInformation> GetCurrencyExchangeRateDataCore(DateTime fromDate,
        DateTime toDate,
        Currency sourceRateCurrency = null, bool returnAverageRate = false)
    {
        var result = new List<CurrencyImportInformation>();
        fromDate = new DateTime(fromDate.Year, fromDate.Month, fromDate.Day, 0, 0, 0);
        toDate = new DateTime(toDate.Year, toDate.Month, toDate.Day, 23, 59, 59);

        if (fromDate < DateTime.Parse("01.01.1999"))
            throw new NotSupportedException("Minimum fromDate is 1999");
        var xmlUrl = "http://www.ecb.europa.eu/stats/eurofxref/eurofxref-hist.xml";

        var timeSpan = DateTime.Now - fromDate;
        if (timeSpan.Days <= 90)
            xmlUrl = "http://www.ecb.europa.eu/stats/eurofxref/eurofxref-hist-90d.xml";

        XmlTextReader xmlReader;

        try
        {
            xmlReader = new XmlTextReader(xmlUrl);
        }
        catch (WebException)
        {
            throw new WebException("Failed to Import CurrencyExchangeRates");
        }

        var date = DateTime.Now;

        while (xmlReader.Read())
            if (!string.IsNullOrEmpty(xmlReader.Name))
                for (var i = 0; i < xmlReader.AttributeCount; i++)
                    //Prüfen ob es den Knoten/Element 'Cube' gibt
                    if (xmlReader.Name == "Cube")
                    {
                        //Falls der Knoten/Element nur 1 Attribut enthält, ist dies das Datum
                        if (xmlReader.AttributeCount == 1)
                        {
                            //Datum auslesen
                            xmlReader.MoveToAttribute("time");
                            date = DateTime.Parse(xmlReader.Value);
                        }

                        //Sind 2 Attribute im aktuellen Knoten/UnterElement, enthält dieser WährungsKürzel und Kurswert
                        if (xmlReader.AttributeCount == 2)
                        {
                            //Währung auslesen
                            xmlReader.MoveToAttribute("currency");
                            var currency = xmlReader.Value;

                            //Kurs auslesen
                            xmlReader.MoveToAttribute("rate");
                            var targetRate =
                                decimal.Parse(xmlReader.Value.Replace(".",
                                    ",")); // Multiply to this Rate is Target currency
                            if (date <= toDate && date >= fromDate)
                                result.Add(new CurrencyImportInformation(date, targetRate, Currency.Find(currency),
                                    sourceRateCurrency));
                        }

                        xmlReader.MoveToNextAttribute();
                    }

        if (result.All(r => r.Currency != Currency.Euro))
            result.Add(new CurrencyImportInformation(date, 1.0m, Currency.Euro, sourceRateCurrency));

        // Wenn andere sourceRateCurrency als bei der ECB, dann umrechnen
        if (sourceRateCurrency != Currency.Euro)
        {
            var sourceRate = result.First(ci => ci.Currency == sourceRateCurrency).Rate;
            foreach (var information in result)
            {
                var oldRateAgainstEur = information.Rate; // 1 EUR => X
                // Wieviel EUR bekommt man für 1 sourceRateCurrency?
                var oneSourceInEur = 1 / sourceRate; // z.B. ~1.176 EUR/GBP
                // Also: 1 sourceRateCurrency => X' (Ziel) = (1.176) * (oldRateAgainstEur)
                information.Rate = oneSourceInEur * oldRateAgainstEur;
            }
        }

        // Durchschnitt errechnen
        if (returnAverageRate)
        {
            var averageResult = new List<CurrencyImportInformation>();
            var groupBy = result.GroupBy(information => information.Currency);
            foreach (IGrouping<string, CurrencyImportInformation> grouping in groupBy)
            {
                var averageRate =
                    grouping.Select(information => information.Rate).Aggregate((arg1, arg2) => arg1 + arg2) /
                    grouping.Count();
                var range = new DateRange(grouping.OrderBy(information => information.Date).First().Date,
                    grouping.OrderBy(information => information.Date).Last().Date);
                averageResult.Add(new CurrencyImportInformation(range, averageRate, Currency.Find(grouping.Key),
                    sourceRateCurrency));
            }

            return averageResult;
        }

        return result;
    }
}