using System.Collections.Generic;
using System.Linq;

namespace Focus.Files
{
    public class ReadOnlyFileIndex : IFileIndex
    {
        private readonly HashSet<string> paths;

        public ReadOnlyFileIndex(IEnumerable<string> paths)
        {
            this.paths = paths.ToHashSet(PathComparer.Default);
        }

        public bool Contains(string path)
        {
            return paths.Contains(path);
        }

        public IEnumerable<string> GetFilePaths()
        {
            return paths;
        }

        public bool IsEmpty()
        {
            return paths.Count == 0;
        }
    }
}
