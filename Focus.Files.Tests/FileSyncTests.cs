using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Focus.Files.Tests
{
    public class FileSyncTests : IDisposable
    {
        private readonly FileSync sync;

        public FileSyncTests()
        {
            sync = new FileSync();
        }

        public void Dispose()
        {
            sync.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task ForSamePath_BlocksConcurrentAccess()
        {
            const string path = @"C:\dummy\path";
            var sharedList = new List<string>();
            using var acquireSignal1 = new ManualResetEventSlim(false);
            using var acquireSignal2 = new ManualResetEventSlim(false);
            using var mutateSignal1 = new ManualResetEventSlim(false);
            using var mutateSignal2 = new ManualResetEventSlim(false);
            using var startedSignal1 = new ManualResetEventSlim(false);
            using var startedSignal2 = new ManualResetEventSlim(false);
            var task1 = Task.Run(() =>
            {
                acquireSignal1.Wait();
                using var _ = sync.Lock(path);
                startedSignal1.Set();
                mutateSignal1.Wait();
                sharedList.Add("task 1 result");
            });
            var task2 = Task.Run(() =>
            {
                acquireSignal2.Wait();
                using var _ = sync.Lock(path);
                startedSignal2.Set();
                mutateSignal2.Wait();
                sharedList.Add("task 2 result");
            });

            acquireSignal2.Set();
            startedSignal2.Wait();
            acquireSignal1.Set();
            mutateSignal1.Set();
            // By yielding here, we should be allowing the first task to add its result, IF it was not blocked, which
            // it should be.
            await Task.Yield();
            mutateSignal2.Set();
            await Task.WhenAll(task1, task2);

            // Task 2 acquired the lock first, so it should always produce its result first, even though task 1 got
            // signaled first.
            Assert.Equal(new[] { "task 2 result", "task 1 result" }, sharedList);
        }

        [Fact]
        public async Task ForDifferentPaths_PermitsConcurrentAccess()
        {
            var sharedList = new List<string>();
            using var acquireSignal1 = new ManualResetEventSlim(false);
            using var acquireSignal2 = new ManualResetEventSlim(false);
            using var mutateSignal1 = new ManualResetEventSlim(false);
            using var mutateSignal2 = new ManualResetEventSlim(false);
            using var startedSignal1 = new ManualResetEventSlim(false);
            using var startedSignal2 = new ManualResetEventSlim(false);
            var task1 = Task.Run(() =>
            {
                acquireSignal1.Wait();
                using var _ = sync.Lock("path1");
                startedSignal1.Set();
                mutateSignal1.Wait();
                sharedList.Add("task 1 result");
            });
            var task2 = Task.Run(() =>
            {
                acquireSignal2.Wait();
                using var _ = sync.Lock("path2");
                startedSignal2.Set();
                mutateSignal2.Wait();
                sharedList.Add("task 2 result");
            });

            acquireSignal2.Set();
            startedSignal2.Wait();
            acquireSignal1.Set();
            mutateSignal1.Set();
            await Task.Yield();
            mutateSignal2.Set();
            await Task.WhenAll(task1, task2);

            // Lock order shouldn't matter here because the tasks lock on different paths, so whichever one got signaled
            // first should get the first result.
            Assert.Equal(new[] { "task 1 result", "task 2 result" }, sharedList);
        }
    }
}
