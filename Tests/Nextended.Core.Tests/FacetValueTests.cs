using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Facets;

namespace Nextended.Core.Tests
{
    [TestClass]
    public class FacetValueTests
    {
        [TestMethod]
        public void Bools_read_back_as_numbers_become_bools()
        {
            Assert.AreEqual(true, FacetBuilder.Coerce("1", typeof(bool)));
            Assert.AreEqual(false, FacetBuilder.Coerce(0L, typeof(bool?)));
            Assert.AreEqual(true, FacetBuilder.Coerce("True", typeof(bool)));
        }

        [TestMethod]
        public void Other_values_convert_or_stay_text()
        {
            var id = Guid.NewGuid();
            Assert.AreEqual(id, FacetBuilder.Coerce(id.ToString(), typeof(Guid)));
            Assert.AreEqual(42, FacetBuilder.Coerce(42L, typeof(int)));
            Assert.AreEqual("abc", FacetBuilder.Coerce("abc", typeof(int)));
            Assert.IsNull(FacetBuilder.Coerce(null, typeof(bool)));
        }
    }
}
