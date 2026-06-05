using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Types;

namespace Nextended.Core.Tests
{
	// Concrete implementation for testing
	public class TestGuidId : BaseId<TestGuidId, Guid>
	{
		public TestGuidId(Guid id) : base(id) { }
	}

	public class TestIntId : BaseId<TestIntId, int>
	{
		public TestIntId(int id) : base(id) { }
	}

	public class TestStringId : BaseId<TestStringId, string>
	{
		public TestStringId(string id) : base(id) { }
	}

	[TestClass]
	public class BaseIdTests
	{
		[TestMethod]
		public void BaseId_Constructor_WithValidGuid_CreatesInstance()
		{
			var guid = Guid.NewGuid();
			var id = new TestGuidId(guid);
			
			Assert.AreEqual(guid, id.Id);
		}

		[TestMethod]
		public void BaseId_Constructor_WithEmptyGuid_ThrowsException()
		{
			ExceptionAssert.Throws<ArgumentException>(
				() => new TestGuidId(Guid.Empty),
				ex => ex.Message.Contains("GUID must not be EMPTY"));
		}

		[TestMethod]
		public void BaseId_Constructor_WithValidInt_CreatesInstance()
		{
			var id = new TestIntId(42);
			
			Assert.AreEqual(42, id.Id);
		}

		[TestMethod]
		public void BaseId_Constructor_WithZeroInt_CreatesInstance()
		{
			var id = new TestIntId(0);
			
			Assert.AreEqual(0, id.Id);
		}

		[TestMethod]
		public void BaseId_Constructor_WithNegativeInt_ThrowsException()
		{
			ExceptionAssert.Throws<ArgumentException>(
				() => new TestIntId(-1),
				ex => ex.Message.Contains("INT must be >= 0"));
		}

		[TestMethod]
		public void BaseId_Constructor_WithString_CreatesInstance()
		{
			var id = new TestStringId("test-id");
			
			Assert.AreEqual("test-id", id.Id);
		}

		[TestMethod]
		public void BaseId_ToString_ReturnsIdAsString()
		{
			var id = new TestIntId(123);
			
			Assert.AreEqual("123", id.ToString());
		}

		[TestMethod]
		public void BaseId_Equals_SameIds_ReturnsTrue()
		{
			var guid = Guid.NewGuid();
			var id1 = new TestGuidId(guid);
			var id2 = new TestGuidId(guid);
			
			Assert.IsTrue(id1.Equals(id2));
			Assert.IsTrue(id1 == id2);
		}

		[TestMethod]
		public void BaseId_Equals_DifferentIds_ReturnsFalse()
		{
			var id1 = new TestGuidId(Guid.NewGuid());
			var id2 = new TestGuidId(Guid.NewGuid());
			
			Assert.IsFalse(id1.Equals(id2));
			Assert.IsFalse(id1 == id2);
		}

		[TestMethod]
		public void BaseId_Equals_Null_ReturnsFalse()
		{
			var id = new TestIntId(42);
			
			Assert.IsFalse(id.Equals(null));
		}

		[TestMethod]
		public void BaseId_Equals_SameReference_ReturnsTrue()
		{
			var id = new TestIntId(42);
			
			Assert.IsTrue(id.Equals(id));
		}

		[TestMethod]
		public void BaseId_NotEquals_Operator_Works()
		{
			var id1 = new TestIntId(1);
			var id2 = new TestIntId(2);
			
			Assert.IsTrue(id1 != id2);
		}

		[TestMethod]
		public void BaseId_GetHashCode_SameIds_ReturnsSameHashCode()
		{
			var guid = Guid.NewGuid();
			var id1 = new TestGuidId(guid);
			var id2 = new TestGuidId(guid);
			
			Assert.AreEqual(id1.GetHashCode(), id2.GetHashCode());
		}

		[TestMethod]
		public void BaseId_ImplicitConversion_ToUnderlyingType_Works()
		{
			var id = new TestIntId(42);
			int value = id;
			
			Assert.AreEqual(42, value);
		}

		[TestMethod]
		public void BaseId_ImplicitConversion_WithGuid_Works()
		{
			var guid = Guid.NewGuid();
			var id = new TestGuidId(guid);
			Guid result = id;
			
			Assert.AreEqual(guid, result);
		}

		[TestMethod]
		public void BaseId_EqualityOperator_WithNull_Works()
		{
			TestIntId id = new TestIntId(1);
			TestIntId nullId = null;
			
			Assert.IsFalse(id == nullId);
			Assert.IsTrue(id != nullId);
		}

		[TestMethod]
		public void BaseId_EqualityOperator_BothNull_ReturnsTrue()
		{
			TestIntId id1 = null;
			TestIntId id2 = null;
			
			Assert.IsTrue(id1 == id2);
			Assert.IsFalse(id1 != id2);
		}

		[TestMethod]
		public void BaseId_IntIds_WithDifferentValues_AreNotEqual()
		{
			var id1 = new TestIntId(100);
			var id2 = new TestIntId(200);
			
			Assert.AreNotEqual(id1, id2);
			Assert.AreNotEqual(id1.GetHashCode(), id2.GetHashCode());
		}

		[TestMethod]
		public void BaseId_StringIds_WithSameValue_AreEqual()
		{
			var id1 = new TestStringId("abc");
			var id2 = new TestStringId("abc");
			
			Assert.AreEqual(id1, id2);
			Assert.IsTrue(id1 == id2);
		}

		[TestMethod]
		public void BaseId_Interface_ITypeBasedId_IsImplemented()
		{
			var id = new TestIntId(42);
			
			Assert.IsInstanceOfType(id, typeof(ITypeBasedId<int>));
			Assert.AreEqual(42, ((ITypeBasedId<int>)id).Id);
		}
	}
}
