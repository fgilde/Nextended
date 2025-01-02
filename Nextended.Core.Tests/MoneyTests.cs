using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Types;

namespace Nextended.Core.Tests
{
    [TestClass]
    public class MoneyTests
    {
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