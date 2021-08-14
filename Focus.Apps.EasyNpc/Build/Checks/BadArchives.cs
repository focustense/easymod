using Focus.Files;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Build.Checks
{
    public class BadArchives : IBuildCheck
    {
        private readonly IArchiveProvider archiveProvider;

        public BadArchives(IArchiveProvider archiveProvider)
        {
            this.archiveProvider = archiveProvider;
        }

        public IEnumerable<BuildWarning> Run(Profiles.Profile profile, BuildSettings settings)
        {
            return archiveProvider.GetBadArchivePaths()
                .Select(p => new BuildWarning(
                    BuildWarningId.BadArchive,
                    WarningMessages.BadArchive(p)));
        }
    }
}
