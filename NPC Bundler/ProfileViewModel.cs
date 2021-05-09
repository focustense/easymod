using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPC_Bundler
{
    public class ProfileViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [DependsOn("NpcConfigurations", "OnlyFaceOverrides", "ShowDlcOverrides", "ShowSinglePluginOverrides")]
        public IEnumerable<NpcConfiguration> DisplayedNpcs
        {
            get {
                var minOverrideCount = ShowSinglePluginOverrides ? 1 : 2;
                return NpcConfigurations
                    .Where(x => x.GetOverrideCount(ShowDlcOverrides, !OnlyFaceOverrides) >= minOverrideCount);
            }
        }

        public bool OnlyFaceOverrides { get; set; }
        public bool ShowDlcOverrides { get; set; }
        public bool ShowSinglePluginOverrides { get; set; } = true;

        protected List<NpcConfiguration> NpcConfigurations { get; init; }

        public ProfileViewModel(IEnumerable<Npc> npcs)
        {
            NpcConfigurations = npcs
                .Where(npc => npc.Overrides.Count > 0)
                .Select(npc => new NpcConfiguration(npc))
                .ToList();
        }
    }

    public class NpcConfiguration : INotifyPropertyChanged
    {
        private static readonly HashSet<string> DlcPluginNames = new HashSet<string>(
            new[] { "Update.esm", "Dawnguard.esm", "Dragonborn.esm", "HearthFires.esm" },
            StringComparer.OrdinalIgnoreCase);

        public event PropertyChangedEventHandler PropertyChanged;

        public string BasePluginName => npc.BasePluginName;
        public string EditorId => npc.EditorId;
        public string ExtendedFormId => $"{BasePluginName}#{LocalFormIdHex}";
        public string LocalFormIdHex => npc.LocalFormIdHex;
        public string Name => npc.Name;
        public string OverridePluginName { get; set; }

        private readonly Npc npc;

        public NpcConfiguration(Npc npc)
        {
            this.npc = npc;
            OverridePluginName = npc.Overrides.LastOrDefault().PluginName;
        }

        public int GetOverrideCount(bool includeDlc, bool includeNonFaces)
        {
            return npc.Overrides
                .Where(x => includeDlc || !DlcPluginNames.Contains(x.PluginName))
                .Where(x => includeNonFaces || x.faceData != null)
                .Count();
        }
    }
}
