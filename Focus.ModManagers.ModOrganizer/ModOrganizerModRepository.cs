using System.IO.Abstractions;

namespace Focus.ModManagers.ModOrganizer
{
    public class ModOrganizerModRepository : ComponentPerDirectoryModRepository<ComponentPerDirectoryConfiguration>
    {
        private readonly IFileSystem fs;
        private readonly IModOrganizerConfiguration modOrganizerConfig;

        public ModOrganizerModRepository(IFileSystem fs, IModOrganizerConfiguration config, IIndexedModRepository inner)
            : base(fs, inner)
        {
            this.fs = fs;
            this.modOrganizerConfig = config;
        }

        protected override IComponentResolver GetComponentResolver(ComponentPerDirectoryConfiguration config)
        {
            return new ModOrganizerComponentResolver(fs, modOrganizerConfig, config.RootPath);
        }
    }
}
