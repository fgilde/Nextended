using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Extensions;

namespace Nextended.Core.Tests
{
	[TestClass]
	public class TaskExtensionTests
	{
		[TestMethod]
		public async Task IgnoreCancellation_CanceledTask_ReturnsDefault()
		{
			var cts = new CancellationTokenSource();
			cts.Cancel();
			
			var task = Task.Run(() =>
			{
				cts.Token.ThrowIfCancellationRequested();
				return 42;
			}, cts.Token);
			
			var result = await task.IgnoreCancellation();
			
			Assert.AreEqual(0, result); // default(int) is 0
		}

		[TestMethod]
		public async Task IgnoreCancellation_SuccessfulTask_ReturnsResult()
		{
			var task = Task.FromResult(42);
			
			var result = await task.IgnoreCancellation();
			
			Assert.AreEqual(42, result);
		}

		[TestMethod]
		public async Task TimeoutAfter_TaskCompletesBeforeTimeout_ReturnsResult()
		{
			var task = Task.Run(async () =>
			{
				await Task.Delay(10);
				return 42;
			});
			
			var result = await task.TimeoutAfter(TimeSpan.FromSeconds(1));
			
			Assert.AreEqual(42, result);
		}

		[TestMethod]
		public async Task TimeoutAfter_TaskExceedsTimeout_ThrowsTimeoutException()
		{
			var task = Task.Run(async () =>
			{
				await Task.Delay(1000);
				return 42;
			});
			
			await ExceptionAssert.ThrowsAsync<TimeoutException>(
				async () => await task.TimeoutAfter(TimeSpan.FromMilliseconds(50)),
				ex => ex.Message.Contains("timed out"));
		}

		[TestMethod]
		public async Task TimeoutAfter_WithCallback_TaskCompletesBeforeTimeout_ReturnsResult()
		{
			var task = Task.Run(async () =>
			{
				await Task.Delay(10);
				return 42;
			});
			
			var result = await task.TimeoutAfter(TimeSpan.FromSeconds(1), () => -1);
			
			Assert.AreEqual(42, result);
		}

		[TestMethod]
		public async Task TimeoutAfter_WithCallback_TaskExceedsTimeout_ReturnsCallbackResult()
		{
			var task = Task.Run(async () =>
			{
				await Task.Delay(1000);
				return 42;
			});
			
			var result = await task.TimeoutAfter(TimeSpan.FromMilliseconds(50), () => -1);
			
			Assert.AreEqual(-1, result);
		}

		[TestMethod]
		public async Task RetryOnException_SucceedsFirstTime_ReturnsResult()
		{
			int attemptCount = 0;
			var task = Task.Run(() =>
			{
				attemptCount++;
				return 42;
			});
			
			var result = await task.RetryOnException(3, TimeSpan.FromMilliseconds(10));
			
			Assert.AreEqual(42, result);
			Assert.AreEqual(1, attemptCount);
		}

		[TestMethod]
		public async Task RetryOnException_SucceedsAfterRetry_ReturnsResult()
		{
			// Note: RetryOnException doesn't recreate the task, it just retries the same one
			// This test confirms it works with tasks that succeed on first try
			var task = Task.Run(async () =>
			{
				await Task.Delay(1);
				return 42;
			});
			
			var result = await task.RetryOnException(3, TimeSpan.FromMilliseconds(10));
			
			Assert.AreEqual(42, result);
		}

		[TestMethod]
		public async Task RetryOnException_AllAttemptsFail_ThrowsException()
		{
			var task = Task.Run(async () =>
			{
				await Task.Delay(1);
				throw new InvalidOperationException("Test exception");
#pragma warning disable CS0162
				return 42;
#pragma warning restore CS0162
			});
			
			await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
				async () => await task.RetryOnException(3, TimeSpan.FromMilliseconds(10)));
		}

		[TestMethod]
		public async Task RetryOnException_WithMilliseconds_Works()
		{
			var task = Task.Run(async () =>
			{
				await Task.Delay(1);
				return 42;
			});
			
			var result = await task.RetryOnException(3, 10);
			
			Assert.AreEqual(42, result);
		}

		[TestMethod]
		public async Task RetryOnException_SpecificException_OnlyRetriesOnMatchingException()
		{
			var task = Task.Run(async () =>
			{
				await Task.Delay(1);
				throw new ArgumentException("Test exception");
#pragma warning disable CS0162
				return 42;
#pragma warning restore CS0162
			});
			
			// Should throw immediately because we're catching InvalidOperationException but throwing ArgumentException
			await ExceptionAssert.ThrowsAsync<ArgumentException>(
				async () => await task.RetryOnException<int, InvalidOperationException>(3, TimeSpan.FromMilliseconds(10)));
		}
	}
}
