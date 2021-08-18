using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Focus.Files
{
    public interface IFileSync
    {
        IDisposable Lock(string path);
    }

    public class FileSync : IFileSync, IDisposable
    {
        private ConcurrentDictionary<string, AutoResetEvent> locks = new(PathComparer.Default);

        private bool isDisposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IDisposable Lock(string path)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(FileSync));
            var resetEvent = locks.GetOrAdd(path, () => new AutoResetEvent(true));
            resetEvent.WaitOne();
            return new LockReleaser(resetEvent);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;
            // Set this early to avoid new locks being taken out.
            isDisposed = true;
            if (disposing)
            {
                foreach (var re in locks.Values)
                    re.Dispose();
                locks.Clear();
            }
        }

        class LockReleaser : IDisposable
        {
            private readonly AutoResetEvent resetEvent;

            public LockReleaser(AutoResetEvent resetEvent)
            {
                this.resetEvent = resetEvent;
            }

            public void Dispose()
            {
                resetEvent.Set();
            }
        }
    }
}
