#nullable enable

using Focus.Apps.EasyNpc.Configuration;
using Focus.Files;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Profiles
{
    public class FileSystemMugshotRepository : IMugshotRepository, IDisposable
    {
        record MugshotFileInfo(IRecordKey Key, string Extension, string ModName);

        public static string[] DefaultExtensions => new[] { ".jpg", ".jpeg", ".png" };

        private readonly Subject<bool> disposed = new();
        private readonly HashSet<string> extensions;
        private readonly IFileSystem fs;

        private string currentDirectoryPath = string.Empty;
        private Task<IBucketedFileIndex> indexTask = Task.FromResult<IBucketedFileIndex>(new EmptyFileIndex());

        public FileSystemMugshotRepository(
            IFileSystem fs, IObservableAppSettings settings, IEnumerable<string>? extensions = null)
        {
            this.extensions = new HashSet<string>(extensions ?? DefaultExtensions, StringComparer.OrdinalIgnoreCase);
            this.fs = fs;

            settings.MugshotsDirectoryObservable
                .TakeUntil(disposed)
                .Subscribe(RebuildIndex);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<IEnumerable<MugshotFile>> GetMugshotFiles(IRecordKey npcKey)
        {
            var searchPaths =
                extensions.Select(ext => fs.Path.Combine(npcKey.BasePluginName, $"00{npcKey.LocalFormIdHex}{ext}"));
            var index = await indexTask;
            var bucketedResults = searchPaths.SelectMany(path => index.FindInBuckets(path));
            return bucketedResults.Select(x =>
                new MugshotFile(string.Empty, x.Key, fs.Path.Combine(currentDirectoryPath, x.Key, x.Value)));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed.IsDisposed)
                return;
            if (disposing)
            {
                DisposeIndex();
            }
            disposed.OnNext(true);
            disposed.Dispose();
        }

        private void DisposeIndex()
        {
            indexTask.ContinueWith(t =>
            {
                if (t.Result is IDisposable disposable)
                    disposable.Dispose();
            });
        }

        private void RebuildIndex(string path)
        {
            currentDirectoryPath = path;
            DisposeIndex();
            indexTask = Task.Run<IBucketedFileIndex>(() =>
                FileSystemIndex.Build(path, Bucketizers.TopLevelDirectory(), extensions));
        }
    }
}
