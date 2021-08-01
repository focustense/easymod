using Focus.Providers.Mutagen.Analysis;
using Loqui;
using Moq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Collections.Generic;
using System.Linq;
using Activator = System.Activator;
using RecordType = Focus.Analysis.Records.RecordType;

namespace Focus.Providers.Mutagen.Tests.Analysis
{
    class FakeGroupCache : IGroupCache
    {
        private static readonly Dictionary<Type, RecordType> groupTypeMap = Enum.GetValues<RecordType>()
            .Select(t => new { RecordType = t, GroupType = t.GetGroupType() })
            .ToDictionary(x => x.GroupType, x => x.RecordType);

        private readonly Dictionary<Tuple<string, RecordType>, FakeReadOnlyCache> groups = new();
        private readonly Dictionary<string, uint> nextIds = new();
        private readonly Dictionary<Tuple<string, Type>, object> typedGroups = new();

        public void AddRecords<T>(string pluginName, params Action<T>[] configureRecords)
            where T : class, ISkyrimMajorRecordGetter
        {
            var groupGetterType = LoquiRegistration.GetRegister(typeof(T)).GetterType;
            var recordType = groupTypeMap[groupGetterType];
            var group = GetCache(pluginName, recordType);
            var nextId = nextIds.TryGetValue(pluginName, out var id) ? id : 0;
            foreach (var configure in configureRecords)
            {
                var formKey = new FormKey(pluginName, ++nextId);
                var record = (T)Activator.CreateInstance(typeof(T), formKey, SkyrimRelease.SkyrimSE);
                configure(record);
                group.Put(record);
            }
            nextIds[pluginName] = nextId;
        }

        public IReadOnlyCache<ISkyrimMajorRecordGetter, FormKey> Get(string pluginName, RecordType recordType)
        {
            return GetCache(pluginName, recordType);
        }

        public IGroupGetter<T> Get<T>(string pluginName, Func<ISkyrimModGetter, IGroupGetter<T>> groupSelector)
            where T : class, ISkyrimMajorRecordGetter
        {
            return GetGroupGetter<T>(pluginName, typeof(T)).Object;
        }

        public ISkyrimModGetter GetMod(string pluginName)
        {
            throw new NotImplementedException();
        }

        public bool MasterExists(FormKey formKey, RecordType recordType)
        {
            return GetCache(formKey.ModKey.FileName, recordType).ContainsKey(formKey);
        }

        private FakeReadOnlyCache GetCache(string pluginName, RecordType type)
        {
            var key = Tuple.Create(pluginName, type);
            if (!groups.TryGetValue(key, out var cache))
            {
                cache = new FakeReadOnlyCache();
                groups.Add(key, cache);
            }
            return cache;
        }

        private Mock<IGroupGetter<T>> GetGroupGetter<T>(string pluginName, Type groupType)
            where T : class, ISkyrimMajorRecordGetter
        {
            var key = Tuple.Create(pluginName, groupType);
            if (!typedGroups.TryGetValue(key, out var group))
            {
                var recordType = groupTypeMap[groupType];
                var baseCache = GetCache(pluginName, recordType);
                var baseTypedCache = baseCache.Of<T>();

                var groupMock = new Mock<IGroupGetter<T>>();
                groupMock.SetupGet(x => x.Count).Returns(() => baseCache.Count);
                groupMock.SetupGet(x => x.FormKeys).Returns(() => baseCache.Keys);
                groupMock.SetupGet(x => x.RecordCache).Returns(baseTypedCache);
                groupMock.Setup(x => x.ContainsKey(It.IsAny<FormKey>()))
                    .Returns((FormKey key) => baseCache.ContainsKey(key));
                groupMock.Setup(x => x[It.IsAny<FormKey>()]).Returns((FormKey key) => baseTypedCache[key]);
                groupMock.Setup(x => x.GetEnumerator()).Returns(() => baseTypedCache.Items.GetEnumerator());
                group = groupMock;
                typedGroups.Add(key, group);
            }
            return (Mock<IGroupGetter<T>>)group;
        }
    }
}
