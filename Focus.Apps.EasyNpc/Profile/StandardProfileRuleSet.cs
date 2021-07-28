using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Apps.EasyNpc.GameData.Records;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Profile
{
    public class StandardProfileRuleSet : IProfileRuleSet
    {
        private readonly IReadOnlySet<string> masterNames;
        private readonly IReadOnlySet<string> overhaulNames;

        public static StandardProfileRuleSet Create<TKey>(IEnumerable<string> masterNames, IEnumerable<INpc<TKey>> npcs)
            where TKey : struct
        {
            var overhaulNames = InferOverhaulNames(npcs);
            return new StandardProfileRuleSet(masterNames, overhaulNames);
        }

        private StandardProfileRuleSet(IEnumerable<string> masterNames, IEnumerable<string> overhaulNames)
        {
            this.masterNames = masterNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
            this.overhaulNames = overhaulNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public NpcConfigurationDefaults GetConfigurationDefaults<TKey>(INpc<TKey> npc) where TKey : struct
        {
            var faceOverride = npc.Overrides.LastOrDefault(x => x.ModifiesFace);
            var defaultOverride = npc.Overrides[npc.Overrides.Count - 1];
            while (!string.IsNullOrEmpty(defaultOverride.ItpoPluginName))
            {
                if (defaultOverride.ItpoPluginName == npc.BasePluginName)
                {
                    defaultOverride = null;
                    break;
                }
                else
                {
                    var itpoOverride = npc.Overrides.SingleOrDefault(x => x.PluginName == defaultOverride.ItpoPluginName);
                    if (itpoOverride != null)   // Should never be null but we still need to check
                        defaultOverride = itpoOverride;
                }
            }
            // Note that "masterNames" below means ESMs and DLCs. These are always allowable candidates for a default
            // plugin setting.
            if (defaultOverride != null && defaultOverride == faceOverride &&
                !masterNames.Contains(defaultOverride.PluginName))
            {
                defaultOverride = null; // If we don't find a better candidate, use the original master
                var faceOverrideIndex = ((IList<NpcOverride<TKey>>)npc.Overrides).IndexOf(faceOverride);
                for (int i = faceOverrideIndex - 1; i >= 0; i--)
                {
                    var prevOverride = npc.Overrides[i];
                    // NPC overhauls tend to have a lot of inadvertent edits, and maybe some edits that are intentional
                    // but refusing to stick to the "overhaul" scope. We ignore these, since we don't want overhauls
                    // being picked as masters.
                    if (overhaulNames.Contains(prevOverride.PluginName))
                        continue;
                    if (masterNames.Contains(prevOverride.PluginName) || prevOverride.ModifiesBehavior ||
                        // This check is a weird one, and is mainly a result of tests done with the very
                        // "interesting" Northbourne NPCs, which likes to inherit from USSEP and *also* make little
                        // tweaks other than the face, especially outfits.
                        // We only get here if we did NOT detect any true behavior overrides, otherwise it would
                        // have short-circuited above. Therefore, we are either looking at a face mod, an outfit
                        // mod, or neither.
                        // We can treat an outfit mod, which does not modify faces, as a behavior mod. It may or may
                        // not be properly patched with other behavior mods, but that's not our problem right
                        // now. However, a face mod that ALSO modifies outfits should be treated as a face mod, not
                        // an outfit mod, and not selected as a behavioral default.
                        (prevOverride.ModifiesOutfits && !prevOverride.ModifiesFace) ||
                        // Body overrides are treated the same as outfits, i.e. if they're part of an overhaul then
                        // ignore them, but if they're apparently standalone then catch them.
                        (prevOverride.ModifiesBody && !prevOverride.ModifiesFace))
                    {
                        defaultOverride = prevOverride;
                        break;
                    }
                }
            }
            return new NpcConfigurationDefaults
            {
                DefaultPlugin = defaultOverride?.PluginName,
                FacePlugin = faceOverride?.PluginName,
            };
        }

        public bool IsLikelyOverhaul(string pluginName)
        {
            return overhaulNames.Contains(pluginName);
        }

        private static IEnumerable<string> InferOverhaulNames<TKey>(IEnumerable<INpc<TKey>> npcs)
            where TKey : struct
        {
            // The rules for distinguishing an overhaul from some other type of mod are currently arbitrary. Too many
            // false positives, and we'll pick masters too high in the load order and overwrite good edits. False
            // negatives, and we'll end up with overhauls as masters.
            //
            // Operating on the assumption that most NPC overhauls either do not intentionally make questionable edits,
            // or at least don't do it all over the place, we can come up with a few fuzzy but straightforward rules:
            //
            // - Mods where 100% of NPCs include face changes
            // - Mods where some high % of NPCs include face changes and only a very low % include behavior
            // ... and perhaps others, but this is as smart as we get for now.
            //
            // Looking at other record types in the mod can also be a useful signal, but requires some care in how to
            // apply it. For example, just because it adds one cell edit doesn't mean it isn't an overhaul, that can be
            // a wild edit just like NPC behavior edits. For the moment, this is left for future work.

            const double behaviorlessModifierPercentThreshold = 0.85;
            var pluginStats = npcs
                .SelectMany(x => x.Overrides
                    .Select(o => new
                    {
                        x.BasePluginName,
                        o.PluginName,
                        o.ModifiesBehavior,
                        o.ModifiesFace,
                    })
                    .Prepend(new
                    {
                        BasePluginName = x.BasePluginName,
                        PluginName = x.BasePluginName,
                        ModifiesBehavior = true,
                        ModifiesFace = true,
                    }))
                .GroupBy(x => x.PluginName)
                .Where(p => !FileStructure.IsVanilla(p.Key) && !FileStructure.IsDlc(p.Key))
                .Select(p => new
                {
                    PluginName = p.Key,
                    TotalNpcCount = p.Count(),
                    FaceModifierCount = p.Count(x =>
                        !x.BasePluginName.Equals(p.Key, StringComparison.OrdinalIgnoreCase) &&
                        x.ModifiesFace),
                    BehaviorlessFaceModifierCount = p.Count(x =>
                        x.ModifiesFace && !x.ModifiesBehavior),
                });
            return pluginStats
                .Where(p => p.FaceModifierCount > 0)
                .Where(p =>
                    p.FaceModifierCount == p.TotalNpcCount ||
                    (p.BehaviorlessFaceModifierCount >= p.FaceModifierCount * behaviorlessModifierPercentThreshold))
                .Select(p => p.PluginName);
        }
    }
}