using Focus.Apps.EasyNpc.Profiles;
using Focus.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Focus.Apps.EasyNpc.Build.Checks
{
    public class BadArchives : IGlobalBuildCheck
    {
        private readonly IArchiveProvider archiveProvider;

        public BadArchives(IArchiveProvider archiveProvider)
        {
            this.archiveProvider = archiveProvider;
        }

        public IEnumerable<BuildWarning> Run(Profile profile, BuildSettings settings)
        {
            var badArchiveNames = archiveProvider.GetBadArchivePaths()
                .ToDictionary(p => Path.GetFileNameWithoutExtension(p), StringComparer.CurrentCultureIgnoreCase);
            foreach (var npc in profile.Npcs)
            {
                var pluginBaseName = Path.GetFileNameWithoutExtension(npc.FaceOption.PluginName);
                if (badArchiveNames.TryGetValue(pluginBaseName, out var mainArchiveName))
                    yield return new(
                        BuildWarningId.BadArchive,
                        WarningMessages.BadArchive(pluginBaseName + Path.GetExtension(mainArchiveName)));
                if (badArchiveNames.TryGetValue($"{pluginBaseName} - Textures", out var textureArchiveName))
                    yield return new(
                        BuildWarningId.BadArchive,
                        WarningMessages.BadArchive(
                            $"{pluginBaseName} - Textures" + Path.GetExtension(textureArchiveName)));
            }
        }
    }
}
