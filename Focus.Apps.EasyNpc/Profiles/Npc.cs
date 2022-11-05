using Focus.Analysis.Plugins;
using Focus.Analysis.Records;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.ModManagers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;

namespace Focus.Apps.EasyNpc.Profiles
{
    public enum NpcChangeResult { OK, Invalid, Redundant }

    public interface INpc : INpcBasicInfo
    {
        bool CanCustomizeFace { get; }
        NpcOption DefaultOption { get; }
        IObservable<NpcOption> DefaultOptionObservable { get; }
        string DescriptiveLabel { get; }
        ModInfo? FaceGenOverride { get; }
        IObservable<ModInfo?> FaceGenOverrideObservable { get; }
        NpcOption FaceOption { get; }
        IObservable<NpcOption> FaceOptionObservable { get; }
        bool HasAvailableFaceCustomizations { get; }
        bool HasAvailableModdedFaceGens { get; }
        bool HasMissingPlugins { get; }
        bool HasUnmodifiedFaceTemplate { get; }
        string? MissingDefaultPluginName { get; }
        string? MissingFacePluginName { get; }
        IReadOnlyList<NpcOption> Options { get; }
        bool SupportsFaceGen { get; }

        void ApplyPolicy(bool resetDefaultPlugin = false, bool resetFacePlugin = false, bool alwaysLog = false);
        IEnumerable<string> GetFaceModNames();
        int GetOverrideCount(bool includeBaseGame);
        bool HasPluginOption(string pluginName);
        bool IsDefaultPlugin(string pluginName);
        bool IsFacePlugin(string pluginName);
        NpcChangeResult RevertToBaseGame();
        NpcChangeResult SetDefaultOption(string pluginName, bool asFallback = false);
        NpcChangeResult SetFaceMod(string modName);
        NpcChangeResult SetFaceOption(string pluginName, bool keepFaceGenMod = false, bool asFallback = false);
        void WriteToEventLog();
    }

    public class Npc : INpc
    {
        public string BasePluginName => records.Key.BasePluginName;
        public string DescriptiveLabel => $"{EditorId} '{Name}' ({LocalFormIdHex}:{BasePluginName})";
        public string EditorId => records.Master.EditorId;
        public bool HasAvailableFaceCustomizations =>
            Options.Any(x => x.Analysis.ComparisonToBase?.ModifiesFace == true) || HasAvailableModdedFaceGens;
        public bool HasAvailableModdedFaceGens => hasAvailableModdedFaceGens.Value;
        public string LocalFormIdHex => records.Key.LocalFormIdHex;
        public string Name => records.Master.Name;
        public bool SupportsFaceGen => records.Master.CanUseFaceGen;

        public bool CanCustomizeFace => DefaultOption.Analysis.TemplateInfo?.InheritsTraits != true;
        public NpcOption DefaultOption => defaultOption.Value;
        public IObservable<NpcOption> DefaultOptionObservable => defaultOption;
        public ModInfo? FaceGenOverride => faceGenOverride.Value;
        public IObservable<ModInfo?> FaceGenOverrideObservable => faceGenOverride;
        public NpcOption FaceOption => faceOption.Value;
        public IObservable<NpcOption> FaceOptionObservable => faceOption;
        public bool HasMissingPlugins =>
            !string.IsNullOrEmpty(MissingDefaultPluginName) || !string.IsNullOrEmpty(MissingFacePluginName);
        public bool HasUnmodifiedFaceTemplate { get; private init; }
        public bool IsFemale => records.Master.IsFemale;
        public string? MissingDefaultPluginName { get; private set; }
        public string? MissingFacePluginName { get; private set; }
        public IReadOnlyList<NpcOption> Options { get; private init; }

