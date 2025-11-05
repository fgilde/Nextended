using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nextended.Core.Tests
{
	public class TestSingleton : SingletonBase<TestSingleton>
	{
		public int Value { get; set; }
	}

	public class AnotherTestSingleton : SingletonBase<AnotherTestSingleton>
	{
		public string Name { get; set; }
	}

	[TestClass]
	public class SingletonTests
	{
		[TestMethod]
		public void Singleton_Instance_ReturnsSameInstance()
		{
			var instance1 = TestSingleton.Instance;
			var instance2 = TestSingleton.Instance;
			
			Assert.AreSame(instance1, instance2);
		}

		[TestMethod]
		public void Singleton_Instance_IsNotNull()
		{
			var instance = TestSingleton.Instance;
			
			Assert.IsNotNull(instance);
		}

		[TestMethod]
		public void Singleton_ModifyingInstance_PersistsAcrossReferences()
		{
			var instance1 = TestSingleton.Instance;
			instance1.Value = 42;
			
			var instance2 = TestSingleton.Instance;
			
			Assert.AreEqual(42, instance2.Value);
		}

		[TestMethod]
		public void Singleton_DifferentTypes_HaveDifferentInstances()
		{
			var singleton1 = TestSingleton.Instance;
			var singleton2 = AnotherTestSingleton.Instance;
			
			Assert.AreNotSame(singleton1, singleton2);
		}

		[TestMethod]
		public void Singleton_IsNotificationObject()
		{
			var instance = TestSingleton.Instance;
			
			Assert.IsInstanceOfType(instance, typeof(NotificationObject));
		}

		[TestMethod]
		public void Singleton_SupportsPropertyChangedNotification()
		{
			var instance = TestSingleton.Instance;
			bool propertyChanged = false;
			
			instance.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(TestSingleton.Value))
					propertyChanged = true;
			};
			
			instance.Value = 100;
			instance.OnPropertyChanged(nameof(TestSingleton.Value));
			
			Assert.IsTrue(propertyChanged);
		}

		[TestMethod]
		public void Singleton_MultipleTypes_MaintainSeparateInstances()
		{
			var singleton1 = TestSingleton.Instance;
			singleton1.Value = 10;
			
			var singleton2 = AnotherTestSingleton.Instance;
			singleton2.Name = "Test";
			
			Assert.AreEqual(10, TestSingleton.Instance.Value);
			Assert.AreEqual("Test", AnotherTestSingleton.Instance.Name);
		}
	}
}
