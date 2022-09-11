using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Providers.Mutagen.Tests.Analysis
{
    class FakeReadOnlyCache : IReadOnlyCache<ISkyrimMajorRecordGetter, FormKey>
    {
        private readonly Dictionary<FormKey, ISkyrimMajorRecordGetter> records = new();

        public ISkyrimMajorRecordGetter this[FormKey key] => records[key];
        public int Count => records.Count;
        public IEnumerable<ISkyrimMajorRecordGetter> Items => records.Values;
        public IEnumerable<FormKey> Keys => records.Keys;

        public IReadOnlyCache<T, FormKey> Of<T>()
            where T : class, ISkyrimMajorRecordGetter
        {
            return new Wrapper<T>(this);
        }

        public void Put(ISkyrimMajorRecordGetter record)
        {
            records[record.FormKey] = record;
        }

        public bool ContainsKey(FormKey key)
        {
            return records.ContainsKey(key);
        }

        public IEnumerator<IKeyValue<FormKey, ISkyrimMajorRecordGetter>> GetEnumerator()
        {
            foreach (var kvp in records)
                yield return new KeyValue<FormKey, ISkyrimMajorRecordGetter>(kvp.Key, kvp.Value);
        }

        public ISkyrimMajorRecordGetter TryGetValue(FormKey key)
        {
            return records.TryGetValue(key, out var rec) ? rec : null;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        class Wrapper<T> : IReadOnlyCache<T, FormKey>
            where T : class, ISkyrimMajorRecordGetter
        {
            public T this[FormKey key] => (T)inner[key];
            public IEnumerable<FormKey> Keys => inner.Keys;
            public IEnumerable<T> Items => inner.Items.Cast<T>();
            public int Count => inner.Count;

            private readonly FakeReadOnlyCache inner;

            public Wrapper(FakeReadOnlyCache inner)
            {
                this.inner = inner;
            }

            public bool ContainsKey(FormKey key)
            {
                return inner.ContainsKey(key);
            }

            public IEnumerator<IKeyValue<FormKey, T>> GetEnumerator()
            {
                return inner
                    .Select(x => new KeyValue<FormKey, T>(x.Key, (T)x.Value))
                    .Cast<IKeyValue<FormKey, T>>()
                    .GetEnumerator();
            }

            public T TryGetValue(FormKey key)
            {
                return inner.TryGetValue(key) as T;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
