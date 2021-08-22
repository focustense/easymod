using System.Collections.Generic;
using System.IO.Abstractions;

namespace Focus.Testing.Files
{
    public class FakeFileSystemWatcherFactory : IFileSystemWatcherFactory
    {
        public IEnumerable<FakeFileSystemWatcher> Watchers => watchers;

        private readonly List<FakeFileSystemWatcher> watchers = new();

        public IFileSystemWatcher CreateNew()
        {
            var watcher = new FakeFileSystemWatcher();
            watchers.Add(watcher);
            return watcher;
        }

        public IFileSystemWatcher CreateNew(string path)
        {
            var watcher = new FakeFileSystemWatcher { Path = path };
            watchers.Add(watcher);
            return watcher;
        }

        public IFileSystemWatcher CreateNew(string path, string filter)
        {
            var watcher = new FakeFileSystemWatcher { Path = path, Filter = filter };
            watchers.Add(watcher);
            return watcher;
        }
    }
}
