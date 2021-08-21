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
            var componentId = !string.IsNullOrEmpty(file?.Id) ? file.Id : componentName;
            var componentPath = Path.Combine(rootPath, componentName);
            // In an ideal world, the default for an unknown state would be disabled, not enabled. However, since the
            // old version of the extension didn't produce this flag, we have to assume the opposite, otherwise all
            // components will end up disabled.
            var isEnabled = file?.IsEnabled ?? true;
            return Task.FromResult(new ModComponentInfo(key, componentId, componentName, componentPath, isEnabled));
        }
    }
}
