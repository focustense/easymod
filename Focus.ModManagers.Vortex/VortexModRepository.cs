using Focus.Files;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

namespace Focus.ModManagers.Vortex
{
    public class VortexModRepository : ComponentPerDirectoryModRepository<ComponentPerDirectoryConfiguration>
    {
        private readonly Dictionary<string, FilePriority> filePriorities;
        private readonly ModManifest manifest;

        public VortexModRepository(IFileSystem fs, IIndexedModRepository inner, ModManifest manifest)
            : base(fs, inner)
        {
            this.manifest = manifest;
            filePriorities = manifest.FilePriorities.ToDictionary(x => x.Path, PathComparer.Default);
        }

        protected override IComponentResolver GetComponentResolver(ComponentPerDirectoryConfiguration config)
        {
            return new VortexComponentResolver(manifest, config.RootPath);
        }

        protected override IEnumerable<ModSearchResult> OrderSearchResults(
            string relativePath, IEnumerable<ModSearchResult> results)
        {
            return filePriorities.TryGetValue(relativePath, out var priority) ? Prioritize(results, priority) : results;
        }

        private static bool MatchesFilePriority(ModSearchResult result, FilePriority priority)
        {
            return
                string.IsNullOrEmpty(result.ArchiveName) &&
                result.ModComponent.Name.Equals(priority.WinningFileId, StringComparison.CurrentCultureIgnoreCase);
        }

        private static IEnumerable<ModSearchResult> Prioritize(
            IEnumerable<ModSearchResult> results, FilePriority priority)
        {
            // There is some inconsistency across the project right now, as a lot of code that isn't too sensitive to
            // ordering would be "better" with implicit priority ordering, but there is a small amount of *more*
            // sensitive code that wants listing order. We go with listing for now, although this needs cleanup.
            ModSearchResult? priorityResult = null;
            foreach (var result in results)
            {
                if (MatchesFilePriority(result, priority))
                    priorityResult = result;
                else
                    yield return result;
            }
            if (priorityResult is not null)
                yield return priorityResult;
        }
    }
}
