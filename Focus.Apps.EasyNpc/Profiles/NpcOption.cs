using Focus.Analysis.Plugins;
using Focus.Analysis.Records;

namespace Focus.Apps.EasyNpc.Profiles
{
    public class NpcOption : IRecordKey
    {
        public string BasePluginName => record.Analysis.BasePluginName;
        public string LocalFormIdHex => record.Analysis.LocalFormIdHex;

        public NpcAnalysis Analysis => record.Analysis;
        public bool HasWig => Analysis.WigInfo != null;
        public bool IsBaseGame { get; private init; }
        public string PluginName => record.PluginName;

        private readonly Sourced<NpcAnalysis> record;

        public NpcOption(Sourced<NpcAnalysis> record, bool isBaseGame)
        {
            this.record = record;
            IsBaseGame = isBaseGame;
        }
    }
}
