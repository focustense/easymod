using MessagePack;
using System;
using System.Collections.Generic;

namespace Focus.ModManagers.Vortex
{
    public interface IReadOnlyDeploymentManifest
    {
        string DeploymentMethod { get; }
        ulong? DeploymentTimeMs { get; }
        IEnumerable<IReadOnlyDeployedFile> Files { get; }
        string GameId { get; }
        string Instance { get; }
        string StagingPath { get; }
        string TargetPath { get; }
        uint Version { get; }

        string ToJson();
    }

    [MessagePackObject]
    public class DeploymentManifest : IReadOnlyDeploymentManifest
    {
        [Key("deploymentMethod")]
        public string DeploymentMethod { get; set; } = string.Empty;

        [Key("deploymentTime")]
        public ulong? DeploymentTimeMs { get; set; }

        [Key("files")]
        public List<DeployedFile> Files { get; set; } = new();

        [Key("gameId")]
        public string GameId { get; set; } = string.Empty;

        [Key("instance")]
        public string Instance { get; set; } = string.Empty;

        [Key("stagingPath")]
        public string StagingPath { get; set; } = string.Empty;

        [Key("targetPath")]
        public string TargetPath { get; set; } = string.Empty;

        [Key("version")]
        public uint Version { get; set; }

        [IgnoreMember]
        IEnumerable<IReadOnlyDeployedFile> IReadOnlyDeploymentManifest.Files => Files;

        string IReadOnlyDeploymentManifest.ToJson()
        {
            return MessagePackSerializer.SerializeToJson(this);
        }
    }
}
