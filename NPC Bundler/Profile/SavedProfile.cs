using System;
using System.Collections.Generic;
using System.IO;

namespace Focus.Apps.EasyNpc.Profile
{
    public class SavedProfile
    {
        public static SavedProfile LoadFromFile(string fileName)
        {
            using var stream = File.OpenRead(fileName);
            return LoadFromStream(stream);
        }

        public static SavedProfile LoadFromStream(Stream stream)
        {
            var npcs = new List<SavedNpcConfiguration>();
            using var reader = new StreamReader(stream);
            string line;
            while ((line = reader.ReadLine()) != null)
                npcs.Add(SavedNpcConfiguration.DeserializeFromString(line));
            return new SavedProfile { Npcs = npcs };
        }

        public IList<SavedNpcConfiguration> Npcs { get; init; } = new List<SavedNpcConfiguration>();

        public void SaveToFile(string fileName)
        {
            using var stream = File.Create(fileName);
            SaveToStream(stream);
        }

        public void SaveToStream(Stream stream)
        {
            using var writer = new StreamWriter(stream);
            foreach (var npc in Npcs)
                writer.WriteLine(npc.SerializeToString());
            writer.Flush();
        }
    }

    public class SavedNpcConfiguration
    {
        public static SavedNpcConfiguration DeserializeFromString(string serialized)
        {
            var sides = serialized.Split('=', 2);
            if (sides.Length != 2)
                return null;
            var lhs = sides[0].Split('#', 2);
            if (lhs.Length != 2)
                return null;
            var rhs = sides[1].Split('|');
            return new SavedNpcConfiguration
            {
                BasePluginName = lhs[0],
                LocalFormIdHex = lhs[1],
                DefaultPluginName = rhs.Length > 0 ? rhs[0] : "",
                FacePluginName = rhs.Length > 1 ? rhs[1] : "",
                FaceModName = rhs.Length > 2 ? rhs[2] : ""
            };
        }

        public string BasePluginName { get; init; }
        public string LocalFormIdHex { get; init; }
        public string DefaultPluginName { get; init; }
        public string FaceModName { get; init; }
        public string FacePluginName { get; init; }

        public string SerializeToString()
        {
            var values = new[] { DefaultPluginName, FacePluginName, FaceModName };
            return $"{BasePluginName}#{LocalFormIdHex}={string.Join('|', values)}";
        }
    }
}