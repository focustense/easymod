using Focus.Files;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Testing.Files
{
    public class FakeBucketedFileIndex : INotifyingBucketedFileIndex
    {
        public event EventHandler<BucketedFileEventArgs> AddedToBucket;
        public event EventHandler<BucketedFileEventArgs> RemovedFromBucket;

        private readonly Dictionary<string, HashSet<string>> buckets = new();

        private bool isWatchingPaused = false;

        public void AddFiles(string bucketName, params string[] paths)
        {
            var bucketFiles = buckets.GetOrAdd(bucketName, () => new HashSet<string>());
            foreach (var path in paths)
            {
                bucketFiles.Add(path);
                if (!isWatchingPaused)
                    AddedToBucket?.Invoke(this, new(bucketName, path));
            }
        }

        public bool Contains(string bucketName, string pathInBucket)
        {
            return buckets.TryGetValue(bucketName, out var bucketFiles) && bucketFiles.Contains(pathInBucket);
        }

        public IEnumerable<KeyValuePair<string, string>> FindInBuckets(string pathInBucket)
        {
            return buckets
                .Where(x => x.Value.Contains(pathInBucket))
                .Select(x => new KeyValuePair<string, string>(x.Key, pathInBucket));
        }

        public IEnumerable<KeyValuePair<string, IEnumerable<string>>> GetBucketedFilePaths()
        {
            return buckets.Select(x => new KeyValuePair<string, IEnumerable<string>>(x.Key, x.Value));
        }

        public IEnumerable<string> GetBucketNames()
        {
            return buckets.Keys;
        }

        public IEnumerable<string> GetFilePaths(string bucketName)
        {
            return buckets.TryGetValue(bucketName, out var bucketFiles) ? bucketFiles : Enumerable.Empty<string>();
        }

        public bool IsEmpty(string bucketName)
        {
            return !buckets.TryGetValue(bucketName, out var bucketFiles) || bucketFiles.Count == 0;
        }

        public void PauseWatching()
        {
            isWatchingPaused = true;
        }

        public void RemoveFiles(string bucketName, params string[] paths)
        {
            if (!buckets.TryGetValue(bucketName, out var bucketFiles))
                return;
            foreach (var path in paths)
            {
                if (bucketFiles.Remove(path))
                    if (!isWatchingPaused)
                        RemovedFromBucket?.Invoke(this, new(bucketName, path));
            }
        }

        public void ResumeWatching()
        {
            isWatchingPaused = false;
        }
    }
}
