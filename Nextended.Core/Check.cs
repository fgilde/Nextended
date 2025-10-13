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
	/// Statische Klasse um bestimmte vorraussetzungen zu Prüfen
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
		/// Prüft ob die Bedingung <paramref name="condition"/> erfüllt ist. 
		/// </summary>
		/// <param name="condition">Bedingung die erfüllt sein muss, sonst wird eine Ausnahme geworfen</param>
		/// <param name="exceptionCreateFactory">Methode um die exception zu erzeugen, die geworfen werden soll, wenn condition false ist</param>
		public static void Requires(bool condition, Func<Exception> exceptionCreateFactory)
		{
			if (!condition)
				throw exceptionCreateFactory();
		}

		/// <summary>
		/// Prüft ob die Bedingung <paramref name="condition"/> erfüllt ist. 
		/// Wenn nicht wird eine Ausnahme vom <typeparamref name="TException"/> geworfen.
		/// </summary>
		/// <typeparam name="TException">Typ der Ausnahme die geworfen werden soll</typeparam>
		/// <param name="condition">Bedingung die erfüllt sein muss, sonst wird eine Ausnahme geworfen</param>
		public static void Requires<TException>(bool condition)
			where TException : Exception, new()
		{
			if (!condition)
				throw new TException();
		}

		/// <summary>
		/// Prüft ob die Bedingung <paramref name="condition" /> erfüllt ist.
		/// Wenn nicht wird eine Ausnahme vom <typeparamref name="TException" /> geworfen.
		/// </summary>
		/// <typeparam name="TException">Typ der Ausnahme die geworfen werden soll</typeparam>
		/// <param name="condition">Bedingung die erfüllt sein muss, sonst wird eine Ausnahme geworfen</param>
		/// <param name="message">Fehlermedlung</param>
		public static void Requires<TException>(bool condition, string message)
			where TException : Exception
		{
			if (!condition)
				throw (TException)Activator.CreateInstance(typeof(TException), message);
		}

		/// <summary>
		/// Prüft ob einer der <c>parameter</c> null ist.
		/// </summary>
		/// <exception cref="System.ArgumentNullException">Falls <code>parameter</code> null ist.</exception>
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
		/// Prüft ob ein <c>parameter</c> null ist.
		/// </summary>
		/// <exception cref="System.ArgumentNullException">Falls <code>parameter</code> null ist.</exception>
		/// <param name="parameter">Ausdruck des zu prüfenden Objektes z.B ()=>name</param>
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
		/// Prüft ob ein <c>parameter</c> null ist.
		/// </summary>
		/// <exception cref="System.ArgumentNullException">Falls <code>parameter</code> null ist.</exception>
		/// <param name="parameter">Das zu prüfende Objekt</param>
		/// <param name="parameterName">Name des Parameters</param>
		public static void NotNull(object parameter, string parameterName)
		{
			if (parameter == null)
				throw new ArgumentNullException(parameterName, string.Format(argumentNullExceptionMessage, parameterName, ReflectionHelper.GetCallingMethod().Name, ReflectionHelper.GetCallingMethod(1).Name));
		}

		/// <summary>
		/// Prüft ob ein <see cref="Guid"/> is leer (Empty)
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <param name="parameterName">Name of the parameter.</param>
		public static void NotNull(Guid parameter, string parameterName)
		{
			if (parameter == Guid.Empty)
				throw new ArgumentNullException(parameterName, string.Format(argumentNullExceptionMessage, parameterName, ReflectionHelper.GetCallingMethod().Name, ReflectionHelper.GetCallingMethod(1).Name));
		}

		/// <summary>
		/// Prüft ob ein <c>parameter</c> null or leer ist.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <param name="parameterName">Name of the parameter.</param>
		public static void NotNullOrEmpty(string parameter, string parameterName)
		{
			if (string.IsNullOrEmpty(parameter))
				throw new ArgumentNullException(string.Format(argumentNullExceptionMessage, parameterName, ReflectionHelper.GetCallingMethod().Name, ReflectionHelper.GetCallingMethod(1).Name));
		}


		/// <summary>
		/// Prüft ob das Betribssystem mindestens Vista ist
		/// </summary>
		public static void IsVistaOrHigher()
		{
			if (!(Environment.OSVersion.Version.Major >= 6))
				throw new InvalidOperationException("Invalid OperatingSystem");
		}

		/// <summary>
		/// Führt den code block in einem trycatch aus, und wirft ggf die Exception
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
		/// 
		/// </summary>
		/// <param name="block"></param>
		/// <param name="onException"></param>
		/// <typeparam name="TResult"></typeparam>
		/// <typeparam name="TException"></typeparam>
		/// <returns></returns>
		public static async Task TryCatchAsync<TResult, TException>(Func<TResult> block, Func<TException, Exception>? onException = null)
			where TException : Exception
		{
			await Task.Run(() => TryCatch(block, onException));
		}

		/// <summary>
		///  Führt den code block in einem trycatch aus, und wirft ggf die Exception
		/// </summary>
		/// <param name="block"></param>
		/// <param name="onException"></param>
		/// <typeparam name="TException"></typeparam>
		/// <returns></returns>
		public static async Task TryCatchAsync<TException>(Action block, Func<TException, Exception>? onException = null)
			where TException : Exception
		{
			await Task.Run(() => TryCatch(block, onException));
		}

		/// <summary>
		///  Führt den code block in einem trycatch aus, und wirft ggf die Exception
		/// </summary>
		/// <param name="task"></param>
		/// <param name="onException"></param>
		/// <param name="cancellation"></param>
		/// <typeparam name="TResult"></typeparam>
		/// <typeparam name="TException"></typeparam>
		/// <returns></returns>
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
		///  Führt den code block in einem trycatch aus, und wirft ggf die Exception
		/// </summary>
		/// <param name="block"></param>
		/// <param name="onException"></param>
		/// <typeparam name="TException"></typeparam>
		/// <returns></returns>
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
		/// Führt den code block in einem trycatch aus, und wirft ggf die Exception
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