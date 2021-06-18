using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nextended.Core.Tests
{
	[TestClass]
	public class NotificationObjectTests
	{
		[TestMethod]
		public void TestNotificationRaised()
		{
			bool changedRaised = false;
			var user = new MyUserModel() {Name = "Florian Gilde", Age = 30};
			user.PropertyChanged += (sender, args) => changedRaised = true;
			user.Name = "Hannes";
			Assert.IsTrue(changedRaised);
			changedRaised = false;
			user.IsNotifying = false;
			user.Name = "Adam";
			Assert.IsFalse(changedRaised);
			user.IsNotifying = true;
			user.Name = "Peter";
			Assert.IsTrue(changedRaised);
		}

		[TestMethod]
		public void TestNotificationRaisedWithAutoNotificationObject()
		{
			bool changedRaised = false;
			var user = new MyAutomaticUserModel() { Name = "Florian Gilde", Age = 30 };
			user.PropertyChanged += (sender, args) => changedRaised = true;
			user.Name = "Hannes";
			Thread.Sleep(15);
			Assert.IsTrue(changedRaised);
			changedRaised = false;
			user.IsNotifying = false;
			user.Name = "Adam";
			Thread.Sleep(15);
			Assert.IsFalse(changedRaised);
			user.IsNotifying = true;
			user.Name = "Peter";
			Thread.Sleep(15);
			Assert.IsTrue(changedRaised);
		}

		[TestMethod]
		public void TestEditableImplementation()
		{
			var user = new MyUserModel() { Name = "Florian Gilde", Age = 30 };
			user.BeginEdit();
			user.Name = "Adam";
			Assert.AreEqual("Adam", user.Name);
			user.CancelEdit();
			Assert.AreEqual("Florian Gilde", user.Name);
			user.BeginEdit();
			user.Name = "Adam";
			Assert.AreEqual("Adam", user.Name);
			user.EndEdit();
			Assert.AreEqual("Adam", user.Name);
		}

	}

	class MyAutomaticUserModel : AutoEditableNotificationObject
	{
		public string Name { get; set; }
		public int Age { get; set; }
	}

	class MyUserModel : EditableNotificationObject
	{
		private string name;
		private int age;

		public int Age
		{
			get { return age; }
			set { SetProperty(ref age, value, () => Age); }
		}

		public string Name
		{
			get { return name; }
			set { SetProperty(ref name, value, () => Name); }
		}
	}
}