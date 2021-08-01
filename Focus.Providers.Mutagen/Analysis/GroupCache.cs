using Mutagen.Bethesda;
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
        private readonly ConcurrentDictionary<Tuple<string, Type>, IGroupCommonGetter<ISkyrimMajorRecordGetter>?> genericGroups =
            new(new TupleEqualityComparer<string, Type>(StringComparer.OrdinalIgnoreCase));
        private readonly ILogger log;

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
                return mod?.GetTopLevelGroupGetter(groupType);
            });
        }

        public IGroupCommonGetter<T>? Get<T>(
            string pluginName, Func<ISkyrimModGetter, IGroupCommonGetter<T>> groupSelector)
            where T : class, ISkyrimMajorRecordGetter
        {
            return (IGroupCommonGetter<T>?)genericGroups.GetOrAdd(Tuple.Create(pluginName, typeof(T)), x =>
            {
                var mod = GetMod(x.Item1);
                return mod != null ? groupSelector(mod) : null;
            });
        }

        public IEnumerable<IKeyValue<T, string>> GetAll<T>(IFormLinkGetter<T> link)
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
                    yield return new KeyValue<T, string>(listing.ModKey.FileName, resolved);
            }
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
            return GetAll(link).FirstOrDefault()?.Value;
        }

        public bool MasterExists(FormKey formKey, RecordType recordType)
        {
            var masterGroup = Get(formKey.ModKey.FileName.String, recordType.GetGroupType());
            return masterGroup?.ContainsKey(formKey) ?? false;
        }
    }
}
