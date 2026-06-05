using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nextended.Core.Tests
{
	[TestClass]
	public class WaiterTests
	{
		[TestMethod]
		public async Task WaitForTrueAsync_ImmediatelyTrue_ReturnsQuickly()
		{
			var stopwatch = Stopwatch.StartNew();
			
			await Waiter.WaitForTrueAsync(() => true);
			
			stopwatch.Stop();
			Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100);
		}

		[TestMethod]
		public async Task WaitForTrueAsync_BecomesTrue_WaitsUntilTrue()
		{
			bool condition = false;
			var stopwatch = Stopwatch.StartNew();
			
			// Set condition to true after 50ms
			var _ = Task.Run(async () =>
			{
				await Task.Delay(50);
				condition = true;
			});
			
			await Waiter.WaitForTrueAsync(() => condition);
			
			stopwatch.Stop();
			Assert.IsTrue(condition);
			Assert.IsTrue(stopwatch.ElapsedMilliseconds >= 40); // Allow some tolerance
		}

		[TestMethod]
		public async Task WaitForResultAsync_ImmediatelyHasResult_ReturnsQuickly()
		{
			var stopwatch = Stopwatch.StartNew();
			
			var result = await Waiter.WaitForResultAsync(() => "test");
			
			stopwatch.Stop();
			Assert.AreEqual("test", result);
			Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100);
		}

		[TestMethod]
		public async Task WaitForResultAsync_EventuallyHasResult_WaitsAndReturnsResult()
		{
			string result = null;
			var stopwatch = Stopwatch.StartNew();
			
			// Set result after 50ms
			var _ = Task.Run(async () =>
			{
				await Task.Delay(50);
				result = "delayed result";
			});
			
			var finalResult = await Waiter.WaitForResultAsync(() => result);
			
			stopwatch.Stop();
			Assert.AreEqual("delayed result", finalResult);
			Assert.IsTrue(stopwatch.ElapsedMilliseconds >= 40);
		}

		[TestMethod]
		public async Task WaitForResultAsync_WithObject_WorksCorrectly()
		{
			object result = null;
			
			var _ = Task.Run(async () =>
			{
				await Task.Delay(30);
				result = new { Value = 42 };
			});
			
			var finalResult = await Waiter.WaitForResultAsync(() => result);
			
			Assert.IsNotNull(finalResult);
		}

		[TestMethod]
		public async Task WaitForTrueAsync_MultipleChecks_WaitsCorrectly()
		{
			int counter = 0;
			
			var _ = Task.Run(async () =>
			{
				await Task.Delay(10);
				counter = 1;
				await Task.Delay(10);
				counter = 2;
				await Task.Delay(10);
				counter = 3;
			});
			
			await Waiter.WaitForTrueAsync(() => counter >= 3);
			
			Assert.IsTrue(counter >= 3);
		}

		[TestMethod]
		public async Task WaitForResultAsync_WithComplexObject_WorksCorrectly()
		{
			TestObject result = null;
			
			var _ = Task.Run(async () =>
			{
				await Task.Delay(30);
				result = new TestObject { Id = 123, Name = "Test" };
			});
			
			var finalResult = await Waiter.WaitForResultAsync(() => result);
			
			Assert.IsNotNull(finalResult);
			Assert.AreEqual(123, finalResult.Id);
			Assert.AreEqual("Test", finalResult.Name);
		}

		private class TestObject
		{
			public int Id { get; set; }
			public string Name { get; set; }
		}
	}
}
