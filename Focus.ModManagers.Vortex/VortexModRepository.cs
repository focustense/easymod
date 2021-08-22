using System.IO.Abstractions;

namespace Focus.ModManagers.Vortex
{
    public class VortexModRepository : ComponentPerDirectoryModRepository<ComponentPerDirectoryConfiguration>
    {
        private readonly ModManifest manifest;

        public VortexModRepository(IFileSystem fs, IIndexedModRepository inner, ModManifest manifest)
            : base(fs, inner)
        {
            this.manifest = manifest;
        }

        protected override IComponentResolver GetComponentResolver(ComponentPerDirectoryConfiguration config)
        {
            return new VortexComponentResolver(manifest, config.RootPath);
        }
    }
}
