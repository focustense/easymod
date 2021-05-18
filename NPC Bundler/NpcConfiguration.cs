using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace NPC_Bundler
{
    public class NpcConfiguration : INotifyPropertyChanged
    {
        private static readonly HashSet<string> DlcPluginNames = new HashSet<string>(
            new[] { "Update.esm", "Dawnguard.esm", "Dragonborn.esm", "HearthFires.esm" },
            StringComparer.OrdinalIgnoreCase);

        public event Action FaceModChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public string BasePluginName => npc.BasePluginName;
        public string DefaultPluginName => defaultConfig?.PluginName;
        public string EditorId => npc.EditorId;
        public string ExtendedFormId => $"{BasePluginName}#{LocalFormIdHex}";
        public string FaceModName { get; private set; }
        public string FacePluginName => faceConfig?.PluginName;
        public uint FormId => npc.FormId;
        public string LocalFormIdHex => npc.LocalFormIdHex;
        public IReadOnlyList<NpcOverrideConfiguration> Overrides { get; init; }
        public string Name => npc.Name;

        private readonly Npc npc;

        private NpcOverrideConfiguration defaultConfig;
        private NpcOverrideConfiguration faceConfig;

        public NpcConfiguration(Npc npc, IReadOnlySet<string> masterNames)
        {
            this.npc = npc;
            var overrides = GetOverrides().ToList();
            Overrides = overrides.AsReadOnly();

            // We're never going to make perfect choices on the first run, but we can make a few assumptions.
            // First, that users have set up their NPC-mod load order to reflect their general preferences - i.e. mods
            // they'll use the most go last, mods they'll use scarcely go first.
            // Second, that they've used LOOT or manually organized their full load order so that all the compatibility
            // and conflict-resolution patches that *don't* deal with body/face mods go nearly last in the load order.
            //
            // This gives us an obviously flawed but still fairly good heuristic:
            // - Choose the last plugin in the load order _which modifies face data_ as the face source.
            // - Try to choose the very last plugin in the load order as the default source, except...
            //   - If that plugin is *also* the face source, then keep going up the list until we find either:
            //     (a) A master (ESM) plugin; these are rare, like USSEP, and we generally shouldn't override; or
            //     (b) Any plugin that modifies the NPC but does not modify the face, regardless of master.
            var faceOverride = overrides.LastOrDefault(x => x.HasFaceOverride);
            var defaultOverride = overrides.LastOrDefault();
            while (!string.IsNullOrEmpty(defaultOverride.ItpoFileName))
            {
                var itpoOverride = overrides.SingleOrDefault(x => x.PluginName == defaultOverride.ItpoFileName);
                if (itpoOverride != null)   // Should never be null but we still need to check
                    defaultOverride = itpoOverride;
            }
            if (defaultOverride == faceOverride && !masterNames.Contains(defaultOverride.PluginName))
            {
                for (int i = overrides.IndexOf(faceOverride) - 1; i >= 0; i--)
                {
                    var prevOverride = overrides[i];
                    if (masterNames.Contains(prevOverride.PluginName) || !prevOverride.HasFaceOverride)
                    {
                        defaultOverride = prevOverride;
                        break;
                    }
                }
            }
            SetDefaultPlugin(defaultOverride);
            SetFacePlugin(faceOverride, true);
        }

        public int GetOverrideCount(bool includeDlc, bool includeNonFaces)
        {
            return npc.Overrides
                .Where(x => includeDlc || !DlcPluginNames.Contains(x.PluginName))
                .Where(x => includeNonFaces || x.FaceData != null)
                .Count();
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

        public void SetDefaultPlugin(NpcOverrideConfiguration overrideConfig)
        {
            if (defaultConfig != null)
                defaultConfig.IsDefaultSource = false;
            if (overrideConfig != null)
                overrideConfig.IsDefaultSource = true;
            defaultConfig = overrideConfig;
        }

        public void SetFaceMod(string modName, bool detectPlugin)
        {
            FaceModName = modName;
            FaceModChanged?.Invoke();
            if (!detectPlugin || string.IsNullOrEmpty(FaceModName))
                return;
            // It should be rare for the same mugshot to correspond to two plugins *with that NPC* in the load order.
            // A single mod might provide several optional add-on plugins that all modify different NPCs (or do totally
            // different things altogether). If this really does happen, the most logical thing to do is to pick the
            // last plugin in the load order which belongs to that mod, which we can assume is the one responsible for
            // any conflict resolution between that mod/plugin and any other ones.
            var modPluginMap = ModPluginMap.ForDirectory(BundlerSettings.Default.ModRootDirectory);
            var modPlugins = new HashSet<string>(
                modPluginMap.GetPluginsForMod(modName), StringComparer.OrdinalIgnoreCase);
            var lastMatchingPlugin = Overrides
                .Where(x => modPlugins.Contains(x.PluginName))
                .LastOrDefault();
            if (lastMatchingPlugin != null)
                SetFacePlugin(lastMatchingPlugin, false);
        }

        public void SetFacePlugin(NpcOverrideConfiguration defaultConfig, bool detectFaceMod)
        {
            if (faceConfig != null)
                faceConfig.IsFaceSource = false;
            if (defaultConfig != null)
                defaultConfig.IsFaceSource = true;
            faceConfig = defaultConfig;
            if (!detectFaceMod || defaultConfig == null)
                return;
            var modPluginMap = ModPluginMap.ForDirectory(BundlerSettings.Default.ModRootDirectory);
            var lastMatchingModName = modPluginMap
                .GetModsForPlugin(defaultConfig.PluginName)
                .OrderBy(f => Mugshot.Exists(f, BasePluginName, LocalFormIdHex))
                .LastOrDefault();
            if (!string.IsNullOrEmpty(lastMatchingModName))
                SetFaceMod(lastMatchingModName, false);
        }

        private IEnumerable<NpcOverrideConfiguration> GetOverrides()
        {
            // The base plugin is always a valid source for any kind of data, so we need to include that in the list.
            var sources = npc.Overrides
                .Select(x => new { x.PluginName, x.HasFaceOverride, x.AffectsFaceGen, x.ItpoPluginName })
                .Prepend(new {
                    PluginName = BasePluginName,
                    HasFaceOverride = true,
                    AffectsFaceGen = true,
                    ItpoPluginName = (string?)null
                });
            return sources.Select(x =>
                new NpcOverrideConfiguration(this, x.PluginName, x.HasFaceOverride, x.AffectsFaceGen, x.ItpoPluginName));
        }
    }

    public class NpcOverrideConfiguration : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public bool HasFaceOverride { get; init; }
        public bool HasFaceGenOverride { get; init; }
        public bool IsDefaultSource { get; set; }
        public bool IsFaceSource { get; set; }
        public bool IsHighlighted { get; set; }
        public bool IsSelected { get; set; }
        public string ItpoFileName { get; init; }
        public string PluginName { get; init; }

        private readonly NpcConfiguration parentConfig;

        public NpcOverrideConfiguration(
            NpcConfiguration parentConfig, string pluginName, bool hasFaceOverride, bool hasFaceGenOverride,
            string? itpoFileName)
        {
            this.parentConfig = parentConfig;
            PluginName = pluginName;
            HasFaceOverride = hasFaceOverride;
            HasFaceGenOverride = hasFaceGenOverride;
            ItpoFileName = itpoFileName ?? string.Empty;
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