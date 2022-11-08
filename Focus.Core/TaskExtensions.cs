using System;
using System.Threading;
using System.Threading.Tasks;

namespace Focus
{
    public static class TaskExtensions
    {
        public static async Task<T> Catch<T, TException>(
            this Task<T> task, Func<TException, T> exceptionResultSelector)
            where TException : Exception
        {
            try
            {
                return await task;
            }
            catch (TException ex)
            {
                return exceptionResultSelector(ex);
            }
        }

        public static Task<T> WithTimeout<T>(
            this Task<T> task, int timeoutMs, Action? onTimeout = null,
            CancellationToken cancellationToken = default)
        {
            return WithTimeout(
                task, TimeSpan.FromMilliseconds(timeoutMs), onTimeout, cancellationToken);
        }

        public static async Task<T> WithTimeout<T>(
            this Task<T> task, TimeSpan timeout, Action? onTimeout = null,
            CancellationToken cancellationToken = default)
        {
            using var timeoutCts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timeoutTask = Task.Delay(timeout, timeoutCts.Token);
            var result = await Task.WhenAny(task, timeoutTask);
            if (result.IsCanceled)
                throw new TaskCanceledException(result);
            if (result == timeoutTask)
            {
                onTimeout?.Invoke();
                throw new TimeoutException($"Task timed out after {timeout}.");
            }
            timeoutCts.Cancel();
            return await task;
        }
    }
}
