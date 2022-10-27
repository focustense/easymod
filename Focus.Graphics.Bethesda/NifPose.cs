using Focus.Files;
using nifly;
using System.Collections.Immutable;
using System.Numerics;

namespace Focus.Graphics.Bethesda
{
    public static class NifPose
    {
        public static Pose FromFile(NifFile nif)
        {
            var filteredTransforms = new Dictionary<Bone, Matrix4x4>();
            foreach (var node in nif.GetNodes())
            {
                var nodeName = node.name.get();
                if (!nodeName.StartsWith("NPC "))
                    continue;
                using var transform = new MatTransform();
                nif.GetNodeTransformToGlobal(nodeName, transform);
                filteredTransforms.Add(new Bone(nodeName), transform.ToMat4());
            }
            return new Pose(filteredTransforms.ToImmutableDictionary());
        }

        public static Task<Pose> FromFileAsync(
            IAsyncFileProvider fileProvider, string fileName) =>
                FromFileAsync(NifLoader.FromProviderFile(fileProvider, fileName));

        public static async Task<Pose> FromFileAsync(FileLoaderAsync openFile)
        {
            using var nif = await openFile();
            return FromFile(nif);
        }
    }
}
