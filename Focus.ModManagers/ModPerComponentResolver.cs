using System.IO;
using System.Threading.Tasks;

namespace Focus.ModManagers
{
    public class ModPerComponentResolver : IComponentResolver
    {
        private readonly string rootDirectory;

        public ModPerComponentResolver(string rootDirectory)
        {
            this.rootDirectory = rootDirectory;
        }

        public Task<ModComponentInfo> ResolveComponentInfo(string componentName)
        {
            var key = new ModLocatorKey(componentName, componentName);
            var path = Path.Combine(rootDirectory, componentName);
            return Task.FromResult(new ModComponentInfo(key, componentName, componentName, path));
        }
    }
}
