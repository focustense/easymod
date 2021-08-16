using Focus.Analysis.Execution;
using Focus.Analysis.Plugins;
using Focus.Analysis.Records;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Focus.Apps.EasyNpc.Build
{
    enum TargetSex
    {
        Unknown = 0,
        Male,
        Female
    }

    public class SimpleWigResolver : IWigResolver, ILoadOrderAnalysisReceiver
    {
        private static readonly Dictionary<string, VanillaRace> termToRace = new(StringComparer.OrdinalIgnoreCase)
        {
            { "nord", VanillaRace.Nord },
            { "imperial", VanillaRace.Imperial },
            { "redguard", VanillaRace.Redguard },
            { "breton", VanillaRace.Breton },
            { "highelf", VanillaRace.HighElf },
            { "helf", VanillaRace.HighElf },
            { "darkelf", VanillaRace.DarkElf },
            { "delf", VanillaRace.DarkElf },
            { "woodelf", VanillaRace.WoodElf },
            { "welf", VanillaRace.WoodElf },
            { "orc", VanillaRace.Orc },
            { "elder", VanillaRace.Elder },
            // For groups of races, plural and singular, we just pick one - doesn't matter which because it either
            // matches the original hair's form list (if it included all those races) or doesn't.
            { "human", VanillaRace.Nord },
            { "humans", VanillaRace.Nord },
            { "elf", VanillaRace.HighElf },
            { "elves", VanillaRace.HighElf },
        };

        private static readonly Dictionary<string, TargetSex> termToSex = new(StringComparer.OrdinalIgnoreCase)
        {
            { "f", TargetSex.Female },
            { "female", TargetSex.Female },
            { "m", TargetSex.Male },
            { "male", TargetSex.Male },
        };

        private static readonly Regex raceRegex = new(
            @$"[\W_]({string.Join("|", termToRace.Keys)})[\W_]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex sexRegex = new(
            @$"[\W_]({string.Join("|", termToSex.Keys)})[\W_]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private ILookup<string, RecordAnalysisChain<HeadPartAnalysis>> hairs =
            Enumerable.Empty<RecordAnalysisChain<HeadPartAnalysis>>().ToLookup(x => "");

        public void Receive(LoadOrderAnalysis analysis)
        {
            var headPartChains = analysis.ExtractChains<HeadPartAnalysis>(RecordType.HeadPart);
            hairs = headPartChains
                .Where(x =>
                    x.Master.IsMainPart && x.Master.PartType == HeadPartType.Hair &&
                    !string.IsNullOrEmpty(x.Master.Name))
                .ToLookup(x => CanonicalizeName(x.Master.Name), StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<WigMatch> ResolveAll(IEnumerable<NpcWigInfo> wigs)
        {
            var alreadyChecked = new HashSet<IRecordKey>(RecordKeyComparer.Default);

            // As of this moment, the only examples to be tested come from High Poly NPC Overhaul 2.0 and its SMP mod.
            // These mods provide no useful information whatsoever in the "strongly-typed" metadata; every wig applies
            // to every race (even if it's actually race-specific), the editor IDs convey no information at all, and the
            // filenames are not equivalent to the originals - instead they point to copies that may have been renamed.
            //
            // Some examples:
            //     Wigs\Female\Desperate.nif
            //     Armor\KSWigsHDT\f\Desperate_1.nif
            //     Wigs\Female\HighLife_Elf.nif
            //     Armor\KSWigsHDT\f\Elves\HighLife.nif
            //     Wigs\Male\Joshua.nif
            //     Wigs\Male\Joshua_Elf.nif
            //     Wigs\Actors\Character\Character Assets\Hair\FemaleDarkElfHair06_shaved.nif
            //
            // Fortunately ("fortunately"), there's just barely enough information here to make a pretty good guess
            // about where the original hair came from. Some hacky regexing can tell us the intended sex and race for
            // the wig; no race generally means "anything human" and elf means "anything mer". HPNPC doesn't have any
            // other races at present although we can guess what they might look like if added. Finally, there are some
            // records that don't even have models, and reference empty texture sets, or reference models that are
            // apparently invalid, like the last example above. Since these NPCs would just end up bald anyway, the
            // default fallback of "I have no idea" should work fine.
            foreach (var wig in wigs)
            {
                if (alreadyChecked.Contains(wig.Key))
                    continue;
                var raceMatch = raceRegex.Match(wig.ModelName ?? string.Empty);
                // We have to assume human by default, because hair packs (almost?) always define variants with human
                // races as the default.
                var targetRace = raceMatch.Success ? termToRace[raceMatch.Groups[0].Value] : VanillaRace.Nord;
                var sexMatch = sexRegex.Match(wig.ModelName ?? string.Empty);
                // Hairs can have both sex flags, or neither sex flags, so "unknown" may be acceptable here. If a sex is
                // indicated then we'll restrict hair matches to it, but otherwise we'll just pick any.
                var targetSex = sexMatch.Success ? termToSex[sexMatch.Groups[0].Value] : TargetSex.Unknown;

                var canonicalName = Path.GetFileNameWithoutExtension(wig.ModelName) ?? string.Empty;
                if (canonicalName.EndsWith("_0") || canonicalName.EndsWith("_1"))   // Weight variants
                    canonicalName = canonicalName.Substring(0, canonicalName.Length - 2);
                canonicalName += '.';   // For regex stuff, we'll remove it later
                canonicalName = raceRegex.Replace(canonicalName, "");
                canonicalName = sexRegex.Replace(canonicalName, "");
                canonicalName = CanonicalizeName(canonicalName.TrimEnd('.'));
                var matchingHairKeys = hairs[canonicalName]
                    .Where(x => MatchesRace(x, targetRace) && MatchesSex(x, targetSex))
                    .Select(x => x.Key);
                yield return new WigMatch(wig.Key, matchingHairKeys);
                alreadyChecked.Add(wig.Key);
            }
        }

        private static string CanonicalizeName(string name)
        {
            return name.Replace(" ", "").Replace("_", "").Replace("-", "");
        }

        private static bool MatchesRace(RecordAnalysisChain<HeadPartAnalysis> hairChain, VanillaRace race)
        {
            if (hairChain.Master.ValidVanillaRaces.Count == 0)
                return true;
            return hairChain.Master.ValidVanillaRaces.Contains(race);
        }

        private static bool MatchesSex(RecordAnalysisChain<HeadPartAnalysis> hairChain, TargetSex sex)
        {
            return sex switch
            {
                TargetSex.Female => hairChain.Master.SupportsFemaleNpcs,
                TargetSex.Male => hairChain.Master.SupportsMaleNpcs,
                _ => true,
            };
        }
    }
}