using System;
using System.IO;

namespace NPC_Bundler
{
    static class StringExtensions
    {
        public static string PrefixPath(this string path, string prefix)
        {
            return
                (path.StartsWith(prefix + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(prefix + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) ?
                path : Path.Combine(prefix, path);
        }
    }
}