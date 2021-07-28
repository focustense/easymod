using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Providers.Mutagen.Analysis
{
    public class CellCache : IReadOnlyCache<ICellGetter, FormKey>
    {
        private readonly Dictionary<FormKey, ICellGetter> cells = new();

        public ICellGetter this[FormKey key] => cells[key];

        public int Count => cells.Count;
        public IEnumerable<ICellGetter> Items => cells.Values;
        public IEnumerable<FormKey> Keys => cells.Keys;

        public CellCache(IListGroupGetter<ICellBlockGetter> cellBlocks)
        {
            cells = cellBlocks.Records
                .SelectMany(x => x.SubBlocks)
                .SelectMany(x => x.Cells)
                .ToDictionary(x => x.FormKey);
        }

        public bool ContainsKey(FormKey key)
        {
            return cells.ContainsKey(key);
        }

        public IEnumerator<IKeyValue<ICellGetter, FormKey>> GetEnumerator()
        {
            return cells
                .Select(x => new KeyValue<ICellGetter, FormKey>(x.Key, x.Value))
                .GetEnumerator();
        }

        public ICellGetter? TryGetValue(FormKey key)
        {
            return cells.TryGetValue(key, out var cell) ? cell : null;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}