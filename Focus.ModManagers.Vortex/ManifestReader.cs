using MessagePack;
using System.IO;
using System.Threading.Tasks;

namespace Focus.ModManagers.Vortex
{
    public class ManifestReader
    {
        public async ValueTask<IReadOnlyDeploymentManifest> ReadDeploymentManifest(string path)
        {
            using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            // Using "return await" allows an implicit cast. We could eliminate the async/await, but it would require
            // an explicit cast, losing type safety.
            return await MessagePackSerializer.DeserializeAsync<DeploymentManifest>(fs);
        }
    }
}