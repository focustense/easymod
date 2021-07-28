using System;
using System.Collections.Generic;

namespace Focus.ModManagers.Vortex
{
    public class ModManifest
    {
        public Dictionary<string, FileInfo> Files { get; set; } = new();
        public Dictionary<string, ModInfo> Mods { get; set; } = new();
        public string StagingDir { get; set; } = string.Empty;
    }

    public class FileInfo
    {
        public string? ModId { get; set; }
    }

    public class ModInfo
    {
        public string? Name { get; set; }
    }
}