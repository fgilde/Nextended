using System;
using System.Threading.Tasks;

namespace Nextended.Core
{
    public static class Waiter
    {
        public static async Task WaitForTrueAsync(this Func<bool> expression)
        {
            while (!expression())
            {
                await Task.Delay(TimeSpan.FromMilliseconds(20));
            }
        }

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