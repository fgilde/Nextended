using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Helper;

namespace Nextended.Core.Tests
{
	[TestClass]
	public class PropertyWatcherTest
	{
		[TestMethod]
		public void TestAddAllProptertiesToWatchChanged()
		{
			Dictionary<string, object> changedValues = new Dictionary<string, object>();

			var person = new MyPerson {Name = "Florian Gilde", Age = 30};
			var watcher = new PropertyWatcher(person);
			watcher.PropertyChanged += (sender, args) => changedValues.Add(args.PropertyName, args.NewValue);
			watcher.AddAllProptertiesToWatch();
			watcher.StartWatching();
			Assert.IsTrue(watcher.IsWatching);
			person.Age = 21;
			person.Name = "Tobi";
			Thread.Sleep(15);
			Assert.IsTrue(changedValues.Count == 2);
			Assert.AreEqual(21, changedValues["Age"]);
			Assert.AreEqual("Tobi", changedValues["Name"]);
		}

		[TestMethod]
		public void TestOneProptertiesToWatchChanged()
		{
			Dictionary<string, object> changedValues = new Dictionary<string, object>();

			var person = new MyPerson() { Name = "Florian Gilde", Age = 30 };
			var watcher = new PropertyWatcher(person);
			watcher.PropertyChanged += (sender, args) => changedValues.Add(args.PropertyName, args.NewValue);
			watcher.AddPropertyToWatch(() => person.Age);
			watcher.StartWatching();
			Assert.IsTrue(watcher.IsWatching);
			person.Age = 21;
			person.Name = "Tobi";
			Thread.Sleep(15);
			Assert.IsTrue(changedValues.Count == 1);
			Assert.AreEqual(21, changedValues["Age"]);
		}

		[TestMethod]
		public void TestOneProptertiesToWatchChangedWithStop()
		{
			Dictionary<string, object> changedValues = new Dictionary<string, object>();

			var person = new MyPerson() { Name = "Florian Gilde", Age = 30 };
			var watcher = new PropertyWatcher(person);
			watcher.PropertyChanged += (sender, args) => changedValues.Add(args.PropertyName, args.NewValue);
			watcher.AddPropertyToWatch(() => person.Age);
			watcher.StartWatching();
			Assert.IsTrue(watcher.IsWatching);
			person.Age = 21;
			person.Name = "Tobi";
			Thread.Sleep(15);
			Assert.IsTrue(changedValues.Count == 1);
			Assert.AreEqual(21, changedValues["Age"]);
			changedValues.Clear();
			watcher.StopWatching();

			person.Age = 11;
			Thread.Sleep(15);
			Assert.IsTrue(changedValues.Count == 0);

			watcher.StartWatching();
			person.Age = 15;
			Thread.Sleep(15);
			Assert.IsTrue(changedValues.Count == 1);
			Assert.AreEqual(15, changedValues["Age"]);
		}
	}

	class MyPerson
	{
		public string Name { get; set; }
		public int Age { get; set; }
	}
}