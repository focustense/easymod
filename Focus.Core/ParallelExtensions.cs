using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Focus
{
    public static class ParallelExtensions
    {
        // The "spin" in the ThrottledSelect yield loop exists as a last resort to guard against any
        // undiscovered deadlock bugs. If no more results are seen for some (generally long) period
        // of time, it will do another check to see if all of the tasks are actually done, in case
        // the various pieces of dependent state were set in the wrong/unexpected order.
        private const int ThrottleYieldSpinIntervalMs = 1000;

        public static async IAsyncEnumerable<U> ThrottledSelect<T, U>(
            this IEnumerable<T> source, Func<T, Task<U>> taskSelector,
            ParallelOptions? parallelOptions = null)
        {
            if (parallelOptions is null)
                parallelOptions = new();
            var pendingResults = new ConcurrentBag<U>();
            int throttleMax = parallelOptions.MaxDegreeOfParallelism > 0
                ? parallelOptions.MaxDegreeOfParallelism : Environment.ProcessorCount;
            using var throttle = new SemaphoreSlim(throttleMax, throttleMax);
            using var pendingSignal = new ManualResetEventSlim();
            Exception? pendingException = null;
            var startedTaskCount = 0;
            var completedTaskCount = 0;
            var allTasksStarted = false;

            // We run the loop itself in a background task so that it does not block the enumerator
            // from returning the results available so far. Otherwise, the enumerator would not
            // yield any results until the final task had started.
            _ = Task.Run(() =>
            {
                using var sourceEnumerator = source.GetEnumerator();
                if (!sourceEnumerator.MoveNext())
                    return;
                while (true)
                {
                    parallelOptions.CancellationToken.ThrowIfCancellationRequested();
                    var item = sourceEnumerator.Current;
                    // The manual enumeration (vs. more typical foreach) is important for avoiding
                    // a potential race condition. If we simply set hasMoreItems after the end of a
                    // foreach loop, there is a possibility of all tasks starting and finishing and
                    // signaling the yield loop below, leaving the method stuck in a wait that will
                    // never resolve.
                    // By ensuring that allTasksStarted gets set *before* starting the final task,
                    // we guarantee that by the time pendingSignal gets signaled by the completion
                    // of that final task, the yield loop knows that there will be no more results.
                    bool hasMoreItems = sourceEnumerator.MoveNext();
                    if (!hasMoreItems)
                        allTasksStarted = true;
                    StartTask(item);
                    if (!hasMoreItems)
                        break;
                }
            }, parallelOptions.CancellationToken);

            while (
                !allTasksStarted
                || !pendingResults.IsEmpty
                || completedTaskCount < startedTaskCount)
            {
                // Spinning up a new task and waiting for the signal might be expensive. Since the
                // producer is running on a different thread, there may have already been new
                // results since the loop iteration started. Don't perform a potentially expensive
                // wait if we can avoid it.
                if (pendingResults.IsEmpty && !pendingSignal.Wait(0))
                {
                    var pendingSignalSource = new TaskCompletionSource<bool>();
                    var registration = ThreadPool.RegisterWaitForSingleObject(
                        pendingSignal.WaitHandle,
                        (_, timedOut) => pendingSignalSource.TrySetResult(timedOut),
                        /* state= */ null,
                        /* millisecondsTimeOutInterval= */ ThrottleYieldSpinIntervalMs,
                        /* executeOnlyOnce= */ true);
                    using var cancellationRegistration = parallelOptions.CancellationToken.Register(
                        () => pendingSignalSource.TrySetCanceled(),
                        /* useSynchronizationContext= */ false);
                    var hasMoreResults = false;
                    try
                    {
                        hasMoreResults = await pendingSignalSource.Task.ConfigureAwait(false);
                    }
                    finally
                    {
                        registration.Unregister(null);
                    }
                    if (!hasMoreResults)
                        continue;
                }
                pendingSignal.Reset();
                while (pendingResults.TryTake(out var nextResult))
                    yield return nextResult;
            }

            if (pendingException is not null)
                throw pendingException;

            void StartTask(T item)
            {
                throttle.Wait(parallelOptions.CancellationToken);
                Task<U> nextTask;
                try
                {
                    nextTask = taskSelector(item);
                    Interlocked.Increment(ref startedTaskCount);
                    nextTask.ContinueWith(t =>
                    {
                        throttle.Release();
                        if (t.IsCompletedSuccessfully)
                            pendingResults.Add(t.Result);
                        Interlocked.Increment(ref completedTaskCount);
                        if (t.Exception is not null)
                        {

                            // Doesn't really matter if we overwrite a previous exception.
                            // We just need to let the iterator know about one of them.
                            pendingException = t.Exception;
                            throw t.Exception;
                        }
                        pendingSignal.Set();
                    });
                }
                catch
                {
                    throttle.Release();
                    throw;
                }
            }
        }
    }
}
