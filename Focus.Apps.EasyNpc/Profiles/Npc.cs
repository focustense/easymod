using Focus.Analysis.Plugins;
using Focus.Analysis.Records;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.ModManagers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Profiles
{
    public class Npc : INpcBasicInfo
    {
        public enum ChangeResult { OK, Invalid, Redundant }

        public string BasePluginName => records.Key.BasePluginName;
        public string DescriptiveLabel => $"{EditorId} '{Name}' ({LocalFormIdHex}:{BasePluginName})";
        public string EditorId => records.Master.EditorId;
        public bool HasAvailableFaceCustomizations =>
            Options.Any(x => x.Analysis.ComparisonToBase?.ModifiesFace == true) || HasAvailableModdedFaceGens;
        public bool HasAvailableModdedFaceGens => hasAvailableModdedFaceGens.Value;
        public string LocalFormIdHex => records.Key.LocalFormIdHex;
        public string Name => records.Master.Name;
        public bool SupportsFaceGen => records.Master.CanUseFaceGen;

        public bool CanCustomizeFace => FaceOption.Analysis.TemplateInfo?.InheritsTraits != true;
        public NpcOption DefaultOption { get; private set; }
        public ModInfo? FaceGenOverride { get; private set; }
        public NpcOption FaceOption { get; private set; }
        public bool HasMissingPlugins =>
            !string.IsNullOrEmpty(MissingDefaultPluginName) || !string.IsNullOrEmpty(MissingFacePluginName);
        public bool IsFemale => records.Master.IsFemale;
        public string? MissingDefaultPluginName { get; private set; }
        public string? MissingFacePluginName { get; private set; }
        public IReadOnlyList<NpcOption> Options { get; private init; }

        private readonly Lazy<bool> hasAvailableModdedFaceGens;
        private readonly IModRepository modRepository;
        private readonly IProfilePolicy policy;
        private readonly IProfileEventLog profileEventLog;
        private readonly RecordAnalysisChain<NpcAnalysis> records;

        public Npc(
            RecordAnalysisChain<NpcAnalysis> records, IReadOnlySet<string> baseGamePluginNames,
            IModRepository modRepository, IProfileEventLog profileEventLog, IProfilePolicy policy)
        {
            this.modRepository = modRepository;
            this.policy = policy;
            this.profileEventLog = profileEventLog;
            this.records = records;

            hasAvailableModdedFaceGens = new(() =>
            {
                var masterComponentNames = modRepository.SearchForFiles(records.Master.BasePluginName, false)
                    .Select(x => x.ModComponent.Name)
                    .ToHashSet(StringComparer.CurrentCultureIgnoreCase);
                var faceGenPath = FileStructure.GetFaceMeshFileName(this);
                // Vanilla BSAs aren't in the mod directory, so all results below are actually modded.
                return modRepository.SearchForFiles(faceGenPath, true)
                    .Where(x => !masterComponentNames.Contains(x.ModComponent.Name))
                    .Any();
            }, true);

            Options = records
                .Select(x => new NpcOption(x, baseGamePluginNames.Contains(x.PluginName)))
                .ToList()
                .AsReadOnly();

            // The last plugin in the load order is not a suitable default choice, despite this being the same behavior
            // as the game itself, without a patch.
            //
            // However, the constructor should be kept simple, fast, and predictable, so as to avoid issues like #66
            // (incomplete resets/profile corruption). The constructing parent can then choose to either configure
            // defaults based on some rule set - i.e. for first-time loads or manual resets - or load previously-saved
            // settings for that NPC. The only reason to set any option here at all is to avoid nulls.
            FaceOption = DefaultOption = Options[Options.Count - 1];
        }

        public void ApplyPolicy(bool resetDefaultPlugin = false, bool resetFacePlugin = false, bool alwaysLog = false)
        {
            if (!resetDefaultPlugin && !resetFacePlugin)
                return;
            var previousDefaultPlugin = DefaultOption.PluginName;
            var previousFacePlugin = FaceOption.PluginName;
            var setupAttributes = policy.GetSetupRecommendation(this);
            if (resetDefaultPlugin)
                if (SetDefaultOption(setupAttributes.DefaultPluginName) == ChangeResult.Redundant && alwaysLog)
                    LogProfileEvent(NpcProfileField.DefaultPlugin, previousDefaultPlugin, DefaultOption.PluginName);
            if (resetFacePlugin)
                if (SetFaceOption(setupAttributes.FacePluginName) == ChangeResult.Redundant && alwaysLog)
                    LogProfileEvent(NpcProfileField.FacePlugin, previousFacePlugin, FaceOption.PluginName);
        }

        public IEnumerable<string> GetFaceModNames()
        {
            if (FaceGenOverride is not null)
                return new[] { FaceGenOverride.Name };
            return modRepository.SearchForFiles(FaceOption.PluginName, false).Select(x => x.ModKey.Name);
        }

        public int GetOverrideCount(bool includeBaseGame)
        {
            return Options.Where(x => includeBaseGame || !x.IsBaseGame).Count();
        }

        public bool HasUnmodifiedFaceTemplate()
        {
            return Options
                .Select(x => x.Analysis.TemplateInfo?.InheritsTraits == true ? x.Analysis.TemplateInfo.Key : null)
                .NotNull()
                .Distinct(RecordKeyComparer.Default)
                .Count() == 1;
        }

        public bool IsDefaultPlugin(string pluginName)
        {
            return DefaultOption.PluginName.Equals(pluginName, StringComparison.CurrentCultureIgnoreCase);
        }

        public bool IsFacePlugin(string pluginName)
        {
            return FaceOption.PluginName.Equals(pluginName, StringComparison.CurrentCultureIgnoreCase);
        }

        public ChangeResult SetDefaultOption(string pluginName, bool asFallback = false)
        {
            var option = FindOption(pluginName);
            if (option is not null && (option != DefaultOption || !string.IsNullOrEmpty(MissingDefaultPluginName)))
            {
                var oldPluginName = !string.IsNullOrEmpty(MissingDefaultPluginName) ?
                    MissingDefaultPluginName : DefaultOption.PluginName;
                if (!asFallback)
                    LogProfileEvent(NpcProfileField.DefaultPlugin, oldPluginName, option.PluginName);
                DefaultOption = option;
                if (!asFallback)
                    MissingDefaultPluginName = null;
                return ChangeResult.OK;
            }
            else if (option is null)
                MissingDefaultPluginName = pluginName;
            return option is null ? ChangeResult.Invalid : ChangeResult.Redundant;
        }

        public ChangeResult SetFaceMod(string modName)
        {
            var mod = ModLocatorKey.TryParse(modName, out var key) ?
                modRepository.FindByKey(key) : modRepository.GetByName(modName);
            if (mod is null || !modRepository.ContainsFile(mod, FileStructure.GetFaceMeshFileName(this), true))
                return ChangeResult.Invalid;
            if ((FaceGenOverride is not null && FaceGenOverride.IncludesName(modName)) ||
                modRepository.ContainsFile(mod, FaceOption.PluginName, false))
                return ChangeResult.Redundant;
            var bestOption = Options.LastOrDefault(x => modRepository.ContainsFile(mod, x.PluginName, false));
            return bestOption is not null ? SetFaceOption(bestOption.PluginName) : SetFaceGenOverride(mod);
        }

        public ChangeResult SetFaceOption(string pluginName, bool keepFaceGenMod = false, bool asFallback = false)
        {
            var option = FindOption(pluginName);
            if (option is not null && !option.HasErrors &&
                (option != FaceOption || FaceGenOverride is not null || !string.IsNullOrEmpty(MissingFacePluginName)))
            {
                var oldPluginName = !string.IsNullOrEmpty(MissingFacePluginName) ?
                    MissingFacePluginName : DefaultOption.PluginName;
                if (!asFallback)
                    LogProfileEvent(NpcProfileField.FacePlugin, oldPluginName, option.PluginName);
                FaceOption = option;
                if (!asFallback)
                    MissingFacePluginName = null;
                if (!keepFaceGenMod)
                    SetFaceGenOverride(null);
                return ChangeResult.OK;
            }
            else if (option is null)
                MissingFacePluginName = pluginName;
            return option is null ? ChangeResult.Invalid : ChangeResult.Redundant;
        }

        // Does NOT need to be called in normal usage - happens automatically. Only used when rewriting the autosave.
        public void WriteToEventLog()
        {
            LogProfileEvent(NpcProfileField.DefaultPlugin, null, DefaultOption.PluginName);
            LogProfileEvent(NpcProfileField.FacePlugin, null, FaceOption.PluginName);
            if (FaceGenOverride is not null)
                LogProfileEvent(NpcProfileField.FaceMod, null, new ModLocatorKey(FaceGenOverride).ToString());
        }

        private ChangeResult SetFaceGenOverride(ModInfo? mod)
        {
            if (FaceGenOverride?.Name == mod?.Name)
                return ChangeResult.Redundant;
            var oldKey = FaceGenOverride is not null ? new ModLocatorKey(FaceGenOverride) : null;
            var newKey = mod is not null ? new ModLocatorKey(mod) : null;
            if (newKey != oldKey)
            {
                FaceGenOverride = mod;
                LogProfileEvent(NpcProfileField.FaceMod, oldKey?.ToString(), newKey?.ToString());
                return ChangeResult.OK;
            }
            return ChangeResult.Redundant;
        }

        private NpcOption? FindOption(string pluginName)
        {
            return Options.SingleOrDefault(x =>
                x.PluginName.Equals(pluginName, StringComparison.CurrentCultureIgnoreCase));
        }

        private void LogProfileEvent(NpcProfileField field, string? oldValue, string? newValue)
        {
            profileEventLog.Append(new()
            {
                BasePluginName = BasePluginName,
                LocalFormIdHex = LocalFormIdHex,
                Timestamp = DateTime.Now,
                Field = field,
                OldValue = oldValue,
                NewValue = newValue,
            });
        }
    }
}
