using Focus.Analysis.Records;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Analysis.Plugins
{
    public class RecordAnalysisChain<T> : IEnumerable<Sourced<T>>
        where T : RecordAnalysis
    {
        public Sourced<T> this[int index] => items[index];
        public Sourced<T> this[string pluginName] => analysesBySource[pluginName];

        public int Count => items.Count;
        public RecordKey Key { get; private init; }
        public T Master => master.Analysis;
        public T Winner => items[items.Count - 1].Analysis;

        private readonly Dictionary<string, Sourced<T>> analysesBySource = new();
        private readonly IReadOnlyList<Sourced<T>> items;
        private readonly Sourced<T> master;

        public RecordAnalysisChain(IEnumerable<Sourced<T>> items)
        {
            this.items = items.ToList().AsReadOnly();

            Key = new RecordKey(this.items[0].Analysis);
            master = this.items.FirstOrDefault(x => Key.PluginEquals(x.PluginName)) ?? this.items[0];
            analysesBySource = this.items.ToDictionary(x => x.PluginName, StringComparer.CurrentCultureIgnoreCase);
        }

        public bool Contains(string pluginName)
        {
            return analysesBySource.ContainsKey(pluginName);
        }

        public IEnumerator<Sourced<T>> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        public int IndexOf(Sourced<T> item)
        {
            // Need the cast because IReadOnlyList<T> doesn't have IndexOf.
            return ((IList<Sourced<T>>)items).IndexOf(item);
        }

        public int IndexOf(string pluginName)
        {
            return items
                .Select((x, i) => new { Index = i, x.PluginName })
                .FirstOrDefault(x => x.PluginName.Equals(pluginName, StringComparison.CurrentCultureIgnoreCase))
                ?.Index ?? -1;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
