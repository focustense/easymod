using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Apps.EasyNpc.GameData.Records;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Focus.Apps.EasyNpc.Profile
{
    public class NpcConfiguration<TKey> : INotifyPropertyChanged
        where TKey : struct
    {
        private static readonly HashSet<string> DlcPluginNames = new HashSet<string>(
            new[] { "Update.esm", "Dawnguard.esm", "Dragonborn.esm", "HearthFires.esm" },
            StringComparer.OrdinalIgnoreCase);

        public event Action FaceModChanged;
        public event EventHandler<ProfileEvent> ProfilePropertyChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public string BasePluginName => npc.BasePluginName;
        public NpcOverrideConfiguration<TKey> DefaultConfiguration => defaultConfig;
        public string DefaultPluginName => defaultConfig?.PluginName;
        public string DescriptiveLabel => $"'{Name}' ({BasePluginName} - {EditorId})";
        public string EditorId => npc.EditorId;
        public string ExtendedFormId => $"{BasePluginName}#{LocalFormIdHex}";
        public NpcOverrideConfiguration<TKey> FaceConfiguration => faceConfig;
        public string FaceModName { get; private set; }
        public string FacePluginName => faceConfig?.PluginName;
        public TKey Key => npc.Key;
        public bool IsFemale => npc.IsFemale;
        public string LocalFormIdHex => npc.LocalFormIdHex;
        public IReadOnlyList<NpcOverrideConfiguration<TKey>> Overrides { get; init; }
        public string Name => npc.Name;

        private readonly Func<NpcOverrideConfiguration<TKey>, int> indexOfOverride;
        private readonly IModPluginMapFactory modPluginMapFactory;
        private readonly INpc<TKey> npc;
        private readonly IProfileRuleSet ruleSet;

        private NpcOverrideConfiguration<TKey> defaultConfig;
        private NpcOverrideConfiguration<TKey> faceConfig;

        public NpcConfiguration(INpc<TKey> npc, IModPluginMapFactory modPluginMapFactory, IProfileRuleSet ruleSet)
        {
            this.modPluginMapFactory = modPluginMapFactory;
            this.npc = npc;
            this.ruleSet = ruleSet;
            var overrides = GetOverrides().ToList();
            Overrides = overrides.AsReadOnly();
            // It's really silly that we have to store this, but for no particular reason, IReadOnlyList<T> does not
            // have the IndexOf method.
            indexOfOverride = @override => overrides.IndexOf(@override);
            Reset(defaults: true, faces: true);
        }

        public int GetOverrideCount(bool includeDlc, bool includeNonFaces)
        {
            return npc.Overrides
                .Where(x => includeDlc || !DlcPluginNames.Contains(x.PluginName))
                .Where(x => includeNonFaces || x.FaceData != null)
                .Count();
        }

        public bool HasCustomizations()
        {
            return (DefaultPluginName != BasePluginName || FacePluginName != DefaultPluginName) &&
                (!FileStructure.IsDlc(DefaultPluginName) || !FileStructure.IsDlc(FacePluginName));
        }

        public bool HasFaceGenOverridesEnabled()
        {
            return faceConfig.HasFaceGenOverride &&
                // Base (master) is considered an "override" in order to mark that it has new face data, but for the
                // purposes of validation we don't want to treat it as a true override, as this program should never be
                // used to merge the original plugins, just the appearance overhauls.
                !string.Equals(FacePluginName, BasePluginName, StringComparison.OrdinalIgnoreCase);
        }

        public bool RequiresFacegenData()
        {
            return HasFaceGenOverridesEnabled() && !FileStructure.IsDlc(FacePluginName);
        }

        public void Reset(bool defaults = true, bool faces = true)
        {
            if (!defaults && !faces)
                return;

            var configDefaults = ruleSet.GetConfigurationDefaults(npc);
            if (defaults)
                SetDefaultPlugin(configDefaults.DefaultPlugin);
            if (faces)
                SetFacePlugin(configDefaults.FacePlugin, true);
        }

        public void RestoreFromProfileEvent(ProfileEvent e)
        {
            switch (e.Field)
            {
                // When a setter has optional side effects (e.g. face mod inferred from face plugin and vice versa),
                // we never want to activate these in restore mode, because there will be separate events for each,
                // and allowing the side effects could lead to an end state that's different from what was saved.
                case NpcProfileField.DefaultPlugin:
                    SetDefaultPlugin(e.NewValue);
                    break;
                case NpcProfileField.FaceMod:
                    SetFaceMod(e.NewValue, false);
                    break;
                case NpcProfileField.FacePlugin:
                    SetFacePlugin(e.NewValue, false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(e), $"Unexpected field type: {Enum.GetName(typeof(ProfileEvent), e.Field)}");
            }
        }

        public void SetDefaultPlugin(NpcOverrideConfiguration<TKey> overrideConfig)
        {
            var oldValue = defaultConfig?.PluginName;
            if (defaultConfig != null)
                defaultConfig.IsDefaultSource = false;
            if (overrideConfig == null)
                overrideConfig = Overrides[0];  // Use the master if nothing else is specified
            if (overrideConfig != null)
                overrideConfig.IsDefaultSource = true;
            defaultConfig = overrideConfig;
            LogProfileEvent(NpcProfileField.DefaultPlugin, oldValue, overrideConfig?.PluginName);
        }

        public void SetDefaultPlugin(string pluginName)
        {
            var foundOverride = FindOverride(pluginName);
            SetDefaultPlugin(foundOverride);
        }

        public void SetFaceMod(string modName, bool detectPlugin)
        {
            var oldValue = FaceModName;
            FaceModName = modName;
            FaceModChanged?.Invoke();
            LogProfileEvent(NpcProfileField.FaceMod, oldValue, modName);
            if (!detectPlugin)
                return;
            // Null/empty mod name is used as a special case to indicate default, which may not have a "mod" if vanilla
            if (string.IsNullOrEmpty(FaceModName))
                SetFacePlugin(npc.BasePluginName, false);
            // It should be rare for the same mugshot to correspond to two plugins *with that NPC* in the load order.
            // A single mod might provide several optional add-on plugins that all modify different NPCs (or do totally
            // different things altogether). If this really does happen, the most logical thing to do is to pick the
            // last plugin in the load order which belongs to that mod, which we can assume is the one responsible for
            // any conflict resolution between that mod/plugin and any other ones.
            var modPlugins = new HashSet<string>(
                modPluginMapFactory.DefaultMap().GetPluginsForMod(modName), StringComparer.OrdinalIgnoreCase);
            var lastMatchingPlugin = Overrides
                .Where(x => modPlugins.Contains(x.PluginName))
                .LastOrDefault();
            if (lastMatchingPlugin != null)
                SetFacePlugin(lastMatchingPlugin, false);
        }

        public void SetFacePlugin(NpcOverrideConfiguration<TKey> faceConfig, bool detectFaceMod)
        {
            var oldValue = this.faceConfig?.PluginName;
            if (this.faceConfig != null)
                this.faceConfig.IsFaceSource = false;
            if (faceConfig == null)
                faceConfig = Overrides[0];  // Use the master if nothing else is specified
            if (faceConfig != null)
                faceConfig.IsFaceSource = true;
            this.faceConfig = faceConfig;
            LogProfileEvent(NpcProfileField.FacePlugin, oldValue, faceConfig?.PluginName);
            if (!detectFaceMod || faceConfig == null)
                return;
            var lastMatchingModName = modPluginMapFactory.DefaultMap()
                .GetModsForPlugin(faceConfig.PluginName)
                .OrderBy(f => Mugshot.Exists(f, BasePluginName, LocalFormIdHex))
                .LastOrDefault();
            if (!string.IsNullOrEmpty(lastMatchingModName))
                SetFaceMod(lastMatchingModName, false);
        }

        public void SetFacePlugin(string pluginName, bool detectFaceMod)
        {
            var foundOverride = FindOverride(pluginName);
            SetFacePlugin(foundOverride, detectFaceMod);
        }

        protected void LogProfileEvent(NpcProfileField field, string oldValue, string newValue)
        {
            // For efficiency, don't log events that don't actually change anything.
            // These "re-sets" may have an effect in the UI, but don't affect the outcome or the profile.
            if (Equals(newValue, oldValue))
                return;
            ProfilePropertyChanged?.Invoke(this, new ProfileEvent
            {
                BasePluginName = BasePluginName,
                LocalFormIdHex = LocalFormIdHex,
                Timestamp = DateTime.Now,
                Field = field,
                OldValue = oldValue,
                NewValue = newValue,
            });
        }

        // This should rarely be called; it's mainly useful on first run, and on subsequent startups if there are new
        // NPCs (i.e. due to new mods) or new fields (i.e. code change).
        internal void EmitProfileEvents(IEnumerable<NpcProfileField> fields)
        {
            foreach (var field in fields)
                LogProfileEvent(field, null, ToFieldGetter(field)(this));
        }

        private NpcOverrideConfiguration<TKey> FindOverride(string pluginName)
        {
            if (string.IsNullOrEmpty(pluginName))
                return null;
            return Overrides.SingleOrDefault(x =>
                string.Equals(pluginName, x.PluginName, StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<NpcOverrideConfiguration<TKey>> GetOverrides()
        {
            var sources = npc.Overrides.Select(x => new NpcOverrideConfiguration<TKey>(this, x));
            // The base plugin is always a valid source for any kind of data, so we need to include that in the list.
            var master = new NpcOverrideConfiguration<TKey>(this, BasePluginName);
            return sources.Prepend(master);
        }

        private static Func<NpcConfiguration<TKey>, string> ToFieldGetter(NpcProfileField field) => field switch
        {
            NpcProfileField.DefaultPlugin => x => x.DefaultPluginName,
            NpcProfileField.FaceMod => x => x.FaceModName,
            NpcProfileField.FacePlugin => x => x.FacePluginName,
            _ => throw new ArgumentOutOfRangeException(
                nameof(field), $"Unexpected field type: {Enum.GetName(typeof(ProfileEvent), field)}")
        };
    }

    public class NpcOverrideConfiguration<TKey> : INotifyPropertyChanged
        where TKey : struct
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public bool HasBehaviorOverride { get; private init; }
        public bool HasBodyOverride { get; private init; }
        public bool HasFaceOverride { get; private init; }
        public bool HasFaceGenOverride { get; private init; }
        public bool HasOutfitOverride { get; private init; }
        public bool HasWig => Wig != null;
        public bool IsDefaultSource { get; set; }
        public bool IsFaceSource { get; set; }
        public bool IsHighlighted { get; set; }
        public bool IsSelected { get; set; }
        public string ItpoFileName { get; private init; }
        public string PluginName { get; private init; }
        public NpcWigInfo<TKey> Wig { get; private init; }

        private readonly NpcConfiguration<TKey> parentConfig;

        public NpcOverrideConfiguration(NpcConfiguration<TKey> parentConfig, string pluginName)
            : this(parentConfig)
        {
            PluginName = pluginName;
            HasBehaviorOverride = true;
            HasFaceOverride = true;
            HasFaceGenOverride = true;
            HasOutfitOverride = true;
        }

        public NpcOverrideConfiguration(NpcConfiguration<TKey> parentConfig, NpcOverride<TKey> @override)
            : this(parentConfig)
        {
            PluginName = @override.PluginName;
            HasBehaviorOverride = @override.ModifiesBehavior;
            HasBodyOverride = @override.ModifiesBody;
            HasFaceOverride = @override.ModifiesFace;
            HasFaceGenOverride = @override.FaceOverridesAffectFaceGen;
            HasOutfitOverride = @override.ModifiesOutfits;
            ItpoFileName = @override.ItpoPluginName;
            Wig = @override.Wig;
        }

        public NpcOverrideConfiguration(NpcConfiguration<TKey> parentConfig)
        {
            this.parentConfig = parentConfig;
        }

        public void SetAsDefault()
        {
            parentConfig.SetDefaultPlugin(this);
        }

        public void SetAsFace(bool detectMod = false)
        {
            parentConfig.SetFacePlugin(this, detectMod);
        }
    }
}