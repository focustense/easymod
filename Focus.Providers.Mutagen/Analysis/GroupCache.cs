using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RecordType = Focus.Analysis.Records.RecordType;

namespace Focus.Providers.Mutagen.Analysis
{
    public class GroupCache : IGroupCache
    {
        private readonly IReadOnlyGameEnvironment<ISkyrimModGetter> environment;
        private readonly ConcurrentDictionary<Tuple<string, Type>, IReadOnlyCache<ISkyrimMajorRecordGetter, FormKey>?> commonGroups =
            new(new TupleEqualityComparer<string, Type>(StringComparer.OrdinalIgnoreCase));
        private readonly ConcurrentDictionary<Tuple<string, Type>, IGroupGetter<ISkyrimMajorRecordGetter>?> genericGroups =
            new(new TupleEqualityComparer<string, Type>(StringComparer.OrdinalIgnoreCase));
        private readonly ILogger log;
        private readonly ConcurrentDictionary<FormKey, IReadOnlyList<IKeyValue<string, object>>> records = new();
        private readonly ConcurrentDictionary<FormKey, IKeyValue<string, object>?> winningRecords = new();

        public GroupCache(IReadOnlyGameEnvironment<ISkyrimModGetter> environment, ILogger log)
        {
            this.environment = environment;
            this.log = log;
        }

        public IReadOnlyCache<ISkyrimMajorRecordGetter, FormKey>? Get(string pluginName, Type groupType)
        {
            var groupKey = Tuple.Create(pluginName, groupType);
            return commonGroups.GetOrAdd(groupKey, x =>
            {
                // Minor optimization since it doesn't work in reverse, but if we've already retrieved the group via the
                // generic version of this method, we can reuse its record cache.
                if (genericGroups.TryGetValue(groupKey, out var genericGroup) && genericGroup != null)
                    return genericGroup.RecordCache;
                var mod = GetMod(x.Item1);
                return mod?.TryGetTopLevelGroup(groupType)?.RecordCache as IReadOnlyCache<ISkyrimMajorRecordGetter, FormKey>;
            });
        }

        public IGroupGetter<T>? Get<T>(
            string pluginName, Func<ISkyrimModGetter, IGroupGetter<T>> groupSelector)
            where T : class, ISkyrimMajorRecordGetter
        {
            return (IGroupGetter<T>?)genericGroups.GetOrAdd(Tuple.Create(pluginName, typeof(T)), x =>
            {
                var mod = GetMod(x.Item1);
                return mod != null ? groupSelector(mod) : null;
            });
        }

        public IEnumerable<IKeyValue<string, T>> GetAll<T>(IFormLinkGetter<T> link)
            where T : class, ISkyrimMajorRecordGetter
        {
            return records.GetOrAdd(link.FormKey, _ => LoadAll(link).ToList().AsReadOnly())
                .Cast<IKeyValue<string, T>>();
        }

        public ISkyrimModGetter? GetMod(string pluginName)
        {
            return environment.TryGetMod(pluginName, log);
        }

        public T? GetWinner<T>(IFormLinkGetter<T> link)
            where T : class, ISkyrimMajorRecordGetter
        {
            // Could use ILinkCache.TryResolve here, but this implementation is more consistent and easier to work with
            // in tests, and should have very similar (?) performance characteristics.
            return winningRecords.GetOrAdd(link.FormKey, _ => GetAll(link).FirstOrDefault())?.Value as T;
        }

        public IKeyValue<string, T>? GetWinnerWithSource<T>(IFormLinkGetter<T> link)
            where T : class, ISkyrimMajorRecordGetter
        {
            var cached = winningRecords.GetOrAdd(link.FormKey, _ => GetAll(link).FirstOrDefault());
            return cached is not null ? new KeyValue<string, T>(cached.Key, (T)cached.Value) : null;
        }

        public bool MasterExists(FormKey formKey, RecordType recordType)
        {
            var masterGroup = Get(formKey.ModKey.FileName.String, recordType.GetGroupType());
            return masterGroup?.ContainsKey(formKey) ?? false;
        }

        public void Purge()
        {
            commonGroups.Clear();
            genericGroups.Clear();
            records.Clear();
            winningRecords.Clear();
        }

        private IEnumerable<IKeyValue<string, T>> LoadAll<T>(IFormLinkGetter<T> link)
            where T : class, ISkyrimMajorRecordGetter
        {
            if (!link.FormKeyNullable.HasValue)
                yield break;
            // Mutagen has `ResolveAllContexts` that would be useful here, but it requires an unreasonable amount of
            // generic arguments for this simplified use case.
            // Since we are already caching groups - that's the whole purpose of this class - it should make little
            // difference performance-wise to iterate through "our" groups and bypass the link cache - at worst it has
            // to repeat group accesses that the link cache has already done exactly one time each.
            foreach (var listing in environment.LoadOrder.PriorityOrder)
            {
                var group = Get(listing.ModKey.FileName, typeof(T));
                var record = group?.TryGetValue(link.FormKey);
                if (record is T resolved)
                    yield return new KeyValue<string, T>(listing.ModKey.FileName, resolved);
            }
        }
    }
}
