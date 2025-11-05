using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nextended.Core.Tests
{
    [TestClass]
    public class MimeTypeTests
    {
        [TestMethod]
        public void ContainesArchives()
        {
            var types = MimeType.ArchiveTypes.ToList();
            Assert.IsTrue(types.Contains("application/zip"));
        }

    }
}