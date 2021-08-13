using Focus.Files;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.ModManagers
{
    public interface IIndexedModRepository : IModRepository
    {
        Task ConfigureIndex(
            INotifyingBucketedFileIndex modIndex, string rootPath, IComponentResolver componentResolver);
    }

    public class IndexedModRepository : IIndexedModRepository, IDisposable
    {
        // For all names used here: Bucket names, component names, mod names.
        private static readonly StringComparer NameComparer = StringComparer.CurrentCultureIgnoreCase;

        private readonly ArchiveIndex archiveIndex;
        private readonly IArchiveProvider archiveProvider;
        private readonly Dictionary<string, HashSet<string>> bucketNamesToArchivePaths = new(NameComparer);

        private Dictionary<string, ModComponentInfo> bucketNamesToComponents = new(NameComparer);
        private Dictionary<string, string> componentNamesToBucketNames = new(NameComparer);
        private Dictionary<string, ModComponentInfo> componentNamesToComponents = new(NameComparer);
        private Dictionary<string, ModComponentInfo> componentPathsToComponents = new(PathComparer.Default);
        private IComponentResolver? componentResolver;
        private bool isDisposed;
        // Some components will not have a mod ID (e.g. installed from file, generated patches, etc.). Therefore, the
        // mods-to-buckets map will generally have an empty group with all the "standalone" ungroupable components.
        private Dictionary<string, HashSet<ModComponentInfo>> modIdsToComponents = new();
        // Storing a map of mod IDs to mod names helps ensure consistency, in case the component resolver manages to
        // return components with the same (non-empty) mod ID, but different names. The name map here should contain
        // the first non-empty name for that ID, as without additional information, there is no better algorithm for
        // choosing the name. (If one exists, the subclass should implement it *before* returning any mod key.)
        private Dictionary<string, string> modIdsToModNames = new();
        private INotifyingBucketedFileIndex modIndex = new EmptyFileIndex();
        // We don't have any guarantee that a mod name is unique. The name of a mod can change, and then possibly,
        // another mod takes its old name - but the same user can have both mods installed.
        // There isn't a lot we can do in this edge case that's sensible, so just keep track of one association, under
        // the assumption that in most cases there will only be one.
        private Dictionary<string, string> modNamesToModIds = new(NameComparer);
        private string rootPath = string.Empty;
        private Task setupTask = Task.CompletedTask;

        public IndexedModRepository(IArchiveProvider archiveProvider)
        {
            this.archiveProvider = archiveProvider;

            archiveIndex = new ArchiveIndex(archiveProvider);
        }

        public async Task ConfigureIndex(
            INotifyingBucketedFileIndex modIndex, string rootPath, IComponentResolver componentResolver)
        {
            var tcs = new TaskCompletionSource();
            setupTask = tcs.Task;

            UnwatchIndex();
            this.componentResolver = componentResolver;
            this.modIndex = modIndex;
            this.rootPath = rootPath;

            var bucketComponents = await componentResolver.ResolveAll(this.modIndex.GetBucketNames())
                .ConfigureAwait(true);
            bucketNamesToComponents = bucketComponents.ToDictionary(x => x.Key, x => x.Component, NameComparer);
            componentNamesToBucketNames =
                bucketNamesToComponents.ToDictionary(x => x.Value.Name, x => x.Key, NameComparer);
            componentNamesToComponents = bucketNamesToComponents.Values.ToDictionary(x => x.Name, NameComparer);
            componentPathsToComponents = bucketNamesToComponents.Values.ToDictionary(x => x.Path, PathComparer.Default);
            modIdsToComponents = bucketNamesToComponents.Values
                .GroupBy(x => x.ModKey.Id)
                .ToDictionary(g => g.Key, g => g.ToHashSet());
            modIdsToModNames = modIdsToComponents
                .Where(x => !string.IsNullOrEmpty(x.Key))
                .ToDictionary(x => x.Key, x => x.Value.First().ModKey.Name);
            modNamesToModIds = modIdsToModNames
                .GroupBy(x => x.Value, x => x.Key)
                .ToDictionary(g => g.Key, g => g.First(), NameComparer);

            SetupArchiveIndex();
            WatchIndex();

            tcs.SetResult();
        }

        public bool ContainsFile(
            IEnumerable<ModComponentInfo> components, string relativePath, bool includeArchives,
            bool includeDisabled = false)
        {
            var bucketNames = components
                .Where(x => includeDisabled || x.IsEnabled)
                .Select(x => componentNamesToBucketNames.GetOrDefault(x.Name))
                .NotNull();
            if (bucketNames.Any(x => modIndex.Contains(x, relativePath)))
                return true;
            if (!includeArchives)
                return false;
            var archivePaths = bucketNames.SelectMany(GetArchivePaths);
            return archivePaths.Any(p => archiveIndex.Contains(p, relativePath));
        }

        public bool ContainsFile(ModInfo mod, string relativePath, bool includeArchives, bool includeDisabled = false)
        {
            return ContainsFile(mod.Components, relativePath, includeArchives, includeDisabled);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public ModInfo? FindByComponentName(string componentName)
        {
            return componentNamesToComponents.TryGetValue(componentName, out var component) ?
                GetComponentMod(component) : null;
        }

        public ModInfo? FindByComponentPath(string componentPath)
        {
            return componentPathsToComponents.TryGetValue(componentPath, out var component) ?
                GetComponentMod(component) : null;
        }

        public ModInfo? FindByKey(IModLocatorKey key)
        {
            if (!string.IsNullOrEmpty(key.Id) && modIdsToComponents.TryGetValue(key.Id, out var modComponents))
                return ConvertModInfo(key.Id, modComponents);
            if (!string.IsNullOrEmpty(key.Name) && modNamesToModIds.TryGetValue(key.Name, out var modId) &&
                modIdsToComponents.TryGetValue(modId, out modComponents))
                return ConvertModInfo(modId, modComponents);
            // Look at components as a fallback, only if we couldn't match any other way. This would be the case for
            // components without mod IDs for which we've assigned an arbitrary name as the "mod name".
            if (!string.IsNullOrEmpty(key.Name) && componentNamesToComponents.TryGetValue(key.Name, out var component))
                return GetComponentMod(component);
            return null;
        }

        public ModInfo? GetById(string modId)
        {
            return !string.IsNullOrEmpty(modId) && modIdsToComponents.TryGetValue(modId, out var modComponents) ?
                ConvertModInfo(modId, modComponents) : null;
        }

        public ModInfo? GetByName(string modName)
        {
            if (string.IsNullOrEmpty(modName))
                return null;
            if (modNamesToModIds.TryGetValue(modName, out var modId) &&
                modIdsToComponents.TryGetValue(modId, out var modComponents))
                return ConvertModInfo(modId, modComponents);
            // Allow using component name as fallback, only if mod name is not found.
            if (componentNamesToComponents.TryGetValue(modName, out var component))
                return GetComponentMod(component);
            return null;
        }

        public IEnumerator<ModInfo> GetEnumerator()
        {
            var resultsFromIds = modIdsToComponents
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .Select(g => ConvertModInfo(g.Key, g.Value));
            var standaloneResults = modIdsToComponents
                .Where(g => string.IsNullOrEmpty(g.Key))
                .SelectMany(g => g.Value)
                .Select(x => GetComponentMod(x));
            return resultsFromIds.Concat(standaloneResults).GetEnumerator();
        }

        public IEnumerable<ModSearchResult> SearchForFiles(
            string relativePath, bool includeArchives, bool includeDisabled)
        {
            var mainResults = modIndex.FindInBuckets(relativePath)
                .Select(x => bucketNamesToComponents.GetOrDefault(x.Key))
                .NotNull()
                .Where(x => includeDisabled || x.IsEnabled)
                .Select(x => new ModSearchResult(x, relativePath, null));

            var archiveBuckets = includeArchives ?
                archiveIndex.FindInBuckets(relativePath) : Enumerable.Empty<KeyValuePair<string, string>>();
            var archiveResults = archiveBuckets
                .Select(x => new
                {
                    ArchiveName = Path.GetFileName(x.Key),
                    BucketName = new FileInfo(x.Key).Directory?.Name ?? string.Empty,
                })
                .Select(x => new
                {
                    x.ArchiveName,
                    Component = x.BucketName is not null ? bucketNamesToComponents.GetOrDefault(x.BucketName) : null,
                })
                .Select(x => x.Component is not null ?
                    new ModSearchResult(x.Component, relativePath, x.ArchiveName) : null)
                .NotNull();
            return mainResults.Concat(archiveResults);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;
            if (disposing)
                UnwatchIndex();
            isDisposed = true;
        }

        protected string GetAbsolutePath(string bucketName, string componentPath = "")
        {
            return Path.Combine(rootPath, bucketName, componentPath);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private ModInfo ConvertModInfo(string id, IEnumerable<ModComponentInfo> modComponents)
        {
            // Try to get the mod name from our dictionary, so we don't potentially end up with a different mod name
            // depending on which component was used to request it.
            var modName = modIdsToModNames.TryGetValue(id, out var cachedName) ?
                cachedName : modComponents.First().ModKey.Name;
            return new ModInfo(id, modName, modComponents);
        }

        private IEnumerable<string> GetArchivePaths(string bucketName)
        {
            return bucketNamesToArchivePaths.TryGetValue(bucketName, out var archivePaths) ?
                archivePaths : Enumerable.Empty<string>();
        }

        private ModInfo GetComponentMod(ModComponentInfo component)
        {
            if (!string.IsNullOrEmpty(component.ModKey.Id) &&
                modIdsToComponents.TryGetValue(component.ModKey.Id, out var modComponents))
                return ConvertModInfo(component.ModKey.Id, modComponents);
            // No matching mod, return component as a standalone mod.
            return new ModInfo(string.Empty, component.Name, new[] { component });
        }

        private bool IsArchive(string contentPath)
        {
            return archiveProvider.IsArchiveFile(contentPath) &&
                // Archives have to be in the mod root to be recognized by the game, and we should do the same.
                Path.GetFileName(contentPath) == contentPath;
        }

        private async void ModIndex_AddedToBucket(object? sender, BucketedFileEventArgs e)
        {
            await setupTask; // Avoid conflicts with initial index processing.

            if (IsArchive(e.Path))
            {
                archiveIndex.AddArchive(GetAbsolutePath(e.BucketName, e.Path));
                var archivePaths =
                    bucketNamesToArchivePaths.GetOrAdd(e.BucketName, () => new(PathComparer.Default));
                archivePaths.Add(GetAbsolutePath(e.BucketName, e.Path));
            }

            // Component resolver should never actually be null here - it's an invariant, and these event handlers are
            // set up after the resolver is updated. This is just to make the compiler happy.
            if (componentResolver is not null && !bucketNamesToComponents.ContainsKey(e.BucketName))
            {
                var component = await componentResolver.ResolveComponentInfo(e.BucketName);
                RegisterComponent(component, e.BucketName);
            }
        }

        private async void ModIndex_RemovedFromBucket(object? sender, BucketedFileEventArgs e)
        {
            await setupTask; // Avoid conflicts with initial index processing.

            if (IsArchive(e.Path))
            {
                archiveIndex.RemoveArchive(GetAbsolutePath(e.BucketName, e.Path));
                if (bucketNamesToArchivePaths.TryGetValue(e.BucketName, out var archivePaths))
                    archivePaths.Remove(GetAbsolutePath(e.BucketName, e.Path));
                if (modIndex.IsEmpty(e.BucketName) &&
                    bucketNamesToComponents.TryGetValue(e.BucketName, out var component))
                    UnregisterComponent(component);
            }
            // Currently we don't detect if an entire bucket (i.e. mod directory) is removed. It likely should not
            // matter if there are no files left in the directory.
        }

        private void RegisterComponent(ModComponentInfo component, string bucketName)
        {
            if (string.IsNullOrEmpty(component.Name))
                throw new ModManagerException("Cannot register a mod component without a component name.");

            bucketNamesToComponents[bucketName] = component;
            componentNamesToBucketNames[component.Name] = bucketName;
            componentNamesToComponents[component.Name] = component;
            componentPathsToComponents[component.Path] = component;

            var modComponents = modIdsToComponents.GetOrAdd(component.ModKey.Id, () => new());
            modComponents.Add(component);
            if (!string.IsNullOrEmpty(component.ModKey.Id) && !modIdsToModNames.ContainsKey(component.ModKey.Id) &&
                !string.IsNullOrEmpty(component.ModKey.Name))
            {
                modIdsToModNames[component.ModKey.Id] = component.ModKey.Name;
                if (!modNamesToModIds.ContainsKey(component.ModKey.Name))
                    modNamesToModIds.Add(component.ModKey.Name, component.ModKey.Id);
            }
        }

        private void SetupArchiveIndex()
        {
            archiveIndex.Clear();
            bucketNamesToArchivePaths.Clear();
            var archivePathPairs = modIndex.GetBucketedFilePaths()
                .AsParallel()
                .Select(x => new
                {
                    BucketName = x.Key,
                    ArchivePaths = x.Value.Where(path => IsArchive(path)).Select(p => GetAbsolutePath(x.Key, p)),
                })
                .ToList();
            foreach (var archivePathPair in archivePathPairs)
                bucketNamesToArchivePaths.Add(
                    archivePathPair.BucketName, archivePathPair.ArchivePaths.ToHashSet(PathComparer.Default));
            archiveIndex.AddArchives(archivePathPairs.SelectMany(x => x.ArchivePaths));
        }

        private void UnregisterComponent(ModComponentInfo component)
        {
            if (!componentNamesToBucketNames.TryGetValue(component.Name, out var bucketName))
                return;
            bucketNamesToComponents.Remove(bucketName);
            componentNamesToBucketNames.Remove(component.Name);
            componentNamesToComponents.Remove(component.Name);
            componentPathsToComponents.Remove(component.Path);
            if (modIdsToComponents.TryGetValue(component.ModKey.Id, out var modComponents))
                modComponents.Remove(component);
        }

        private void UnwatchIndex()
        {
            if (modIndex is null)
                return;
            modIndex.AddedToBucket -= ModIndex_AddedToBucket;
            modIndex.RemovedFromBucket -= ModIndex_RemovedFromBucket;
        }

        private void WatchIndex()
        {
            if (modIndex is null)
                return;
            modIndex.AddedToBucket += ModIndex_AddedToBucket;
            modIndex.RemovedFromBucket += ModIndex_RemovedFromBucket;
        }
    }
}
