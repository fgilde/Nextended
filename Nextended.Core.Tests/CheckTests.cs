using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nextended.Core.Tests
{
	[TestClass]
	public class CheckTests
	{

		[TestMethod]
		public void RequiresThrowTest()
		{
			ExceptionAssert.Throws<ArgumentNullException>(() => Check.Requires<ArgumentNullException>(false, "Fehler 27"),
			exception => exception.Message.Contains("Fehler 27"));
		}

		[TestMethod]
		public void RequiresThrowTestFactory()
		{
			
			ExceptionAssert.Throws<FieldAccessException>(() => Check.Requires(false, () => new FieldAccessException("Feld kaputt")),
			exception => exception.Message.Contains("Feld kaputt"));
		}


		[TestMethod]
		public void MultipleParamNullCheck()
		{
			object reportId = new { Id = 21 };
			object endDate = new { Blah = 2 };
			object startDate = null;
			object someOther = new { Name = "Kalle" };
			ExceptionAssert.Throws<ArgumentNullException>(
				() => Check.NotNull(() => reportId, () => startDate, () => endDate, () => someOther),
				exception => exception.Message.Contains("startDate") && exception.Message.Contains("MultipleParamNullCheck"));

		}

		[TestMethod]
		public void SingleParamNullCheck()
		{
			object myObject = null;
			ExceptionAssert.Throws<ArgumentNullException>(
				() => Check.NotNull(() => myObject),
				exception => exception.Message.Contains("myObject") && exception.Message.Contains("SingleParamNullCheck"));

		}

		[TestMethod]
		public void CheckWithResultAssignException()
		{
			FileInfo fileInfo = null;
			ExceptionAssert.Throws<ArgumentNullException>(() =>
			{
				var myId = Check.NotNull(() => fileInfo);
			},
			exception => exception.Message.Contains("fileInfo") && exception.Message.Contains("CheckWithResultAssignException"));
		}

		[TestMethod]
		public void SimpleObjectCheckNull()
		{
			object id = new object();
			var myObj = Check.NotNull(() => id);
			Assert.IsTrue(ReferenceEquals(id, myObj));
		}


		[TestMethod]
		public void CheckWithResultAssign()
		{
			FileInfo fileInfo = new FileInfo("test.txt");
			var checkResultFileInfo = Check.NotNull(() => fileInfo);
			Assert.AreEqual(fileInfo, checkResultFileInfo);
			Assert.IsTrue(ReferenceEquals(fileInfo, checkResultFileInfo));
		}
	}
}