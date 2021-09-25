using Focus.ModManagers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Profiles
{
    public class ProfileSection
    {
        public int Count => npcs.Count;
        public IEnumerable<Npc> Npcs => npcs.Values;

        private readonly Dictionary<IRecordKey, Npc> npcs = new();

        public ProfileSection(IEnumerable<Npc> npcs)
        {
            this.npcs = npcs.ToDictionary(x => new RecordKey(x), RecordKeyComparer.Default);
        }

        public bool TryGetNpc(IRecordKey key, [MaybeNullWhen(false)] out Npc npc)
        {
            return npcs.TryGetValue(key, out npc);
        }
    }

    public class Profile : ProfileSection
    {
        public ProfileSection Hidden { get; private init; }

        public Profile(IEnumerable<Npc> npcs)
            : base(npcs.Where(x => x.HasAvailableFaceCustomizations))
        {
            Hidden = new ProfileSection(npcs.Where(x => !x.HasAvailableFaceCustomizations));
        }

        public void Load(Stream stream)
        {
            var savedProfile = SavedProfile.LoadFromStream(stream);
            Parallel.ForEach(savedProfile.Npcs, x =>
            {
                var key = new RecordKey(x);
                if (!TryGetNpc(key, out var npc))
                    return;
                npc.SetDefaultOption(x.DefaultPluginName);
                npc.SetFaceOption(x.FacePluginName);
                if (!string.IsNullOrEmpty(x.FaceModName))
                    npc.SetFaceMod(x.FaceModName);
            });
        }

        public void Load(string path)
        {
            using var fs = File.OpenRead(path);
            Load(fs);
        }

        public bool TryResolveTemplate(Npc npc, [MaybeNullWhen(false)] out Npc targetNpc)
        {
            var visitedKeys = new HashSet<IRecordKey>(RecordKeyComparer.Default);
            return TryResolveTemplate(npc, out targetNpc, visitedKeys);
        }

        public void Save(Stream stream)
        {
            var savedNpcs = Npcs.Select(x => new SavedNpcConfiguration
            {
                BasePluginName = x.BasePluginName,
                LocalFormIdHex = x.LocalFormIdHex,
                DefaultPluginName = x.DefaultOption.PluginName,
                FacePluginName = x.FaceOption.PluginName,
                FaceModName = x.FaceGenOverride is not null ? new ModLocatorKey(x.FaceGenOverride).ToString() : null,
            });
            new SavedProfile(savedNpcs).SaveToStream(stream);
        }

        public void Save(string path)
        {
            using var fs = File.Create(path);
            Save(fs);
        }

        private bool TryResolveTemplate(
            Npc npc, [MaybeNullWhen(false)] out Npc recursiveTargetNpc, HashSet<IRecordKey> visitedKeys)
        {
            recursiveTargetNpc = null;
            if (npc.DefaultOption.Analysis.TemplateInfo?.InheritsTraits != true)
            {
                recursiveTargetNpc = npc;
                return true;
            }
            if (visitedKeys.Contains(npc))
                return false;
            visitedKeys.Add(npc);
            if (TryGetNpc(npc.DefaultOption.Analysis.TemplateInfo.Key, out var targetNpc))
                return TryResolveTemplate(targetNpc, out recursiveTargetNpc);
            return false;
        }
    }
}
