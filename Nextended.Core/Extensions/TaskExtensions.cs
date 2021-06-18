using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nextended.Core.Extensions
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Tries to execute a <see cref="Task"/> until it has been successfully executed or the defined retry count
        /// has been reached.
        /// </summary>
        /// <param name="task">The task to execute</param>
        /// <param name="retryCount">How many times to try to execute the task</param>
        /// <param name="retryDelayMilliseconds">The time to wait between the executions</param>
        /// <returns></returns>
        public static Task<T> RetryOnException<T>(this Task<T> task, int retryCount, int retryDelayMilliseconds)
        {
            return RetryOnException(task, retryCount, TimeSpan.FromMilliseconds(retryDelayMilliseconds));
        }

        public static Task<T> RetryOnException<T>(this Task<T> task, int retryCount, TimeSpan retryDelay)
        {
            return RetryOnException<T, Exception>(task, retryCount, retryDelay);
        }

        public static async Task<T> RetryOnException<T, TException>(this Task<T> task, int retryCount, TimeSpan retryDelay)
            where TException : Exception
        {
            Exception exception = null;

            for (var i = 0; i < retryCount; i++)
            {
                try
                {
                    return await task;
                }
                catch (TException e)
                {
                    exception = e;

                    if (i < retryCount - 1) 
                        await Task.Delay(retryDelay);
                    else
                        throw;
                }
            }

            throw new Exception("Max. retry reached while trying to execute task", exception);
        }

        /// <summary>
        /// Tries to execute a <see cref="Task"/> within a certain amount of time. If the time limit is reached
        /// before the task has been executed a <see cref="TimeoutException"/> is thrown.
        /// </summary>
        /// <param name="task">The task to execute</param>
        /// <param name="timeout">The time limit</param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        /// <exception cref="TimeoutException"></exception>
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            using var timeoutCancellationTokenSource = new CancellationTokenSource();
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask == task)
            {
                timeoutCancellationTokenSource.Cancel();
                return await task;
            }

            throw new TimeoutException("The operation has timed out.");
        }

        /// <summary>
        /// Tries to execute a <see cref="Task"/> within a certain amount of time. If the time limit is reached
        /// the provided function <see cref="onTimeoutReached"/> is executed.
        /// </summary>
        /// <param name="task">The task to execute</param>
        /// <param name="timeout">The time limit</param>
        /// <param name="onTimeoutReached">The function to execute if the time limit is reached</param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout, Func<TResult> onTimeoutReached)
        {
            using var timeoutCancellationTokenSource = new CancellationTokenSource();
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask == task)
            {
                timeoutCancellationTokenSource.Cancel();
                return await task;
            }

            return onTimeoutReached();
        }
    }
}