using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Extensions;

namespace Nextended.Core.Tests
{
	[TestClass]
	public class GuidTests
	{

        [TestMethod]
        public void CanFormatBack()
        {
            var teststr = "IdE4LgfmQU_B17r5K4R_kw";

            var guid = Guider.FromFormattedId(teststr);
            Assert.AreEqual(Guid.Parse("2e38d121-e607-4f41-81d7-baf92b847e93"), guid);

        }

        [TestMethod]
		public void CanFormat()
        {
            Guid id = Guid.Parse("3CCB1FFD-8301-45BB-9357-2CB82A69DA82");

            var str = id.ToFormattedId();
            Assert.AreEqual("-R-LPAGDu0WTVyy4Kmnagg", str);
            var guid = Guider.FromFormattedId(str);
            Assert.AreEqual(id, guid);

        }

        [TestMethod]
        public void CanFormatRnd()
        {
            Guid id = Guid.NewGuid();

            var str = id.ToFormattedId();
            Assert.IsTrue(!string.IsNullOrEmpty(str));
            var guid = Guider.FromFormattedId(str);
            Assert.AreEqual(id, guid);

        }
    }
}