using System.Diagnostics.CodeAnalysis;

namespace Focus.Graphics
{
    public class LruCache<TKey, TValue> : IConcurrentCache<TKey, TValue>
        where TKey : notnull
    {
        record Entry(TKey Key, TValue Value, long Weight);

        // Using a lock-free collection like ConcurrentDictionary would be nice, but is not
        // practical here due to the need for multiple maps. This isn't just a priority queue with
        // fixed priority "keys", we need to change the priority when an access happens.
        //
        // We shouldn't be expecting thousands of operations per second on this cache anyway, more
        // like 20 or fewer (e.g. total # of meshes/textures used in a scene).
        private readonly object sync = new();

        private readonly long maxWeight;
        private readonly Func<TValue, long> weightSelector;
        private readonly LinkedList<Entry> entries = new();
        private readonly Dictionary<TKey, LinkedListNode<Entry>> entryNodesByKey = new();

        private long currentWeight;

        // Treats every item as having exactly 1 weight, so max weight = max items.
        // Represents the very simplest LRU which is just a bounded list.
        public LruCache(long maxItems) : this(maxItems, _ => 1) { }

        public LruCache(long maxWeight, Func<TValue, long> weightSelector)
        {
            this.weightSelector = weightSelector;
            this.maxWeight = maxWeight;
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> newValueSelector)
        {
            lock (sync)
            {
                if (entryNodesByKey.TryGetValue(key, out var entryNode))
                {
                    MoveToEnd(entryNode);
                    return entryNode.Value.Value;
                }
                var newValue = newValueSelector(key);
                if (newValue == null)
                    return newValue;
                var weight = weightSelector(newValue);
                // Don't free up entries or try to cache unless there's actually a chance of being
                // able to fit our new item.
                if (weight <= maxWeight)
                {
                    FreeWeight(weight);
                    entryNode = entries.AddLast(new Entry(key, newValue, weight));
                    entryNodesByKey.Add(key, entryNode);
                }
                return newValue;
            }
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            lock (sync)
            {
                if (!entryNodesByKey.TryGetValue(key, out var entryNode))
                {
                    value = default;
                    return false;
                }
                MoveToEnd(entryNode);
                value = entryNode.Value.Value;
                return true;
            }
        }

        private bool FreeWeight(long requiredWeight)
        {
            var targetWeight = maxWeight - requiredWeight;
            while (currentWeight > targetWeight)
            {
                var nextNode = entries.First;
                if (nextNode == null)
                    return false;
                Remove(nextNode);
            }
            return true;
        }

        private void MoveToEnd(LinkedListNode<Entry> entryNode)
        {
            entries.Remove(entryNode);
            entries.AddLast(entryNode);
        }

        private void Remove(LinkedListNode<Entry> node)
        {
            var entry = node.Value;
            entries.Remove(node);
            entryNodesByKey.Remove(entry.Key);
            currentWeight -= entry.Weight;
        }
    }
}
