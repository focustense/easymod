using Focus.Apps.EasyNpc.Profile;
using Focus.ModManagers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Profiles
{
    public class Profile
    {
        public int Count => npcs.Count;
        public IEnumerable<NpcModel> Npcs => npcs.Values;

        private readonly Dictionary<RecordKey, NpcModel> npcs = new();

        public Profile(IEnumerable<NpcModel> npcs)
        {
            this.npcs = npcs.ToDictionary(x => new RecordKey(x));
        }

        public void Load(Stream stream)
        {
            var savedProfile = SavedProfile.LoadFromStream(stream);
            Parallel.ForEach(savedProfile.Npcs, x =>
            {
                var key = new RecordKey(x.BasePluginName, x.LocalFormIdHex);
                if (!npcs.TryGetValue(key, out var npc))
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
    }
}
