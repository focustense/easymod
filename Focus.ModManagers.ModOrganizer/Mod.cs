using System.Collections.Generic;

namespace Focus.ModManagers.ModOrganizer
{
    class Mod
    {
        // Assumption: If caching of file lists inside archives is expected, then it should be handled by the Archive
        // Provider. We therefore cache the paths TO archives, but not the paths IN archives.
        public IReadOnlySet<string> ArchiveFileNames { get; init; } = new HashSet<string>();
        public string DirectoryName { get; init; } = string.Empty;
        public IReadOnlySet<string> FileNames { get; init; } = new HashSet<string>();
        public string Id { get; init; } = string.Empty;
        public string Path { get; init; } = string.Empty;
    }
}
