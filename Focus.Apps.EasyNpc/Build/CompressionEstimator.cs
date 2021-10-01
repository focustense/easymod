using Focus.Apps.EasyNpc.GameData.Files;
using System.IO;

namespace Focus.Apps.EasyNpc.Build
{
    public interface ICompressionEstimator
    {
        double EstimateCompressionRatio(string relativePath);
    }

    public class CompressionEstimator : ICompressionEstimator
    {
        private const double DefaultMeshRatio = 0.7;
        private const double DefaultTextureRatio = 0.5;
        private const double FaceGenMeshRatio = 0.5;
        private const double FaceTintRatio = 0.08;

        public double EstimateCompressionRatio(string relativePath)
        {
            return Path.GetExtension(relativePath).ToLower() switch
            {
                ".nif" => FileStructure.IsFaceGen(relativePath) ? FaceGenMeshRatio : DefaultMeshRatio,
                ".dds" => FileStructure.IsFaceGen(relativePath) ? FaceTintRatio : DefaultTextureRatio,
                _ => 1,
            };
        }
    }
}
