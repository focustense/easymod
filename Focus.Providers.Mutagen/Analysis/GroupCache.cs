using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Serilog;
using System;
using System.Collections.Concurrent;
using RecordType = Focus.Analysis.Records.RecordType;

namespace Focus.Providers.Mutagen.Analysis
{
    public class GroupCache : IGroupCache
    {
        private readonly IReadOnlyGameEnvironment<ISkyrimModGetter> environment;
        private readonly ConcurrentDictionary<Tuple<string, RecordType>, IReadOnlyCache<ISkyrimMajorRecordGetter, FormKey>?> commonGroups =
            new(new TupleEqualityComparer<string, RecordType>(StringComparer.OrdinalIgnoreCase));
        private readonly ConcurrentDictionary<Tuple<string, Type>, IGroupGetter<ISkyrimMajorRecordGetter>?> genericGroups =
            new(new TupleEqualityComparer<string, Type>(StringComparer.OrdinalIgnoreCase));
        private readonly ILogger log;

        public GroupCache(IReadOnlyGameEnvironment<ISkyrimModGetter> environment, ILogger log)
        {
            this.environment = environment;
            this.log = log;
        }

        public IReadOnlyCache<ISkyrimMajorRecordGetter, FormKey>? Get(string pluginName, RecordType recordType)
        {
            return commonGroups.GetOrAdd(Tuple.Create(pluginName, recordType), x =>
            {
                var mod = GetMod(x.Item1);
                return mod?.GetTopLevelGroupGetter(recordType);
            });
        }

        public IGroupGetter<T>? Get<T>(string pluginName, Func<ISkyrimModGetter, IGroupGetter<T>> groupSelector)
            where T : class, ISkyrimMajorRecordGetter
        {
            return (IGroupGetter<T>?)genericGroups.GetOrAdd(Tuple.Create(pluginName, typeof(T)), x =>
            {
                var mod = GetMod(x.Item1);
                return mod != null ? groupSelector(mod) : null;
            });
        }

        public ISkyrimModGetter? GetMod(string pluginName)
        {
            return environment.TryGetMod(pluginName, log);
        }

        public bool MasterExists(FormKey formKey, RecordType recordType)
        {
            var masterGroup = Get(formKey.ModKey.FileName.String, recordType);
            return masterGroup?.ContainsKey(formKey) ?? false;
        }
    }
}
