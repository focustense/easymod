using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace Focus.Files
{
    public record BucketedPath(string BucketName, string Path);

    public delegate BucketedPath Bucketizer(string path);

    public static class Bucketizers
    {
        public static Bucketizer Default(string name = "")
        {
            return relativePath => new BucketedPath(name, relativePath);
        }

        public static Bucketizer TopLevelDirectory()
        {
            // DirectoryInfo can behave a bit strangely for relative paths. Attempting to go up (Parent) from the top
            // of the relative path lands at whatever the current working directory happens to be - not null.
            // We're not allowed to actually construct a DirectoryInfo with an empty name, so the way to get the same
            // result is to construct a dummy path and then go up one level. This can then be compared with other paths
            // to stop when they reach the "relative root" (see GetPathComponents).
            var relativeRootPath = new DirectoryInfo("a").Parent?.FullName ?? string.Empty;
            return relativePath =>
            {
                var file = new FileInfo(relativePath);
                var dir = file.Directory;
                if (dir?.FullName == relativeRootPath)
                    dir = null;
                while (dir?.Parent is not null && dir.Parent.FullName != relativeRootPath)
                    dir = dir.Parent;
                return new BucketedPath(
                    dir?.Name ?? string.Empty,
                    dir is not null ? Path.GetRelativePath(dir.FullName, file.FullName) : relativePath);
            };
        }
    }

    public sealed class FileSystemIndex : INotifyingBucketedFileIndex, INotifyingFileIndex, IDisposable
    {
        private readonly HashSet<string> allPaths = new(PathComparer.Default);
        private readonly Bucketizer bucketize;
        private readonly Dictionary<string, HashSet<string>> buckets = new(StringComparer.CurrentCultureIgnoreCase);
        private readonly Dictionary<string, HashSet<string>> inverseBuckets =
            new(StringComparer.CurrentCultureIgnoreCase);
        private readonly IFileSystem fs;
        private readonly IDirectoryInfo rootDirectory;
        private readonly IFileSystemWatcher watcher;

        private bool isDisposed;

        public event EventHandler<FileEventArgs>? Added;
        public event EventHandler<BucketedFileEventArgs>? AddedToBucket;
        public event EventHandler<FileEventArgs>? Removed;
        public event EventHandler<BucketedFileEventArgs>? RemovedFromBucket;

        public static FileSystemIndex Build(string rootPath, IEnumerable<string>? extensions = null)
        {
            return Build(new FileSystem(), rootPath, Bucketizers.Default(), extensions);
        }

        public static FileSystemIndex Build(
            string rootPath, Bucketizer bucketize, IEnumerable<string>? extensions = null)
        {
            return Build(new FileSystem(), rootPath, bucketize, extensions);
        }

        public static FileSystemIndex Build(
            IFileSystem fs, string rootPath, Bucketizer bucketize, IEnumerable<string>? extensions = null)
        {
            var rootDirectory = fs.DirectoryInfo.FromDirectoryName(rootPath);
            var extensionsSet = extensions?.ToHashSet(StringComparer.CurrentCultureIgnoreCase);
            var initialFiles = rootDirectory.Exists ?
                rootDirectory.EnumerateFiles("*", SearchOption.AllDirectories)
                    .Where(x => extensionsSet is null || extensionsSet.Contains(x.Extension))
                    .Select(x => fs.Path.GetRelativePath(rootDirectory.FullName, x.FullName)) :
                Enumerable.Empty<string>();

            var watcher = fs.FileSystemWatcher.CreateNew(rootDirectory.FullName);
            foreach (var ext in extensionsSet ?? Enumerable.Empty<string>())
                watcher.Filters.Add("*" + ext);
            watcher.NotifyFilter = NotifyFilters.FileName;
            watcher.IncludeSubdirectories = true;

            var index = new FileSystemIndex(fs, rootDirectory, bucketize, initialFiles, watcher);
            watcher.EnableRaisingEvents = true;
            return index;
        }

        private FileSystemIndex(
            IFileSystem fs, IDirectoryInfo rootDirectory, Bucketizer bucketize, IEnumerable<string> initialFiles,
            IFileSystemWatcher watcher)
        {
            this.bucketize = bucketize;
            this.fs = fs;
            this.rootDirectory = rootDirectory;
            this.watcher = watcher;

            allPaths = initialFiles.ToHashSet(PathComparer.Default);
            buckets = allPaths
                .Select(f => bucketize(f))
                .GroupBy(x => x.BucketName, x => x.Path, StringComparer.CurrentCultureIgnoreCase)
                .Select(g => new { BucketName = g.Key, Items = g.ToHashSet(PathComparer.Default) })
                .ToDictionary(x => x.BucketName, x => x.Items, StringComparer.CurrentCultureIgnoreCase);
            inverseBuckets = buckets
                .SelectMany(b => b.Value.Select(path => new { BucketName = b.Key, FilePath = path }))
                .GroupBy(x => x.FilePath, PathComparer.Default)
                .Select(g => new
                {
                    FilePath = g.Key,
                    BucketNames = g.Select(x => x.BucketName).ToHashSet(StringComparer.CurrentCultureIgnoreCase),
                })
                .ToDictionary(x => x.FilePath, x => x.BucketNames, PathComparer.Default);

            watcher.Created += Watcher_Created;
            watcher.Deleted += Watcher_Deleted;
            watcher.Renamed += Watcher_Renamed;
        }

        public bool Contains(string relativePath)
        {
            return allPaths.Contains(relativePath);
        }

        public bool Contains(string bucketName, string pathInBucket)
        {
            return buckets.TryGetValue(bucketName, out var bucket) && bucket.Contains(pathInBucket);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IEnumerable<KeyValuePair<string, string>> FindInBuckets(string pathInBucket)
        {
            return inverseBuckets.TryGetValue(pathInBucket, out var bucketNames) ?
                bucketNames.Select(b => new KeyValuePair<string, string>(b, pathInBucket)) :
                Enumerable.Empty<KeyValuePair<string, string>>();
        }

        public IEnumerable<KeyValuePair<string, IEnumerable<string>>> GetBucketedFilePaths()
        {
            return buckets.Select(b => new KeyValuePair<string, IEnumerable<string>>(b.Key, b.Value));
        }

        public IEnumerable<string> GetFilePaths(string bucketName)
        {
            return buckets.TryGetValue(bucketName, out var fileNames) ? fileNames : Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetFilePaths()
        {
            return allPaths;
        }

        private void Dispose(bool disposing)
        {
            if (!isDisposed)
                return;
            if (disposing)
            {
                watcher.Dispose();
                allPaths.Clear();
                buckets.Clear();
                inverseBuckets.Clear();
            }
            isDisposed = true;
        }

        private void TrackFile(string absolutePath)
        {
            var relativePath = fs.Path.GetRelativePath(rootDirectory.FullName, absolutePath);
            var bucketedPath = bucketize(relativePath);
            if (!buckets.TryGetValue(bucketedPath.BucketName, out var bucket))
            {
                bucket = new(PathComparer.Default);
                buckets.Add(bucketedPath.BucketName, bucket);
            }
            bucket.Add(bucketedPath.Path);
            if (!inverseBuckets.TryGetValue(bucketedPath.Path, out var bucketNames))
            {
                bucketNames = new(StringComparer.CurrentCultureIgnoreCase);
                inverseBuckets.Add(bucketedPath.Path, bucketNames);
            }
            bucketNames.Add(bucketedPath.BucketName);
            allPaths.Add(relativePath);
            Added?.Invoke(this, new FileEventArgs(relativePath));
            AddedToBucket?.Invoke(this, new BucketedFileEventArgs(bucketedPath.BucketName, bucketedPath.Path));
        }

        private void UntrackFile(string absolutePath)
        {
            var relativePath = fs.Path.GetRelativePath(rootDirectory.FullName, absolutePath);
            var bucketedPath = bucketize(relativePath);
            if (buckets.TryGetValue(bucketedPath.BucketName, out var bucket) && bucket.Remove(bucketedPath.Path))
                RemovedFromBucket?.Invoke(this, new BucketedFileEventArgs(bucketedPath.BucketName, bucketedPath.Path));
            if (inverseBuckets.TryGetValue(bucketedPath.Path, out var bucketNames))
                bucketNames.Remove(bucketedPath.BucketName);
            if (allPaths.Remove(relativePath))
                Removed?.Invoke(this, new FileEventArgs(relativePath));
            
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            UntrackFile(e.OldFullPath);
            TrackFile(e.FullPath);
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            UntrackFile(e.FullPath);
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            TrackFile(e.FullPath);
        }
    }
}
