using Focus.Analysis.Records;
using Focus.Apps.EasyNpc.Profiles;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Reports
{
    public class InvalidReferenceViewModel
    {
        public string EditorId => npc.EditorId;
        public IRecordKey Key => npc;
        public string Name => npc.Name;
        public IReadOnlyList<ReferencePathViewModel> Paths { get; private init; }
        public string PluginName => option.PluginName;

        private readonly Npc npc;
        private readonly NpcOption option;

        public InvalidReferenceViewModel(Npc npc, NpcOption option)
        {
            this.npc = npc;
            this.option = option;

            Paths = option.Analysis.InvalidPaths.Select(p => new ReferencePathViewModel(p)).ToList().AsReadOnly();
        }
    }

    public class InvalidReferencesViewModel
    {
        public delegate InvalidReferencesViewModel Factory(Profile profile);

        public IReadOnlyList<InvalidReferenceViewModel> Items { get; private init; }

        public InvalidReferencesViewModel(IGameSettings gameSettings, Profile profile)
        {
            Items = profile.Npcs
                .AsParallel()
                .SelectMany(npc => npc.Options.Select(option => (npc, option)))
                .Where(x => x.option.HasInvalidPaths)
                .Select(x => new InvalidReferenceViewModel(x.npc, x.option))
                .OrderByLoadOrder(x => x.Key, gameSettings.PluginLoadOrder)
                .ToList()
                .AsReadOnly();
        }
    }

    public class ReferenceInfoViewModel
    {
        public string? EditorId => info.EditorId;
        public bool Exists { get; private init; }
        public string InfoText => info.ToString();
        public IRecordKey Key => info.Key;
        public RecordType Type => info.Type;

        private readonly ReferenceInfo info;

        public ReferenceInfoViewModel(ReferenceInfo info, bool exists)
        {
            this.info = info;
            Exists = exists;
        }
    }

    public class ReferencePathViewModel
    {
        public IEnumerable<ReferenceInfoViewModel> References => path.References
            .Select((x, i) => new ReferenceInfoViewModel(x, i < (path.References.Count - 1)));

        private readonly ReferencePath path;

        public ReferencePathViewModel(ReferencePath path)
        {
            this.path = path;
        }
    }
}
