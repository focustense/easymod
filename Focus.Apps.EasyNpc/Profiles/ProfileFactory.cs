using Focus.Analysis.Execution;
using Focus.Analysis.Plugins;
using Focus.Analysis.Records;
using Focus.ModManagers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Profiles
{
    public interface IProfileFactory
    {
        Profile CreateNew(LoadOrderAnalysis analysis);
        Profile RestoreSaved(
            LoadOrderAnalysis analysis, out IReadOnlyList<NpcRestorationFailure> failures,
            out IReadOnlyList<Npc> newNpcs);
    }

    public class ProfileFactory : IProfileFactory
    {
        private readonly ILogger log;
        private readonly IModRepository modRepository;
        private readonly ISuspendableProfileEventLog profileEventLog;
        private readonly IProfilePolicy policy;

        public ProfileFactory(
            IProfilePolicy policy, IModRepository modRepository, ISuspendableProfileEventLog profileEventLog,
            ILogger log)
        {
            this.log = log;
            this.modRepository = modRepository;
            this.policy = policy;
            this.profileEventLog = profileEventLog;
        }

        public Profile CreateNew(LoadOrderAnalysis analysis)
        {
            return Create(analysis, policy, npc => npc.ApplyPolicy(true, true));
        }

        public Profile RestoreSaved(
            LoadOrderAnalysis analysis, out IReadOnlyList<NpcRestorationFailure> failures,
            out IReadOnlyList<Npc> newNpcs)
        {
            // When restoring a profile, what we want to do is pause logging during the restore phase so that we don't
            // add redundant entries; but with additional actions taken for NPCs that either couldn't be correctly
            // restored (revert to policy defaults, without logging) or who aren't in the profile at all (initialize
            // with policy defaults and log those for next time).
            var failuresList = new List<NpcRestorationFailure>();
            var newNpcsList = new List<Npc>();
            var newestEvents = profileEventLog
                .GroupBy(x => (new RecordKey(x), x.Field))
                .Select(g => (g.Key.Item1, g.Key.Field, g.Last()))
                .ToDictionary(x => (x.Item1, x.Field), x => x.Item3);
            profileEventLog.Suspend();
            Profile profile;
            try
            {
                profile = Create(analysis, policy, npc =>
                {
                    var npcKey = new RecordKey(npc);
                    var isDefaultPluginInvalid =
                        newestEvents.TryGetValue((npcKey, NpcProfileField.DefaultPlugin), out var defaultPluginEvent) &&
                        npc.SetDefaultOption(defaultPluginEvent.NewValue) == Npc.ChangeResult.Invalid;
                    var isFacePluginInvalid =
                        newestEvents.TryGetValue((npcKey, NpcProfileField.FacePlugin), out var facePluginEvent) &&
                        npc.SetFaceOption(facePluginEvent.NewValue) == Npc.ChangeResult.Invalid;
                    // Face mods are now "facegen overrides", so we only care about (want to restore) this setting if it
                    // actually looks like an override - i.e. if the chosen mod exists and does not include any of the
                    // plugins in the record chain.
                    var isFaceModInvalid = false;
                    if (newestEvents.TryGetValue((npcKey, NpcProfileField.FaceMod), out var faceModEvent) &&
                        !string.IsNullOrEmpty(faceModEvent.NewValue))
                    {
                        var faceMod = ModLocatorKey.TryParse(faceModEvent.NewValue, out var key) ?
                            modRepository.FindByKey(key) : modRepository.GetByName(faceModEvent.NewValue);
                        var isFaceGenOverride =
                            faceMod is not null &&
                            !npc.Options.Any(x => modRepository.ContainsFile(faceMod, x.PluginName, false));
                        isFaceModInvalid =
                            isFaceGenOverride && npc.SetFaceMod(faceModEvent.NewValue) == Npc.ChangeResult.Invalid;
                    }
                    var allEvents = new[] { defaultPluginEvent, facePluginEvent, faceModEvent }.NotNull();
                    if (!allEvents.Any())
                        newNpcsList.Add(npc);
                    else if (isDefaultPluginInvalid || isFacePluginInvalid || isFaceModInvalid)
                    {
                        failuresList.Add(
                            new(npc, allEvents, isDefaultPluginInvalid, isFacePluginInvalid, isFaceModInvalid));
                    }
                });

                // Failure processing still happens with event log suspended. User may want to correct these errors by
                // restarting the app with correct plugins/mods enabled/installed.
                foreach (var failure in failuresList)
                {
                    var setupAttributes = policy.GetSetupRecommendation(failure.Npc);
                    if (failure.IsDefaultPluginInvalid)
                        failure.Npc.SetDefaultOption(setupAttributes.DefaultPluginName, asFallback: true);
                    if (failure.IsFacePluginInvalid)
                        failure.Npc.SetFaceOption(setupAttributes.FacePluginName, asFallback: true);
                    // Invalid face mod can be effectively ignored. If face plugin was invalid, then face mod got reset
                    // along with it, and if face plugin was NOT invalid, then doing nothing means reverting to default
                    // mod matching based on the plugin and not applying any facegen override.
                }
            }
            finally
            {
                profileEventLog.Resume();
            }
            foreach (var newNpc in newNpcsList)
                newNpc.ApplyPolicy(true, true, true);
            failures = failuresList.AsReadOnly();
            newNpcs = newNpcsList.AsReadOnly();
            return profile;
        }

        private Profile Create(LoadOrderAnalysis analysis, IProfilePolicy policy, Action<Npc> defaultAction)
        {
            var baseGamePluginNames = analysis.Plugins
                .Where(x => x.IsBaseGame)
                .Select(x => x.FileName)
                .ToHashSet(StringComparer.CurrentCultureIgnoreCase);
            var npcs = analysis
                .ExtractChains<NpcAnalysis>(RecordType.Npc)
                .AsParallel()
                .Where(x =>
                    x.Master.CanUseFaceGen && !x.Master.IsChild && !x.Master.IsAudioTemplate &&
                    // Template NPCs that are based on another NPC should be included but treated as "read only".
                    // If ALL POSSIBILITIES point only to Leveled NPC or unknown/invalid (not standard NPC) targets,
                    // then there is effectively nothing useful we can do with it and it should be excluded entirely.
                    x.Any(r =>
                        r.Analysis.TemplateInfo is null ||
                        r.Analysis.TemplateInfo.TargetType == NpcTemplateTargetType.Npc))
                .Tap(LogInvalidReferences)
                .Select(x => new Npc(x, baseGamePluginNames, modRepository, profileEventLog, policy))
                .Where(x => x.HasAvailableFaceCustomizations)
                .Tap(defaultAction);
            return new Profile(npcs);
        }

        private void LogInvalidReferences(RecordAnalysisChain<NpcAnalysis> chain)
        {
            foreach (var record in chain)
            {
                if (record.Analysis.InvalidPaths.Count == 0)
                    continue;
                log.Warning(
                    "Record {recordKey} in plugin {pluginName} contains invalid references:\n{referenceList:l}",
                    chain.Key, record.PluginName,
                    string.Join("\n", record.Analysis.InvalidPaths.Select(p => $"  {p}")));
            }
        }
    }
}
