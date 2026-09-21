using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nextended.Core.Tests
{
	public static class ExceptionAssert
	{
		public static void Throws<TException>(Action action)
			where TException : Exception
		{
			Throws<TException>(action, exception => true);
		}

		public static void Throws<TException>(Action action, Func<TException, bool> condition)
			where TException : Exception
		{
			try
			{
				action();
				Assert.Fail($"No exception was thrown. But exception of type '{typeof(TException)}' was expected.");
			}
			catch (TException exception)
			{
				Assert.IsTrue(exception.GetType() == typeof(TException),
							$"Exception of type '{exception.GetType()}' was thrown. But exception of type '{typeof(TException)}' was expected.");

				Assert.IsTrue(condition(exception), "Exception condition failed");
			}
			catch (AssertFailedException)
			{
				throw;
			}
			catch (Exception exception)
			{
				Assert.Fail($"Exception of type '{exception.GetType()}' was thrown. But exception of type '{typeof(TException)}' was expected. Stacktrace: {exception.StackTrace}");
			}
		}

		public static async Task ThrowsAsync<TException>(Func<Task> action)
			where TException : Exception
		{
			await ThrowsAsync<TException>(action, exception => true);
		}

		public static async Task ThrowsAsync<TException>(Func<Task> action, Func<TException, bool> condition)
			where TException : Exception
		{
			try
			{
				await action();
				Assert.Fail($"No exception was thrown. But exception of type '{typeof(TException)}' was expected.");
			}
			catch (TException exception)
			{
				Assert.IsTrue(exception.GetType() == typeof(TException),
							$"Exception of type '{exception.GetType()}' was thrown. But exception of type '{typeof(TException)}' was expected.");

				Assert.IsTrue(condition(exception), "Exception condition failed");
			}
			catch (AssertFailedException)
			{
				throw;
			}
			catch (Exception exception)
			{
				Assert.Fail($"Exception of type '{exception.GetType()}' was thrown. But exception of type '{typeof(TException)}' was expected. Stacktrace: {exception.StackTrace}");
			}
		}
	}
}