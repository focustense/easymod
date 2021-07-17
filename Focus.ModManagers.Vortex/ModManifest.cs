using System;
using System.Collections.Generic;

namespace Focus.ModManagers.Vortex
{
    public class ModManifest
    {
        public Dictionary<string, FileInfo> Files { get; set; }
        public Dictionary<string, ModInfo> Mods { get; set; }
        public string StagingDir { get; set; }
    }

    public class FileInfo
    {
        public string ModId { get; set; }
    }

    public class ModInfo
    {
        public string Name { get; set; }
    }
}