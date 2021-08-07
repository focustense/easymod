using System.Collections.Generic;

namespace Focus.Files
{
    public interface IFileIndex
    {
        bool Contains(string path);
        IEnumerable<string> GetFilePaths();
    }
}
