using System.Collections.Generic;
using System.Linq;

namespace Focus.Files
{
    public class EmptyFileIndex : IBucketedFileIndex, IFileIndex
    {
        public bool Contains(string path)
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

        public IEnumerable<string> GetFilePaths()
        {
            return Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetFilePaths(string bucketName)
        {
            return Enumerable.Empty<string>();
        }
    }
}
