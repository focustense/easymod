using MessagePack;
using System;
using System.Collections.Generic;

namespace Focus.ModManagers.Vortex
{
    public interface IReadOnlyDeployedFile
    {
        ulong LastModifiedTimeMs { get; }
        IEnumerable<string> MergedMods { get; }
        string RelativePath { get; }
        string SourceFileName { get; }
        string TargetDirectory { get; }
    }

    [MessagePackObject]
    public class DeployedFile : IReadOnlyDeployedFile
    {
        [Key("time")]
        public ulong LastModifiedTimeMs { get; set; }

        [Key("merged")]
        public List<string> MergedMods { get; set; } = new();

        [Key("relPath")]
        public string RelativePath { get; set; } = string.Empty;

        [Key("source")]
        public string SourceFileName { get; set; } = string.Empty;

        [Key("target")]
        public string TargetDirectory { get; set; } = string.Empty;

        [IgnoreMember]
        IEnumerable<string> IReadOnlyDeployedFile.MergedMods => MergedMods;
    }
}
