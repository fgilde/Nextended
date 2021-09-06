using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Extensions;

namespace Nextended.Core.Tests
{
	[TestClass]
	public class StringExtensionTests
	{

		[TestMethod]
		public void CanEllipse()
        {
            var toEllips = "Hello this is my text";
            string res = toEllips.ToEllipsis(4).ToString();
            string res2 = toEllips.ToEllipsis(4, '*', true);
            Assert.AreEqual("H...", res);
            Assert.AreEqual("Hell*****************", res2);
        }
    }
}