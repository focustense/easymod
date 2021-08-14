using Focus.Apps.EasyNpc.Profile;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Build.Checks
{
    public class MissingPlugins : IBuildCheck
    {
        private readonly IGameSettings gameSettings;
        private readonly IReadOnlyProfileEventLog profileEvents;

        public MissingPlugins(IGameSettings gameSettings, IReadOnlyProfileEventLog profileEvents)
        {
            this.gameSettings = gameSettings;
            this.profileEvents = profileEvents;
        }

        public IEnumerable<BuildWarning> Run(Profiles.Profile profile, BuildSettings settings)
        {
            var npcs = profile.Npcs.ToDictionary(x => new RecordKey(x), RecordKeyComparer.Default);
            return profileEvents
                .MostRecentByNpc()
                .WithMissingPlugins(gameSettings.PluginLoadOrder.ToHashSet(StringComparer.OrdinalIgnoreCase))
                .Select(x => npcs.TryGetValue(x, out var npc) ?
                    new
                    {
                        npc.BasePluginName,
                        npc.LocalFormIdHex,
                        npc.EditorId,
                        npc.Name,
                        FieldName = x.Field == NpcProfileField.FacePlugin ? "face" : "default",
                        PluginName = x.NewValue,
                    } : null)
                .Where(x => x != null)
                .Select(x => new BuildWarning(
                    new RecordKey(x.BasePluginName, x.LocalFormIdHex),
                    BuildWarningId.SelectedPluginRemoved,
                    WarningMessages.SelectedPluginRemoved(x.EditorId, x.Name, x.FieldName, x.PluginName)));
        }
    }
}
