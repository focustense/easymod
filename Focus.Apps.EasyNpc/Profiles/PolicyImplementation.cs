using Focus.Analysis.Execution;
using Focus.Analysis.Plugins;
using Focus.Analysis.Records;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Profiles
{
    public class ProfilePolicy : IProfilePolicy, ILoadOrderAnalysisReceiver
    {
        private IReadOnlySet<string> baseNames = new HashSet<string>();
        private IReadOnlyDictionary<IRecordKey, RecordAnalysisChain<NpcAnalysis>> npcChains =
            new Dictionary<IRecordKey, RecordAnalysisChain<NpcAnalysis>>();
        private IReadOnlySet<string> overhaulNames = new HashSet<string>();

        /*
         * Some rules in plain English:
         * 1. Assume that visual overhauls are last in the load order (LOOT should do this).
         * 2. Assume that the rest of the load order is properly sorted and patched, aside from overhauls.
         * 3. For face plugin, start with the last plugin that modifies the face. Considering rule (1), this should
         *    either be an overhaul mod, or a patch for one.
         * 4. If the face plugin also modifies behavior, we we may have picked a patch. Check if the face is NOT changed
         *    from any of the face plugin's masters. If we find an identical face in the plugin's masters (i.e. earlier
         *    in the load order), switch to that one and run the same test again. Repeat until we either reach a plugin
         *    that modifies face and NOT behavior, or we can't find a master with the same face.
         * 5. For default plugin, start with the last plugin that modifies behavior, even if it also modifies visuals.
         *    Except for plugins flagged as NPC overhauls, some of which contain stray or "preference" edits.
         * 6. Check all of the current default's masters AND the previous override for identical behavior. If found,
         *    skip to the highest same-behavior plugin in the load order, and repeat until no more identical matches.
         *    Mark this as "Default A".
         * 7. Repeat steps 5 and 6, but start with the last behavior-modifying plugin that does NOT modify face (i.e.
         *    has vanilla face), and isn't from a plugin flagged as an overhaul. Mark the final outcome as "Default B".
         * 8. Choose the LOWER of Default A and Default B as the final default.
         *
         * The reason for this convoluted default logic is because there are two likely scenarios:
         * - The lowest behavior-modifying plugin is actually a behavior mod, or a patch for the most recent behavior
         *   mod in the load order. In this case, walking up (steps 5-6) will yield the original behavior mod.
         * - The lowest behavior-modifying plugin is a patch for a visual overhaul THAT REVERTS IMPORTANT EDITS. In this
         *   case, walking up that plugin's behavior-matching tree (step 6) is likely to land higher in the listed order
         *   than the alternate result from step 7, and the latter is more likely to be correct.
         *
         * All of this depends on the assumption that load order was "mostly sorted" to begin with. If the load order is
         * totally random, e.g. from someone who's never modded before and doesn't even know how to sort, then there's
         * very little we can do to improve things for them. Their game is probably broken already.
         */
        public NpcSetupAttributes GetSetupRecommendation(INpcBasicInfo npc)
        {
            var chain = npcChains[npc];

            // Step 3: Choose last face-modifying plugin.
            var faceGuess = chain.LastOrDefault(x => x.Analysis.ComparisonToBase?.ModifiesFace == true) ?? chain[0];
            // Step 4: Navigate up to to the first listing with the same face.
            var resolvedFaceRecord = FindEarliestSource(
                chain, faceGuess, false, x => !x.ModifiesFace,
                r => FindEarliestComparison(r.Analysis, x => x.ModifiesBehavior, false) is null);

            var nonOverhaulChain = chain.Where(x => !overhaulNames.Contains(x.PluginName));
            // Step 5: Choose last behavior-modifying plugin.
            var defaultGuessA =
                nonOverhaulChain.LastOrDefault(x =>
                    !x.Analysis.IsOverride || x.Analysis.ComparisonToBase?.ModifiesBehavior == true) ??
                chain[^1];
            // Step 6: Navigate up to the first listing with the same behavior.
            var resolvedDefaultA = FindEarliestSource(chain, defaultGuessA, true, x => !x.ModifiesBehavior);
            // Step 7: Find alternate behavior-modifying plugin.
            var defaultGuessB = nonOverhaulChain.LastOrDefault(x =>
                !x.Analysis.IsOverride || (
                    x.Analysis.ComparisonToBase?.ModifiesBehavior == true &&
                    x.Analysis.ComparisonToBase?.ModifiesFace != true));
            var resolvedDefaultB = defaultGuessB is not null ?
                FindEarliestSource(chain, defaultGuessB, true, x => !x.ModifiesBehavior) :
                // If we didn't even find a starting match, have to fall back to previous attempt.
                resolvedDefaultA;
            var resolvedDefaultRecord =
                chain.IndexOf(resolvedDefaultB.PluginName) > chain.IndexOf(resolvedDefaultA.PluginName) ?
                    resolvedDefaultB : resolvedDefaultA;

            return new(resolvedDefaultRecord.PluginName, resolvedFaceRecord.PluginName);
        }

        public bool IsLikelyOverhaul(string pluginName)
        {
            return overhaulNames.Contains(pluginName);
        }

        public void Receive(LoadOrderAnalysis analysis)
        {
            baseNames = analysis.Plugins
                .Where(x => x.IsBaseGame)
                .Select(x => x.FileName)
                .ToHashSet(StringComparer.CurrentCultureIgnoreCase);
            overhaulNames = InferOverhaulNames(analysis).ToHashSet(StringComparer.CurrentCultureIgnoreCase);
            npcChains = analysis
                .ExtractChains<NpcAnalysis>(RecordType.Npc)
                .ToDictionary(x => x.Key, RecordKeyComparer.Default);
        }

        private static NpcComparison? FindEarliestComparison(
            NpcAnalysis npc, Predicate<NpcComparison> predicate, bool alwaysCheckPreviousOverride)
        {
            // "Earliest" in this context means first in the load order, i.e. lowest priority. We start from the base
            // and move down the list.
            if (npc.ComparisonToBase is not null && predicate(npc.ComparisonToBase))
                return npc.ComparisonToBase;
            foreach (var comparison in npc.ComparisonToMasters)
            {
                if (comparison != npc.ComparisonToBase &&
                    (!alwaysCheckPreviousOverride || comparison != npc.ComparisonToPreviousOverride) &&
                    predicate(comparison))
                // then
                    return comparison;
            }
            if (alwaysCheckPreviousOverride && npc.ComparisonToPreviousOverride is not null &&
                predicate(npc.ComparisonToPreviousOverride))
                // then
                return npc.ComparisonToPreviousOverride;

            return null;
        }

        private Sourced<NpcAnalysis> FindEarliestSource(
            RecordAnalysisChain<NpcAnalysis> chain, Sourced<NpcAnalysis> referenceRecord,
            bool alwaysCheckPreviousOverride, Predicate<NpcComparison> sourcePredicate,
            Predicate<Sourced<NpcAnalysis>>? breakPredicate = null)
        {
            var currentRecord = referenceRecord;
            while (true)
            {
                if (!currentRecord.Analysis.IsOverride || currentRecord == chain[0])
                    break;
                if (breakPredicate is not null && breakPredicate(currentRecord))
                    return currentRecord;
                var match =
                    FindEarliestComparison(currentRecord.Analysis, sourcePredicate, alwaysCheckPreviousOverride);
                if (match is not null &&
                    // Guard against conditions that should all be impossible but would cause serious problems (crashes,
                    // infinite loops) if broken.
                    !string.IsNullOrEmpty(match.PluginName) &&
                    match.PluginName != currentRecord.PluginName &&
                    chain.Contains(match.PluginName))
                    // then
                    currentRecord = chain[match.PluginName]!; // Not null, since we checked Contains()
                else
                    break;
            }
            return currentRecord;
        }

        private static IEnumerable<string> InferOverhaulNames(LoadOrderAnalysis analysis)
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
            const double newNpcPercentThreshold = 0.5;
            var pluginStats = analysis.Plugins
                .Where(p => !p.IsBaseGame && p.Groups.ContainsKey(RecordType.Npc))
                .Select(p => new { p.FileName, Npcs = p.Groups[RecordType.Npc].Records.OfType<NpcAnalysis>().ToList() })
                .Select(p => new
                {
                    p.FileName,
                    TotalNpcCount = p.Npcs.Count,
                    NewNpcCount = p.Npcs.Count(x => !x.IsOverride),
                    FaceModifierCount = p.Npcs.Count(x => x.ComparisonToBase?.ModifiesFace ?? false),
                    BehaviorlessFaceModifierCount = p.Npcs.Count(x =>
                        (x.ComparisonToBase?.ModifiesFace ?? false) &&
                        // "Behaviorless" in this context means "inherits behavior from another mod". An NPC overhaul
                        // may be based on a foundation like USSEP and will therefore have different behavior from the
                        // vanilla origin (which is also a "master"). However, a truly new behavior mod, or a patch for
                        // multiple behavior mods, should always have distinct behavior from each dependency.
                        x.ComparisonToMasters.Any(x => !x.ModifiesBehavior)),
                });
            return pluginStats
                .Where(p => p.FaceModifierCount > 0)
                // Mods that add several new NPCs are probably expansions or follower mods, not overhauls. They may also
                // modify a few existing NPCs. This can be a good heuristic as long as the numbers are large enough to
                // make an informed decision, i.e. not just one or two of each.
                .Where(p => p.TotalNpcCount < 3 || (double)p.NewNpcCount / p.TotalNpcCount < newNpcPercentThreshold)
                .Where(p =>
                    p.FaceModifierCount == p.TotalNpcCount ||
                    (p.BehaviorlessFaceModifierCount >= p.FaceModifierCount * behaviorlessModifierPercentThreshold))
                .Select(p => p.FileName);
        }
    }
}
