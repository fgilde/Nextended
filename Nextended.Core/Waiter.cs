using System;
using System.Threading.Tasks;

namespace Nextended.Core
{
    /// <summary>
    /// Provides utility methods for asynchronously waiting for conditions or results
    /// </summary>
    public static class Waiter
    {
        /// <summary>
        /// Asynchronously waits until the specified expression returns true
        /// </summary>
        /// <param name="expression">The expression to evaluate</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public static async Task WaitForTrueAsync(this Func<bool> expression)
        {
            while (!expression())
            {
                await Task.Delay(TimeSpan.FromMilliseconds(20));
            }
        }

        /// <summary>
        /// Asynchronously waits until the specified expression returns a non-null result
        /// </summary>
        /// <typeparam name="T">The type of the result</typeparam>
        /// <param name="expression">The expression to evaluate</param>
        /// <returns>A task representing the asynchronous operation with the result</returns>
        public static async Task<T> WaitForResultAsync<T>(this Func<T> expression)
        {
            T result = expression();
            while (result == null)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(20));
                result = expression();
            }
            return result;
        }
    }
}