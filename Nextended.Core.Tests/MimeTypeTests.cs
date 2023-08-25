using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Encode;
using Nextended.Core.Encryption;
using Nextended.Core.Hashing;
using Nextended.Core.Helper;

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