using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;

namespace Nextended.Core
{
	/// <summary>
	/// Static class to check certain preconditions
	/// </summary>
	public static class Check
	{
		private static readonly string argumentNullExceptionMessage = "Parameter '{0}' in Method '{1}' is null" + Environment.NewLine + "Method '{1}' was called from '{2}'";

        public static T ThrowIfNull<T>(this T input, string paramName)
        {
            return input.ThrowIfNull<ArgumentNullException, T>(paramName);
        }

        public static T ThrowIfNull<TException, T>(this T input, string message) where TException : Exception, new()
        {
            if (input.NotNull())
            {
                return input;
            }

            var exception = typeof(TException).CreateInstance<TException>(message);
            throw exception;
        }

        /// <summary>
		/// Checks if the condition <paramref name="condition"/> is met.
		/// </summary>
		/// <param name="condition">Condition that must be met, otherwise an exception is thrown</param>
		/// <param name="exceptionCreateFactory">Method to create the exception to be thrown when condition is false</param>
		public static void Requires(bool condition, Func<Exception> exceptionCreateFactory)
		{
			if (!condition)
				throw exceptionCreateFactory();
		}

		/// <summary>
		/// Checks if the condition <paramref name="condition"/> is met.
		/// If not, an exception of type <typeparamref name="TException"/> is thrown.
		/// </summary>
		/// <typeparam name="TException">Type of exception to be thrown</typeparam>
		/// <param name="condition">Condition that must be met, otherwise an exception is thrown</param>
		public static void Requires<TException>(bool condition)
			where TException : Exception, new()
		{
			if (!condition)
				throw new TException();
		}

		/// <summary>
		/// Checks if the condition <paramref name="condition" /> is met.
		/// If not, an exception of type <typeparamref name="TException" /> is thrown.
		/// </summary>
		/// <typeparam name="TException">Type of exception to be thrown</typeparam>
		/// <param name="condition">Condition that must be met, otherwise an exception is thrown</param>
		/// <param name="message">Error message</param>
		public static void Requires<TException>(bool condition, string message)
			where TException : Exception
		{
			if (!condition)
				throw (TException)Activator.CreateInstance(typeof(TException), message);
		}

		/// <summary>
		/// Checks if any of the <c>parameter</c> is null.
		/// </summary>
		/// <exception cref="System.ArgumentNullException">If <code>parameter</code> is null.</exception>
		public static void NotNull(Expression<Func<object>> expression1, Expression<Func<object>> expression2,
			params Expression<Func<object>>[] parameters)
		{
			foreach (var parameterName in from expression in parameters.Concat(new[] { expression1, expression2 })
										  let parameterToCheck = expression.Compile()()
										  where parameterToCheck == null
										  select expression.GetMemberName())
				throw new ArgumentNullException(parameterName, string.Format(argumentNullExceptionMessage, parameterName, ReflectionHelper.GetCallingMethod().Name, ReflectionHelper.GetCallingMethod(1).Name));
		}

		/// <summary>
		/// Checks if a <c>parameter</c> is null.
		/// </summary>
		/// <exception cref="System.ArgumentNullException">If <code>parameter</code> is null.</exception>
		/// <param name="parameter">Expression of the object to be checked e.g. ()=>name</param>
		public static T NotNull<T>(Expression<Func<T>> parameter)
		{
			var parameterToCheck = parameter.Compile()();
			if (parameterToCheck == null)
			{
				var parameterName = parameter.GetMemberName();
				throw new ArgumentNullException(parameterName, string.Format(argumentNullExceptionMessage, parameterName, ReflectionHelper.GetCallingMethod().Name, ReflectionHelper.GetCallingMethod(1).Name));
			}
			return parameterToCheck;
		}

		/// <summary>
		/// Checks if a <c>parameter</c> is null.
		/// </summary>
		/// <exception cref="System.ArgumentNullException">If <code>parameter</code> is null.</exception>
		/// <param name="parameter">The object to be checked</param>
		/// <param name="parameterName">Name of the parameter</param>
		public static void NotNull(object parameter, string parameterName)
		{
			if (parameter == null)
				throw new ArgumentNullException(parameterName, string.Format(argumentNullExceptionMessage, parameterName, ReflectionHelper.GetCallingMethod().Name, ReflectionHelper.GetCallingMethod(1).Name));
		}

