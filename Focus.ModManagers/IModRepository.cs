using System.Collections.Generic;

namespace Focus.ModManagers
{
    public interface IModRepository : IEnumerable<ModInfo>
    {
        bool ContainsFile(ModInfo mod, string relativePath, bool includeArchives, bool includeDisabled = false);
        ModInfo? FindByComponentName(string componentName);
        ModInfo? FindByComponentPath(string componentPath);
        ModInfo? FindByKey(IModLocatorKey key);
        // If multiple mods have the same name, then the repository should present them all as a single mod with many
        // components. The components may have relevant user info, such as directory paths or source file names, but
        // there is no friendly way to present multiple mods of the same name to the user.
        ModInfo? GetByName(string modName);
        // In order for an "ID" to be an "ID", there should be exactly one result. In practice, some mods may not have
        // an ID, and the mod manager will give them a default ID of e.g. 0.
        // Implementations should be aware of this fact and return null when given a default ID. If there is more than
        // one match, return the first.
        ModInfo? GetById(string modId);
        IEnumerable<ModSearchResult> SearchForFiles(
            string relativePath, bool includeArchives, bool includeDisabled = false);
    }
}
