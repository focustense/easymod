using System.IO;
using System.Threading.Tasks;

namespace Focus.ModManagers.Vortex
{
    public class VortexComponentResolver : IComponentResolver
    {
        private readonly ModManifest manifest;
        private readonly string rootPath;

        public VortexComponentResolver(ModManifest manifest, string rootPath)
        {
            this.manifest = manifest;
            this.rootPath = rootPath;
        }

        public Task<ModComponentInfo> ResolveComponentInfo(string componentName)
        {
            var file = manifest.Files.GetOrDefault(componentName);
            var mod = file is not null && !string.IsNullOrEmpty(file.ModId) ?
                manifest.Mods.GetOrDefault(file.ModId) : null;
            var modId = file?.ModId ?? string.Empty;
            var modName = !string.IsNullOrEmpty(mod?.Name) ? mod.Name : componentName;
            var key = new ModLocatorKey(modId, modName);
            // TODO: Include the file ID (always exists) in the Vortex manifest and read that instead. Not critical
            // right now as we don't really use ComponentId.
            var componentId = componentName;
            var componentPath = Path.Combine(rootPath, componentName);
            // TODO: Include the enabled state from Vortex.
            return Task.FromResult(new ModComponentInfo(key, componentId, componentName, componentPath, true));
        }
    }
}
