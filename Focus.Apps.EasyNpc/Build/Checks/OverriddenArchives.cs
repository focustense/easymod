using Focus.Apps.EasyNpc.Profiles;
using Focus.ModManagers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Focus.Apps.EasyNpc.Build.Checks
{
    public class OverriddenArchives : IBuildCheck
    {
        private readonly IGameSettings gameSettings;
        private readonly IModRepository modRepository;

        public OverriddenArchives(IGameSettings gameSettings, IModRepository modRepository)
        {
            this.gameSettings = gameSettings;
            this.modRepository = modRepository;
        }

        public IEnumerable<BuildWarning> Run(Profile profile, BuildSettings settings)
        {
            // It is not - necessarily - a major problem for the game itself if multiple mods provide the same BSA.
            // The game, and this program, will simply use whichever version is actually loaded, i.e. from the last mod
            // in the list. However, there's no obvious way to tell *which* mod is currently providing that BSA.
            // This means that the user might select a mod for some NPC, and we might believe we are pulling facegen
            // data from that mod's archive, but in fact we are pulling it from a different version of the archive
            // provided by some other mod, which might be fine, or might be totally broken.
            // It's also extremely rare, with the only known instance (at the time of writing) being a patch to the
            // Sofia follower mod that removes a conflicting script, i.e. doesn't affect facegen data at all.
            // So it may be an obscure theoretical problem that never comes up in practice, but if we do see it, then
            // it at least merits a warning, which the user can ignore if it's on purpose.
            return gameSettings.ArchiveOrder
                .Select(path => Path.GetFileName(path))
                .Select(f => new
                {
                    Name = f,
                    ComponentNames = modRepository
                        .SearchForFiles(f, false)
                        .Select(x => x.ModComponent)
                        .Select(x => x.Name)
                        .Distinct()
                        .ToList(),
                })
                .Where(x => x.ComponentNames.Count > 1)
                .Select(x => new BuildWarning(
                    BuildWarningId.MultipleArchiveSources,
                    WarningMessages.MultipleArchiveSources(x.Name, x.ComponentNames)));
        }
    }
}
