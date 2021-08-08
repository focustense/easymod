using System;
using System.Collections.Generic;

namespace Focus.Files
{
    public interface IBucketedFileIndex
    {
        bool Contains(string bucketName, string pathInBucket);
        IEnumerable<KeyValuePair<string, string>> FindInBuckets(string pathInBucket);
        IEnumerable<KeyValuePair<string, IEnumerable<string>>> GetBucketedFilePaths();
        IEnumerable<string> GetBucketNames();
        IEnumerable<string> GetFilePaths(string bucketName);
        bool IsEmpty(string bucketName);
    }

    public interface IFileIndex
    {
        bool Contains(string path);
        bool IsEmpty();
        IEnumerable<string> GetFilePaths();
    }

    public interface INotifyingBucketedFileIndex : IBucketedFileIndex
    {
        event EventHandler<BucketedFileEventArgs> AddedToBucket;
        event EventHandler<BucketedFileEventArgs> RemovedFromBucket;
    }

    public interface INotifyingFileIndex : IFileIndex
    {
        event EventHandler<FileEventArgs> Added;
        event EventHandler<FileEventArgs> Removed;
    }

    public class FileEventArgs : EventArgs
    {
        public string Path { get; private init; }

        public FileEventArgs(string path)
        {
            Path = path;
        }
    }

    public class BucketedFileEventArgs : FileEventArgs
    {
        public string BucketName { get; private init; }

        public BucketedFileEventArgs(string bucketName, string path)
            : base(path)
        {
            BucketName = bucketName;
        }
    }
}
