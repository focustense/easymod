namespace Focus.ModManagers
{
    public class ModSearchResult
    {
        public string? ArchiveName { get; private init; }
        public ModComponentInfo ModComponent { get; private init; }
        public IModLocatorKey ModKey => ModComponent.ModKey;
        public string RelativePath { get; private init; }

        public ModSearchResult(
            ModComponentInfo component, string relativePath, string? archiveName = null)
        {
            ModComponent = component;
            RelativePath = relativePath;
            ArchiveName = archiveName;
        }
    }
}
