using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;

namespace Focus.Files
{
    public class PathComparer : IEqualityComparer<string>
    {
        public static readonly PathComparer Default = new();

        public static string NormalizePath(string path)
        {
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            return Path.TrimEndingDirectorySeparator(path).ToLower(CultureInfo.CurrentCulture);
        }

        public bool Equals(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;
            return NormalizePath(x) == NormalizePath(y);
        }

        public int GetHashCode([DisallowNull] string obj)
        {
            return NormalizePath(obj).GetHashCode();
        }
    }
}
