using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Files
{
    public class EmptyFileIndex : INotifyingBucketedFileIndex, INotifyingFileIndex
    {
        public event EventHandler<FileEventArgs>? Added;
        public event EventHandler<FileEventArgs>? Removed;
        public event EventHandler<BucketedFileEventArgs>? AddedToBucket;
        public event EventHandler<BucketedFileEventArgs>? RemovedFromBucket;

        public bool Contains(string path)
        {
            return false;
        }

        public bool Contains(string bucketName, string pathInBucket)
        {
            return false;
        }

        public IEnumerable<KeyValuePair<string, string>> FindInBuckets(string pathInBucket)
        {
            return Enumerable.Empty<KeyValuePair<string, string>>();
        }

        public IEnumerable<KeyValuePair<string, IEnumerable<string>>> GetBucketedFilePaths()
        {
            return Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>();
        }

        public IEnumerable<string> GetBucketNames()
        {
            return Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetFilePaths()
        {
            return Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetFilePaths(string bucketName)
        {
            return Enumerable.Empty<string>();
        }

        public bool IsEmpty()
        {
            return true;
        }

        public bool IsEmpty(string bucketName)
        {
            return true;
        }
    }
}
