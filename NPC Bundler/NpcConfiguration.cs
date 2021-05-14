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
        public string EditorId => npc.EditorId;
        public string ExtendedFormId => $"{BasePluginName}#{LocalFormIdHex}";
        public string FaceModName { get; private set; }
        public string LocalFormIdHex => npc.LocalFormIdHex;
        public IReadOnlyList<NpcOverrideConfiguration> Overrides { get; init; }
        public string Name => npc.Name;

        private readonly Npc npc;

        private NpcOverrideConfiguration defaultConfig;
        private NpcOverrideConfiguration faceConfig;

        public NpcConfiguration(Npc npc)
        {
            this.npc = npc;
            Overrides = GetOverrides().ToList().AsReadOnly();

            var defaultOverride = Overrides.LastOrDefault();
            SetDefaultPlugin(defaultOverride);
            SetFacePlugin(defaultOverride, true);
        }

        public int GetOverrideCount(bool includeDlc, bool includeNonFaces)
        {
            return npc.Overrides
                .Where(x => includeDlc || !DlcPluginNames.Contains(x.PluginName))
                .Where(x => includeNonFaces || x.FaceData != null)
                .Count();
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
                .Where(f => Mugshot.Exists(f, BasePluginName, LocalFormIdHex))
                .LastOrDefault();
            if (!string.IsNullOrEmpty(lastMatchingModName))
                SetFaceMod(lastMatchingModName, false);
        }

        private IEnumerable<NpcOverrideConfiguration> GetOverrides()
        {
            // The base plugin is always a valid source for any kind of data, so we need to include that in the list.
            var sources = npc.Overrides.Select(x => new { x.PluginName, x.HasFaceOverride })
                .Prepend(new { PluginName = BasePluginName, HasFaceOverride = true });
            return sources.Select(x => new NpcOverrideConfiguration(this, x.PluginName, x.HasFaceOverride));
        }
    }

    public class NpcOverrideConfiguration : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public bool HasFaceOverride { get; init; }
        public bool IsDefaultSource { get; set; }
        public bool IsFaceSource { get; set; }
        public bool IsHighlighted { get; set; }
        public bool IsSelected { get; set; }
        public string PluginName { get; init; }

        private readonly NpcConfiguration parentConfig;

        public NpcOverrideConfiguration(NpcConfiguration parentConfig, string pluginName, bool hasFaceOverride)
        {
            this.parentConfig = parentConfig;
            PluginName = pluginName;
            HasFaceOverride = hasFaceOverride;
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