using Focus.Providers.Mutagen.Analysis;
using Loqui;
using Moq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Activator = System.Activator;
using RecordType = Focus.Analysis.Records.RecordType;

namespace Focus.Providers.Mutagen.Tests.Analysis
{
    class FakeGroupCache : IGroupCache
    {
        private readonly Dictionary<Tuple<string, Type>, FakeReadOnlyCache> groups = new();
        private readonly Dictionary<string, Mock<ISkyrimModGetter>> mods = new();
        private readonly Dictionary<string, uint> nextIds = new();
        private readonly Dictionary<Tuple<string, Type>, object> typedGroups = new();

        private List<string> pluginOrder = new();

        public RecordKey[] AddRecords<T>(string pluginName, params Action<T>[] configureRecords)
            where T : class, ISkyrimMajorRecordGetter
        {
            return AddRecords(pluginName, pluginName, configureRecords);
        }

        public RecordKey[] AddRecords<T>(string pluginName, string masterName, params Action<T>[] configureRecords)
            where T : class, ISkyrimMajorRecordGetter
        {
            // Implicit order as determined by order in which records are added - many tests will never need to override
            // this behavior.
            if (!pluginOrder.Contains(pluginName))
                pluginOrder.Add(pluginName);
            var groupGetterType = LoquiRegistration.GetRegister(typeof(T)).GetterType;
            var group = GetCache(pluginName, groupGetterType);
            var nextId = nextIds.TryGetValue(pluginName, out var id) ? id : 0;
            var newKeys = new List<FormKey>();
            foreach (var configure in configureRecords)
            {
                var formKey = new FormKey(masterName, ++nextId);
                nextIds[pluginName] = nextId;
                var record = (T)Activator.CreateInstance(typeof(T), formKey, SkyrimRelease.SkyrimSE);
                configure(record);
                group.Put(record);
                newKeys.Add(formKey);
            }
            return newKeys.Select(x => x.ToRecordKey()).ToArray();
        }

        public void ConfigureMod(string pluginName, Action<Mock<ISkyrimModGetter>> configure)
        {
            if (!mods.TryGetValue(pluginName, out var modMock))
            {
                modMock = new Mock<ISkyrimModGetter>();
                mods.Add(pluginName, modMock);
            }
            configure(modMock);
        }

        public FormKey FindKeyForEditorId(string editorId, out FakeReadOnlyCache group)
        {
            var found = groups.Values
                .SelectMany(g => g.Where(x => x.Value.EditorID == editorId).Select(x => (g, x.Key)))
                .FirstOrDefault();
            group = found.g;
            return found.Key;
        }

        public IReadOnlyCache<ISkyrimMajorRecordGetter, FormKey> Get(string pluginName, Type groupType)
        {
            return GetCache(pluginName, groupType);
        }

        public IGroupCommonGetter<T> Get<T>(
            string pluginName, Func<ISkyrimModGetter, IGroupCommonGetter<T>> groupSelector)
            where T : class, ISkyrimMajorRecordGetter
        {
            return GetGroupGetter<T>(pluginName, typeof(T));
        }

        public IEnumerable<IKeyValue<T, string>> GetAll<T>(IFormLinkGetter<T> link)
            where T : class, ISkyrimMajorRecordGetter
        {
            if (!link.FormKeyNullable.HasValue)
                yield break;
            for (var i = pluginOrder.Count - 1; i >= 0; i--)
            {
                var groupCache = GetCache(pluginOrder[i], typeof(T));
                var record = groupCache.TryGetValue(link.FormKey);
                if (record != null)
                    yield return new KeyValue<T, string>(pluginOrder[i], (T)record);
            }
        }

        public ISkyrimModGetter GetMod(string pluginName)
        {
            return mods.TryGetValue(pluginName, out var modMock) ? modMock.Object : default;
        }

        public T GetWinner<T>(IFormLinkGetter<T> link)
            where T : class, ISkyrimMajorRecordGetter
        {
            return GetAll(link).FirstOrDefault()?.Value;
        }

        public bool MasterExists(FormKey formKey, RecordType recordType)
        {
            return GetCache(formKey.ModKey.FileName, recordType.GetGroupType()).ContainsKey(formKey);
        }

        public void Purge()
        {
            groups.Clear();
            typedGroups.Clear();
        }

        public void SetLoadOrder(IEnumerable<string> pluginOrder)
        {
            this.pluginOrder = (pluginOrder ?? Enumerable.Empty<string>()).ToList();
        }

        private FakeReadOnlyCache GetCache(string pluginName, Type groupType)
        {
            var key = Tuple.Create(pluginName, groupType);
            if (!groups.TryGetValue(key, out var cache))
            {
                cache = new FakeReadOnlyCache();
                groups.Add(key, cache);
            }
            return cache;
        }

        private IGroupCommonGetter<T> GetGroupGetter<T>(string pluginName, Type groupType)
            where T : class, ISkyrimMajorRecordGetter
        {
            var key = Tuple.Create(pluginName, groupType);
            if (!typedGroups.TryGetValue(key, out var group))
            {
                var baseCache = GetCache(pluginName, groupType);
                var baseTypedCache = baseCache.Of<T>();
                var fakeGroup = new WrappedGroupGetter<T>(baseTypedCache);
                group = fakeGroup;
                typedGroups.Add(key, group);
            }
            return (IGroupCommonGetter<T>)group;
        }

        // Moq is too buggy to use a mock for this, but its implementation isn't suitable for use as a "general" test
        // double, which is why it is currently a private inner class.
        class WrappedGroupGetter<T> : IGroupCommonGetter<T>
            where T : class, ISkyrimMajorRecordGetter
        {
            public T this[FormKey key] => cache[key];
            public int Count => cache.Count;
            public IEnumerable<FormKey> FormKeys => cache.Keys;
            public IReadOnlyCache<T, FormKey> RecordCache => cache;
            public IMod SourceMod => throw new NotImplementedException();

            private readonly IReadOnlyCache<T, FormKey> cache;

            public WrappedGroupGetter(IReadOnlyCache<T, FormKey> cache)
            {
                this.cache = cache;
            }

            public bool ContainsKey(FormKey key)
            {
                return cache.ContainsKey(key);
            }

            public IEnumerator<T> GetEnumerator()
            {
                return cache.Items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return cache.Items.GetEnumerator();
            }
        }
    }
}
