#nullable enable

using Focus.Files;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Profiles
{
    public class FileSystemMugshotRepository : IMugshotRepository, IDisposable
    {
        record MugshotFileInfo(IRecordKey Key, string Extension, string ModName);

        public static string[] DefaultExtensions => new[] { ".jpg", ".jpeg", ".png" };

        private readonly HashSet<string> extensions;
        private readonly IFileSystem fs;

        private string currentDirectoryPath = string.Empty;
        private bool disposed;
        private Task<IBucketedFileIndex> indexTask = Task.FromResult<IBucketedFileIndex>(new EmptyFileIndex());

        public FileSystemMugshotRepository(IObservable<string> directoryPathObs, IEnumerable<string>? extensions = null)
            : this(new FileSystem(), directoryPathObs, extensions) { }

        public FileSystemMugshotRepository(
            IFileSystem fs, IObservable<string> directoryPathObs, IEnumerable<string>? extensions = null)
        {
            this.extensions = new HashSet<string>(extensions ?? DefaultExtensions, StringComparer.OrdinalIgnoreCase);
            this.fs = fs;

            directoryPathObs.Subscribe(RebuildIndex);
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
            if (disposed)
                return;
            if (disposing)
                DisposeIndex();
            disposed = true;
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
