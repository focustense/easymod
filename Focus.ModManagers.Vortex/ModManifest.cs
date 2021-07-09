using System;
using System.Collections.Generic;

namespace Focus.ModManagers.Vortex
{
    public class ModManifest
    {
        public Dictionary<string, FileInfo> Files { get; set; }
        public Dictionary<int, ModInfo> Mods { get; set; }
    }

    public class FileInfo
    {
        public int ModId { get; set; }
    }

    public class ModInfo
    {
        public string Name { get; set; }
    }
}