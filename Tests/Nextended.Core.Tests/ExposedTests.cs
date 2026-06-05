using Microsoft.CSharp.RuntimeBinder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Extensions;

namespace Nextended.Core.Tests
{
	[TestClass]
	public class ExposedTests
	{
		[TestMethod]
		public void TestExposeAnObject()
		{
			var classInstance = new MyClassWithPrivateField();
			dynamic d = classInstance;
			ExceptionAssert.Throws<RuntimeBinderException>(() => d.value = 123);
			Assert.AreEqual(0, classInstance.GetValue());
			d = ExposedObject.From(classInstance);
			d.value = 123;
			Assert.AreEqual(123, classInstance.GetValue());
			var exposed = d as ExposedObject;
		}

		[TestMethod]
		public void TestExposeAnObjectWithExtensionUsing()
		{
			var classInstance = new MyClassWithPrivateField();
			classInstance.SetExposed(o => o.value = 1234);
			Assert.AreEqual(1234, classInstance.GetValue());
		}
	}

	class MyClassWithPrivateField
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Object"/> class.
		/// </summary>
		public MyClassWithPrivateField()
		{
			value = 0;
		}

		private int value;

		public int GetValue() // Used for easy testing
		{
			return value;
		}
	}
}