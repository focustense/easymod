using System.IO.Abstractions;

namespace Focus.ModManagers
{
    public class ModPerComponentRepository : ComponentPerDirectoryModRepository<ComponentPerDirectoryConfiguration>
    {
        public ModPerComponentRepository(IFileSystem fs, IIndexedModRepository inner)
            : base(fs, inner)
        {
        }

        protected override IComponentResolver GetComponentResolver(ComponentPerDirectoryConfiguration config)
        {
            return new ModPerComponentResolver(config.RootPath);
        }
    }
}