		/// <summary>
		/// Checks if a <see cref="Guid"/> is empty
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <param name="parameterName">Name of the parameter.</param>
		public static void NotNull(Guid parameter, string parameterName)
		{
			if (parameter == Guid.Empty)
				throw new ArgumentNullException(parameterName, string.Format(argumentNullExceptionMessage, parameterName, ReflectionHelper.GetCallingMethod().Name, ReflectionHelper.GetCallingMethod(1).Name));
		}

		/// <summary>
		/// Checks if a <c>parameter</c> is null or empty.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <param name="parameterName">Name of the parameter.</param>
		public static void NotNullOrEmpty(string parameter, string parameterName)
		{
			if (string.IsNullOrEmpty(parameter))
				throw new ArgumentNullException(string.Format(argumentNullExceptionMessage, parameterName, ReflectionHelper.GetCallingMethod().Name, ReflectionHelper.GetCallingMethod(1).Name));
		}


		/// <summary>
		/// Checks if the operating system is at least Vista
		/// </summary>
		public static void IsVistaOrHigher()
		{
			if (!(Environment.OSVersion.Version.Major >= 6))
				throw new InvalidOperationException("Invalid OperatingSystem");
		}

		/// <summary>
		/// Executes the code block in a try-catch and throws the exception if necessary
		/// </summary>
		public static TResult? TryCatch<TResult, TException>(Func<TResult> block, Func<TException, Exception>? onException = null)
			where TException : Exception
		{
			try
			{
				return block();
			}
			catch (TException e)
			{
				if (onException != null)
					throw onException(e);
			}
			return default(TResult);
		}

		/// <summary>
		/// Executes the code block asynchronously in a try-catch and throws the exception if necessary
		/// </summary>
		/// <param name="block">The code block to execute</param>
		/// <param name="onException">Optional exception handler</param>
		/// <typeparam name="TResult">The result type</typeparam>
		/// <typeparam name="TException">The exception type to catch</typeparam>
		/// <returns>A task representing the asynchronous operation</returns>
		public static async Task TryCatchAsync<TResult, TException>(Func<TResult> block, Func<TException, Exception>? onException = null)
			where TException : Exception
		{
			await Task.Run(() => TryCatch(block, onException));
		}

		/// <summary>
		/// Executes the code block asynchronously in a try-catch and throws the exception if necessary
		/// </summary>
		/// <param name="block">The code block to execute</param>
		/// <param name="onException">Optional exception handler</param>
		/// <typeparam name="TException">The exception type to catch</typeparam>
		/// <returns>A task representing the asynchronous operation</returns>
		public static async Task TryCatchAsync<TException>(Action block, Func<TException, Exception>? onException = null)
			where TException : Exception
		{
			await Task.Run(() => TryCatch(block, onException));
		}

		/// <summary>
		/// Executes the task in a try-catch and throws the exception if necessary
		/// </summary>
		/// <param name="task">The task to execute</param>
		/// <param name="onException">Optional exception handler</param>
		/// <param name="cancellation">Cancellation token</param>
		/// <typeparam name="TResult">The result type</typeparam>
		/// <typeparam name="TException">The exception type to catch</typeparam>
		/// <returns>A task representing the asynchronous operation with the result</returns>
		/// <exception cref="Exception"></exception>
		public static async Task<TResult?> TryCatchAsync<TResult, TException>(Task<TResult> task,
			Func<TException, Exception>? onException = null, CancellationToken cancellation = default(CancellationToken))
			where TException : Exception
		{
			try
			{
				return await task;
			}
			catch (TException e)
			{
				if (onException != null)
					throw onException(e);
				return default(TResult);
			}
		}

		/// <summary>
		/// Executes the task in a try-catch and throws the exception if necessary
		/// </summary>
		/// <param name="block">The task to execute</param>
		/// <param name="onException">Optional exception handler</param>
		/// <typeparam name="TException">The exception type to catch</typeparam>
		/// <returns>A task representing the asynchronous operation</returns>
		/// <exception cref="Exception"></exception>
		public static async Task TryCatchAsync<TException>(Task block, Func<TException, Exception>? onException = null)
			where TException : Exception
		{
			try
			{
				await block;
			}
			catch (TException e)
			{
				if (onException != null)
					throw onException(e);
			}
		}

		/// <summary>
		/// Executes the code block in a try-catch and throws the exception if necessary
		/// </summary>
		public static void TryCatch<TException>(Action block, Func<TException, Exception>? onException = null)
			where TException : Exception
		{
			try
			{
				block();
			}
			catch (TException e)
			{
				if (onException != null)
					throw onException(e);
			}
		}
    }
}