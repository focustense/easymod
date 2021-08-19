using Focus.Analysis.Plugins;
using Focus.Analysis.Records;
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

        public void ApplyPolicy(bool resetDefaultPlugin = false, bool resetFacePlugin = false)
        {
            if (!resetDefaultPlugin && !resetFacePlugin)
                return;
            var setupAttributes = policy.GetSetupRecommendation(this);
            if (resetDefaultPlugin)
                SetDefaultOption(setupAttributes.DefaultPluginName);
            if (resetFacePlugin)
                SetFaceOption(setupAttributes.FacePluginName);
        }

        public IEnumerable<string> GetFaceModNames()
        {
            if (FaceGenOverride is not null)
                return new[] { FaceGenOverride.Name };
            return modRepository.SearchForFiles(FaceOption.PluginName, false).Select(x => x.ModKey.Name);
        }

        public int GetOverrideCount(bool includeBaseGame, bool includeNonFaces)
        {
            return Options
                .Where(x => includeBaseGame || !x.IsBaseGame)
                .Where(x => includeNonFaces || x.Analysis.ComparisonToBase?.ModifiesFace != false)
                .Count();
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

        public ChangeResult SetDefaultOption(string pluginName)
        {
            var option = FindOption(pluginName);
            if (option is not null && option != DefaultOption)
            {
                LogProfileEvent(NpcProfileField.DefaultPlugin, DefaultOption.PluginName, option.PluginName);
                DefaultOption = option;
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
            if (mod is null)
                return ChangeResult.Invalid;
            if (modRepository.ContainsFile(mod, FaceOption.PluginName, false))
                return ChangeResult.Redundant;
            var bestOption = Options.LastOrDefault(x => modRepository.ContainsFile(mod, x.PluginName, false));
            return bestOption is not null ? SetFaceOption(bestOption.PluginName) : SetFaceGenOverride(mod);
        }

        public ChangeResult SetFaceOption(string pluginName, bool keepFaceGenMod = false)
        {
            var option = FindOption(pluginName);
            if (option is not null && option != FaceOption)
            {
                LogProfileEvent(NpcProfileField.FacePlugin, FaceOption.PluginName, option.PluginName);
                FaceOption = option;
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
