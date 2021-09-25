using Focus.Apps.EasyNpc.Profiles;
using System.Collections.Generic;

namespace Focus.Apps.EasyNpc.Build.Checks
{
    public class MissingPlugins : INpcBuildCheck
    {
        public IEnumerable<BuildWarning> Run(Npc npc, BuildSettings _)
        {
            if (!string.IsNullOrEmpty(npc.MissingDefaultPluginName))
                yield return GetWarning(npc, "default", npc.MissingDefaultPluginName);
            if (!string.IsNullOrEmpty(npc.MissingFacePluginName))
                yield return GetWarning(npc, "face", npc.MissingFacePluginName);
        }

        private static BuildWarning GetWarning(Npc npc, string fieldName, string pluginName)
        {
            return new BuildWarning(
                new RecordKey(npc),
                BuildWarningId.SelectedPluginRemoved,
                WarningMessages.SelectedPluginRemoved(npc.EditorId, npc.Name, fieldName, pluginName));
        }
    }
}
