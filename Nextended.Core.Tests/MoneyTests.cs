using System;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Types;

namespace Nextended.Core.Tests
{
    [TestClass]
    public class MoneyTests
    {

        [TestMethod]
        public void CanParseMoneyStrings()
        {
            bool exceptionRaised1 = false;
            bool exceptionRaised2 = false;
            var refAmount = new Money(new decimal(1026.62));
            var eurGermanStr = "1026,62 €";
            var eurGerman2Str = "1.026,62 €";
            var usDollarStr = "$ 1,026.62";
            var usDollarStr2 = "1,026.62 USD";
            var noCurrencyStr = "1,026.62";

            var m1 = Money.Parse(eurGermanStr);
            var m2 = Money.Parse(eurGerman2Str);
            var m2explicit = Money.Parse(eurGerman2Str, new CultureInfo("de-DE"));
            var noCurrencyInUs = Money.Parse(noCurrencyStr, new CultureInfo("en-US"));

            try
            {
                var noCurrencyInDE2 = Money.Parse(usDollarStr, new CultureInfo("de-DE"));
            }
            catch (Exception e) { exceptionRaised1 = true; }

            try
            {
                var noCurrencyInDE = Money.Parse(noCurrencyStr, new CultureInfo("de-DE"));
            }
            catch (Exception e) { exceptionRaised2 = true; }

            var m3 = Money.Parse(usDollarStr);
            var m4 = Money.Parse(usDollarStr2);

            Assert.IsTrue(exceptionRaised1);
            Assert.IsTrue(exceptionRaised2);
            Assert.AreEqual(null, noCurrencyInUs.Currency);
            Assert.AreEqual(refAmount.Amount, m1.Amount);
            Assert.AreEqual(refAmount.Amount, noCurrencyInUs.Amount);
            Assert.AreEqual(refAmount.Amount, m2.Amount);
            Assert.AreEqual(refAmount.Amount, m3.Amount);
            Assert.AreEqual(refAmount.Amount, m2explicit.Amount);
            Assert.AreEqual(refAmount.Amount, m4.Amount);
            Assert.AreEqual(Currency.Euro, m1.Currency);
            Assert.AreEqual(Currency.USD, m4.Currency);
            Assert.AreEqual(Currency.USD, m3.Currency);

        }

        [TestMethod]
        public void TestMoneyExchange()
        {
            Money money = 36.4; //ca 42,63 USD
            Money moneyInUsd = money.SetCurrency(Currency.Euro).ConvertCurrency(Currency.USD);
            Assert.IsTrue(moneyInUsd > money);            
        }

        [TestMethod]
        public void TestAUDMoneyExchange()
        {
            var audMoney = new Money((decimal) 36.4, Currency.AUD);
            Money eurMoney = audMoney.ConvertCurrency(Currency.Euro);
            Assert.IsTrue(eurMoney < 36);
            var inUsd = audMoney.ConvertCurrency(Currency.USD);
            var alsoMoneyInUsd = eurMoney.ConvertCurrency(Currency.USD);
            Assert.AreEqual(inUsd.Round(), alsoMoneyInUsd.Round());

        }

        [TestMethod]        
        public void TestMoneyExchangeEnsureException()
        {
            Money moneyWithoutCurrency = 33;
            var res = moneyWithoutCurrency.ConvertCurrency(Currency.USD);
        }

        [TestMethod]
        public void Calc()
        {
            Money euro100InEur = new Money(100, Currency.Euro);
            Money euro100InUsd = euro100InEur.ConvertCurrency(Currency.USD);
            Money money = 36.4;

            var x = money.SetCurrency(Currency.USD).ConvertCurrency(Currency.Euro);
            Money simpleRes = euro100InEur + 30;


            Assert.AreEqual(130, simpleRes);
            Assert.AreEqual(euro100InEur.Currency, simpleRes.Currency);


            Assert.IsTrue(euro100InUsd.Currency.IsoCode == "USD");
            Assert.IsTrue(euro100InUsd > 100);


            var combined = euro100InEur + euro100InUsd;
            Assert.IsTrue(combined.Currency.IsoCode == "EUR");
            Assert.IsTrue(combined == 200);

            var combinedM4 = combined * 4;
            Assert.IsTrue(combinedM4 == 800);

            var exceptionRaised = false;
            try
            {
                var combinedM5 = euro100InEur * euro100InUsd;
            }
            catch (Exception)
            {
                exceptionRaised = true;
            }
            Assert.IsTrue(exceptionRaised);

        }
    }
}