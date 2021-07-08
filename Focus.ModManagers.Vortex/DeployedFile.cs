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
        public List<string> MergedMods { get; set; }

        [Key("relPath")]
        public string RelativePath { get; set; }

        [Key("source")]
        public string SourceFileName { get; set; }

        [Key("target")]
        public string TargetDirectory { get; set; }

        [IgnoreMember]
        IEnumerable<string> IReadOnlyDeployedFile.MergedMods => MergedMods;
    }
}
