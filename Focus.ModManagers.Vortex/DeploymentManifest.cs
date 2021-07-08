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
        public string DeploymentMethod { get; set; }

        [Key("deploymentTime")]
        public ulong? DeploymentTimeMs { get; set; }

        [Key("files")]
        public List<DeployedFile> Files { get; set; }

        [Key("gameId")]
        public string GameId { get; set; }

        [Key("instance")]
        public string Instance { get; set; }

        [Key("stagingPath")]
        public string StagingPath { get; set; }

        [Key("targetPath")]
        public string TargetPath { get; set; }

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
