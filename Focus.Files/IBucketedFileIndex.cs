using System.Collections.Generic;

namespace Focus.Files
{
    public interface IBucketedFileIndex
    {
        IEnumerable<KeyValuePair<string, string>> FindInBuckets(string pathInBucket);
        IEnumerable<KeyValuePair<string, IEnumerable<string>>> GetBucketedFilePaths();
        IEnumerable<string> GetFilePaths(string bucketName);
    }
}
