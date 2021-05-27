using System;
using System.IO;

namespace NPC_Bundler
{
    static class StringExtensions
    {
        public static string PrefixPath(this string path, string prefix)
        {
            if (StartsWithPathPrefix(path, "data")) // Unnecessary and annoying
                path = path[5..];
            return StartsWithPathPrefix(path, prefix) ? path : Path.Combine(prefix, path);
        }

        private static bool StartsWithPathPrefix(string path, string prefix)
        {
            return
                path.StartsWith(prefix + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(prefix + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
    }
}