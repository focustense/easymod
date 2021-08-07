using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace Focus.Files.Tests
{
    class FakeFileSystemWatcher : IFileSystemWatcher
    {
        public bool IncludeSubdirectories { get; set; }
        public bool EnableRaisingEvents { get; set; }
        public string Filter
        {
            get => Filters.FirstOrDefault();
            set
            {
                Filters.Clear();
                Filters.Add(value);
            }
        }
        public Collection<string> Filters { get; } = new();

        public int InternalBufferSize { get; set; }
        public bool IsDisposed { get; private set; }
        public NotifyFilters NotifyFilter { get; set; }
        public string Path { get; set; }
        public ISite Site { get; set; }
        public ISynchronizeInvoke SynchronizingObject { get; set; }

        public event FileSystemEventHandler Changed;
        public event FileSystemEventHandler Created;
        public event FileSystemEventHandler Deleted;
        public event ErrorEventHandler Error;
        public event RenamedEventHandler Renamed;

        public void BeginInit()
        {
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        public void EndInit()
        {
        }

        public void RaiseChanged(FileSystemEventArgs e)
        {
            Changed?.Invoke(this, e);
        }

        public void RaiseChanged(string fileName)
        {
            RaiseChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, Path, fileName));
        }

        public void RaiseCreated(FileSystemEventArgs e)
        {
            Created?.Invoke(this, e);
        }

        public void RaiseCreated(string fileName)
        {
            RaiseCreated(new FileSystemEventArgs(WatcherChangeTypes.Created, Path, fileName));
        }

        public void RaiseDeleted(FileSystemEventArgs e)
        {
            Deleted?.Invoke(this, e);
        }

        public void RaiseDeleted(string fileName)
        {
            RaiseDeleted(new FileSystemEventArgs(WatcherChangeTypes.Deleted, Path, fileName));
        }

        public void RaiseError(ErrorEventArgs e)
        {
            Error?.Invoke(this, e);
        }

        public void RaiseRenamed(RenamedEventArgs e)
        {
            Renamed?.Invoke(this, e);
        }

        public void RaiseRenamed(string oldFileName, string newFileName)
        {
            RaiseRenamed(new RenamedEventArgs(WatcherChangeTypes.Renamed, Path, newFileName, oldFileName));
        }

        public WaitForChangedResult WaitForChanged(WatcherChangeTypes changeType)
        {
            throw new NotImplementedException();
        }

        public WaitForChangedResult WaitForChanged(WatcherChangeTypes changeType, int timeout)
        {
            throw new NotImplementedException();
        }
    }
}
