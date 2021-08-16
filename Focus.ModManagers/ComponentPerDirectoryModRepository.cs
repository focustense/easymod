using Focus.Files;
using System.Collections;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace Focus.ModManagers
{
    public record ComponentPerDirectoryConfiguration(string RootPath);

    public abstract class ComponentPerDirectoryModRepository<TConfig> : IConfigurableModRepository<TConfig>, IWatchable
        where TConfig : ComponentPerDirectoryConfiguration
    {
        private readonly IFileSystem fs;
        private readonly IIndexedModRepository inner;

        private INotifyingBucketedFileIndex? index;

        public ComponentPerDirectoryModRepository(IFileSystem fs, IIndexedModRepository inner)
        {
            this.fs = fs;
            this.inner = inner;
        }

        public async Task Configure(TConfig config)
        {
            var resolver = GetComponentResolver(config);
            index = await BuildIndexAsync(config).ConfigureAwait(false);
            await inner.ConfigureIndex(index, config.RootPath, resolver).ConfigureAwait(false);
        }

        public bool ContainsFile(string relativePath, bool includeArchives, bool includeDisabled = false)
        {
            return inner.ContainsFile(relativePath, includeArchives, includeDisabled);
        }

        public bool ContainsFile(
            IEnumerable<ModComponentInfo> components, string relativePath, bool includeArchives,
            bool includeDisabled = false)
        {
            return inner.ContainsFile(components, relativePath, includeArchives, includeDisabled);
        }

        public bool ContainsFile(ModInfo mod, string relativePath, bool includeArchives, bool includeDisabled = false)
        {
            return inner.ContainsFile(mod, relativePath, includeArchives, includeDisabled);
        }

        public ModInfo? FindByComponentName(string componentName)
        {
            return inner.FindByComponentName(componentName);
        }

        public ModInfo? FindByComponentPath(string componentPath)
        {
            return inner.FindByComponentPath(componentPath);
        }

        public ModInfo? FindByKey(IModLocatorKey key)
        {
            return inner.FindByKey(key);
        }

        public ModInfo? GetById(string modId)
        {
            return inner.GetById(modId);
        }

        public ModInfo? GetByName(string modName)
        {
            return inner.GetByName(modName);
        }

        public IEnumerator<ModInfo> GetEnumerator()
        {
            return inner.GetEnumerator();
        }

        public void PauseWatching()
        {
            if (index is IWatchable watchable)
                watchable.PauseWatching();
        }

        public void ResumeWatching()
        {
            if (index is IWatchable watchable)
                watchable.ResumeWatching();
        }

        public IEnumerable<ModSearchResult> SearchForFiles(
            string relativePath, bool includeArchives, bool includeDisabled = false)
        {
            return inner.SearchForFiles(relativePath, includeArchives, includeDisabled);
        }

        public IEnumerable<ModSearchResult> SearchForFiles(
            IEnumerable<ModComponentInfo> components, string relativePath, bool includeArchives, bool includeDisabled = false)
        {
            return inner.SearchForFiles(components, relativePath, includeArchives, includeDisabled);
        }

        protected virtual Task<INotifyingBucketedFileIndex> BuildIndexAsync(TConfig config)
        {
            // Building the index is synchronous because the relevant file system APIs are all synchronous (e.g.
            // Directory.EnumerateFiles), but we can still run it on a background thread.
            return Task.Run(() =>
                FileSystemIndex.Build(fs, config.RootPath, Bucketizers.TopLevelDirectory())
                as INotifyingBucketedFileIndex);
        }

        protected abstract IComponentResolver GetComponentResolver(TConfig config);

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