        private readonly BehaviorSubject<NpcOption> defaultOption;
        private readonly BehaviorSubject<ModInfo?> faceGenOverride = new(null);
        private readonly BehaviorSubject<NpcOption> faceOption;
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
                    .Any(x => !masterComponentNames.Contains(x.ModComponent.Name));
            }, true);

            Options = records
                .Select(x => new NpcOption(x, baseGamePluginNames.Contains(x.PluginName)))
                .ToList()
                .AsReadOnly();
            HasUnmodifiedFaceTemplate = CheckUnmodifiedFaceTemplate();

            // The last plugin in the load order is not a suitable default choice, despite this being the same behavior
            // as the game itself, without a patch.
            //
            // However, the constructor should be kept simple, fast, and predictable, so as to avoid issues like #66
            // (incomplete resets/profile corruption). The constructing parent can then choose to either configure
            // defaults based on some rule set - i.e. for first-time loads or manual resets - or load previously-saved
            // settings for that NPC. The only reason to set any option here at all is to avoid nulls.
            var lastOption = Options[^1];
            defaultOption = new(lastOption);
            faceOption = new(lastOption);
        }

        public void ApplyPolicy(bool resetDefaultPlugin = false, bool resetFacePlugin = false, bool alwaysLog = false)
        {
            if (!resetDefaultPlugin && !resetFacePlugin)
                return;
            var previousDefaultPlugin = DefaultOption.PluginName;
            var previousFacePlugin = FaceOption.PluginName;
            var setupAttributes = policy.GetSetupRecommendation(this);
            if (resetDefaultPlugin)
                if (SetDefaultOption(setupAttributes.DefaultPluginName) == NpcChangeResult.Redundant && alwaysLog)
                    LogProfileEvent(NpcProfileField.DefaultPlugin, previousDefaultPlugin, DefaultOption.PluginName);
            if (resetFacePlugin)
                if (SetFaceOption(setupAttributes.FacePluginName) == NpcChangeResult.Redundant && alwaysLog)
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
            return Options.Count(x => includeBaseGame || !x.IsBaseGame);
        }

        public bool HasPluginOption(string pluginName)
        {
            return Options.Any(x => x.PluginName.Equals(
                pluginName, StringComparison.CurrentCultureIgnoreCase));
        }

        public bool IsDefaultPlugin(string pluginName)
        {
            return DefaultOption.PluginName.Equals(pluginName, StringComparison.CurrentCultureIgnoreCase);
        }

        public bool IsFacePlugin(string pluginName)
        {
            return FaceOption.PluginName.Equals(pluginName, StringComparison.CurrentCultureIgnoreCase);
        }

        public NpcChangeResult RevertToBaseGame()
        {
            var option = Options
                .Where(x => x.IsBaseGame)
                .LastOrDefault();
            return option is not null ? SetFaceOption(option.PluginName) : NpcChangeResult.Invalid;
        }

        public NpcChangeResult SetDefaultOption(string pluginName, bool asFallback = false)
        {
            var option = FindOption(pluginName);
            if (option is not null && (option != DefaultOption || !string.IsNullOrEmpty(MissingDefaultPluginName)))
            {
                var oldPluginName = !string.IsNullOrEmpty(MissingDefaultPluginName) ?
                    MissingDefaultPluginName : DefaultOption.PluginName;
                if (!asFallback)
                    LogProfileEvent(NpcProfileField.DefaultPlugin, oldPluginName, option.PluginName);
                defaultOption.OnNext(option);
                if (!asFallback)
                    MissingDefaultPluginName = null;
                return NpcChangeResult.OK;
            }
            else if (option is null)
                MissingDefaultPluginName = pluginName;
            return option is null ? NpcChangeResult.Invalid : NpcChangeResult.Redundant;
        }

        public NpcChangeResult SetFaceMod(string modName)
        {
            var mod = ModLocatorKey.TryParse(modName, out var key) ?
                modRepository.FindByKey(key) : modRepository.GetByName(modName);
            if (mod is null)
                return NpcChangeResult.Invalid;
            var bestOption = Options.LastOrDefault(x => modRepository.ContainsFile(mod, x.PluginName, false));
            if (bestOption is null && !modRepository.ContainsFile(mod, FileStructure.GetFaceMeshFileName(this), true))
                return NpcChangeResult.Invalid;
            if ((FaceGenOverride is not null && FaceGenOverride.IncludesName(modName)) ||
                modRepository.ContainsFile(mod, FaceOption.PluginName, false))
                return NpcChangeResult.Redundant;
            return bestOption is not null ? SetFaceOption(bestOption.PluginName) : SetFaceGenOverride(mod);
        }

        public NpcChangeResult SetFaceOption(string pluginName, bool keepFaceGenMod = false, bool asFallback = false)
        {
            var option = FindOption(pluginName);
            if (option is not null && !option.HasErrors &&
                (option != FaceOption || FaceGenOverride is not null || !string.IsNullOrEmpty(MissingFacePluginName)))
            {
                var oldPluginName = !string.IsNullOrEmpty(MissingFacePluginName) ?
                    MissingFacePluginName : DefaultOption.PluginName;
                if (!asFallback)
                    LogProfileEvent(NpcProfileField.FacePlugin, oldPluginName, option.PluginName);
                faceOption.OnNext(option);
                if (!asFallback)
                    MissingFacePluginName = null;
                if (!keepFaceGenMod)
                    SetFaceGenOverride(null);
                return NpcChangeResult.OK;
            }
            else if (option is null)
                MissingFacePluginName = pluginName;
            return option is null ? NpcChangeResult.Invalid : NpcChangeResult.Redundant;
        }

        // Does NOT need to be called in normal usage - happens automatically. Only used when rewriting the autosave.
        public void WriteToEventLog()
        {
            LogProfileEvent(NpcProfileField.DefaultPlugin, null, DefaultOption.PluginName);
            LogProfileEvent(NpcProfileField.FacePlugin, null, FaceOption.PluginName);
            if (FaceGenOverride is not null)
                LogProfileEvent(NpcProfileField.FaceMod, null, new ModLocatorKey(FaceGenOverride).ToString());
        }

        private bool CheckUnmodifiedFaceTemplate()
        {
            if (Options.Count == 0)
                return false;
            RecordKey? previousTemplateKey = null;
            var keyComparer = RecordKeyComparer.Default;
            foreach (var option in Options)
            {
                if (option.Analysis.TemplateInfo is null || !option.Analysis.TemplateInfo.InheritsTraits)
                    return false;
                var currentTemplateKey = option.Analysis.TemplateInfo.Key;
                if (previousTemplateKey is not null && !keyComparer.Equals(previousTemplateKey, currentTemplateKey))
                    return false;
                previousTemplateKey = currentTemplateKey;
            }
            return true;
        }

        private NpcChangeResult SetFaceGenOverride(ModInfo? mod)
        {
            if (FaceGenOverride?.Name == mod?.Name)
                return NpcChangeResult.Redundant;
            var oldKey = FaceGenOverride is not null ? new ModLocatorKey(FaceGenOverride) : null;
            var newKey = mod is not null ? new ModLocatorKey(mod) : null;
            if (newKey != oldKey)
            {
                faceGenOverride.OnNext(mod);
                LogProfileEvent(NpcProfileField.FaceMod, oldKey?.ToString(), newKey?.ToString());
                return NpcChangeResult.OK;
            }
            return NpcChangeResult.Redundant;
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
