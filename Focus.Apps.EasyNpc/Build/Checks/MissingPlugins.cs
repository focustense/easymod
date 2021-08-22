using Focus.Apps.EasyNpc.Profiles;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Build.Checks
{
    public class MissingPlugins : IBuildCheck
    {
        public IEnumerable<BuildWarning> Run(Profile profile, BuildSettings settings)
        {
            return profile.Npcs
                .Where(x => x.HasMissingPlugins)
                .SelectMany(x =>
                    new (NpcProfileField field, string? pluginName)[]
                    {
                        (NpcProfileField.DefaultPlugin, x.MissingDefaultPluginName),
                        (NpcProfileField.FacePlugin, x.MissingFacePluginName)
                    }
                    .Where(f => !string.IsNullOrEmpty(f.pluginName))
                    .Select(f => new
                    {
                        x.BasePluginName,
                        x.LocalFormIdHex,
                        x.EditorId,
                        x.Name,
                        FieldName = f.field == NpcProfileField.FacePlugin ? "face" : "default",
                        PluginName = f.pluginName!,
                    }))
                .Select(x => new BuildWarning(
                    new RecordKey(x.BasePluginName, x.LocalFormIdHex),
                    BuildWarningId.SelectedPluginRemoved,
                    WarningMessages.SelectedPluginRemoved(x.EditorId, x.Name, x.FieldName, x.PluginName)));
        }
    }
}
