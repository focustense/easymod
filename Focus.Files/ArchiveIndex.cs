using System.Collections.Generic;
using System.Linq;

namespace Focus.Files
{
    public class ArchiveIndex : IBucketedFileIndex
    {
        private readonly List<string> archiveOrder = new();
        private readonly IArchiveProvider archiveProvider;
        private readonly Dictionary<string, IReadOnlySet<string>> contentPaths = new(PathComparer.Default);
        private readonly Dictionary<string, HashSet<string>> inverseContentPaths = new(PathComparer.Default);

        public ArchiveIndex(IArchiveProvider archiveProvider)
        {
            this.archiveProvider = archiveProvider;
        }

        public void AddArchive(string archivePath)
        {
            AddArchives(new[] { archivePath });
        }

        public void AddArchives(IEnumerable<string> archivePaths)
        {
            var newArchivePaths = archivePaths.Where(f => !contentPaths.ContainsKey(f));
            archiveOrder.AddRange(newArchivePaths);
            var newEntries = newArchivePaths
                .AsParallel()
                .Select(p => new
                {
                    ArchivePath = p,
                    Files = archiveProvider.GetArchiveFileNames(p)
                        // Normalizing isn't for lookups, which is already handled by comparer, but so that the paths
                        // we return are normalized. Makes testing easier.
                        .Select(p => PathComparer.NormalizePath(p))
                        .ToHashSet(PathComparer.Default)
                })
                .ToList();
            foreach (var newEntry in newEntries)
            {
                contentPaths.Add(newEntry.ArchivePath, newEntry.Files);
                foreach (var contentPath in newEntry.Files)
                    GetOrAddInverseSet(contentPath).Add(newEntry.ArchivePath);
            }
        }

        public void Clear()
        {
            archiveOrder.Clear();
            contentPaths.Clear();
            inverseContentPaths.Clear();
        }

        public bool Contains(string bucketName, string pathInBucket)
        {
            return contentPaths.TryGetValue(bucketName, out var contents) && contents.Contains(pathInBucket);
        }

        public IEnumerable<KeyValuePair<string, string>> FindInBuckets(string pathInBucket)
        {
            if (inverseContentPaths.TryGetValue(pathInBucket, out var archivePaths))
            {
                var orderings = archiveOrder.Select((name, index) => (name, index)).ToDictionary(x => x.name, x => x.index);
                return archivePaths
                    .OrderBy(p => orderings[p])
                    .Select(p => new KeyValuePair<string, string>(p, PathComparer.NormalizePath(pathInBucket)));
            }
            return Enumerable.Empty<KeyValuePair<string, string>>();
        }

        public IEnumerable<KeyValuePair<string, IEnumerable<string>>> GetBucketedFilePaths()
        {
            return archiveOrder.Select(f => new KeyValuePair<string, IEnumerable<string>>(f, contentPaths[f]));
        }

        public IEnumerable<string> GetBucketNames()
        {
            return archiveOrder;
        }

        public IEnumerable<string> GetFilePaths(string bucketName)
        {
            return contentPaths.TryGetValue(bucketName, out var contents) ? contents : Enumerable.Empty<string>();
        }

        public bool IsEmpty(string bucketName)
        {
            return !contentPaths.TryGetValue(bucketName, out var contents) || contents.Count == 0;
        }

        public bool RemoveArchive(string archivePath)
        {
            var removed = contentPaths.Remove(archivePath, out var contents);
            if (removed && contents is not null)
            {
                foreach (var contentPath in contents)
                    GetOrAddInverseSet(contentPath).Remove(archivePath);
            }
            archiveOrder.Remove(archivePath);
            return removed;
        }

        private HashSet<string> GetOrAddInverseSet(string contentPath)
        {
            if (!inverseContentPaths.TryGetValue(contentPath, out var inverseSet))
            {
                inverseSet = new(PathComparer.Default);
                inverseContentPaths.Add(contentPath, inverseSet);
            }
            return inverseSet;
        }
    }
}
