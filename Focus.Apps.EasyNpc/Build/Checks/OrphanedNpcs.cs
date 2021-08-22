using Focus.Apps.EasyNpc.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Build.Checks
{
    public class OrphanedNpcs : IBuildCheck
    {
        private readonly IGameSettings gameSettings;
        private readonly IReadOnlyProfileEventLog profileEvents;

        public OrphanedNpcs(IGameSettings gameSettings, IReadOnlyProfileEventLog profileEvents)
        {
            this.gameSettings = gameSettings;
            this.profileEvents = profileEvents;
        }

        public IEnumerable<BuildWarning> Run(Profile profile, BuildSettings settings)
        {
            var allPluginsInProfile = profileEvents.Select(x => x.BasePluginName).Distinct().ToList();
            var currentPlugins = gameSettings.PluginLoadOrder.ToHashSet(StringComparer.OrdinalIgnoreCase);
            return allPluginsInProfile
                .Where(p => !currentPlugins.Contains(p))
                .Select(p => new BuildWarning(
                    p, BuildWarningId.MasterPluginRemoved, WarningMessages.MasterPluginRemoved(p)));
        }
    }
}
