using Focus.Apps.EasyNpc.GameData.Files;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Profile
{
    static class ProfileExtensions
    {
        public static IEnumerable<ProfileEvent> MostRecentByNpc(this IEnumerable<ProfileEvent> events)
        {
            return events
                .GroupBy(x => new { x.BasePluginName, x.LocalFormIdHex, x.Field })
                .Select(g => g.Last());
        }

        public static IEnumerable<ProfileEvent> WithMissingPlugins(
            this IEnumerable<ProfileEvent> events, IReadOnlySet<string> currentPlugins,
            ModPluginMap modPluginMap = null)
        {
            return events.Where(e =>
                SelectsMissingPlugin(e, currentPlugins) ||
                SelectsModWithMissingPlugin(e, currentPlugins, modPluginMap));
        }

        private static bool SelectsMissingPlugin(ProfileEvent e, IReadOnlySet<string> currentPlugins)
        {
            return
                (e.Field == NpcProfileField.DefaultPlugin || e.Field == NpcProfileField.FacePlugin) &&
                !currentPlugins.Contains(e.NewValue);
        }

        // This isn't really used for build checks, because the condition is already covered when checking for
        // consistency: if no plugin is detected for a selected mod, then that means a non-matching plugin must be
        // selected and therefore it won't match. However, when filtering in the UI, a more conventional interpretation
        // of "missing" will be "missing anything at all" - including, not necessarily only NPCs referencing a no-longer
        // existant plugin or non-installed mod, but a mod that is not "used" - or, a mod that provides a plugin which
        // is no longer active.
        private static bool SelectsModWithMissingPlugin(
            ProfileEvent e, IReadOnlySet<string> currentPlugins, ModPluginMap modPluginMap)
        {
            if (modPluginMap == null)
                return false;
            return
                e.Field == NpcProfileField.FaceMod && !string.IsNullOrEmpty(e.NewValue) &&
                !modPluginMap.GetPluginsForMod(e.NewValue).Any(p => currentPlugins.Contains(p));
        }
    }
}