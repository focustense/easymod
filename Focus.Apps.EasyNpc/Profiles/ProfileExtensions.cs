using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Profiles
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
            this IEnumerable<ProfileEvent> events, IReadOnlySet<string> currentPlugins)
        {
            return events.Where(e => SelectsMissingPlugin(e, currentPlugins));
        }

        private static bool SelectsMissingPlugin(ProfileEvent e, IReadOnlySet<string> currentPlugins)
        {
            return
                (e.Field == NpcProfileField.DefaultPlugin || e.Field == NpcProfileField.FacePlugin) &&
                !currentPlugins.Contains(e.NewValue);
        }
    }
}