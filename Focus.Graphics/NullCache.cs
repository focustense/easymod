using System.Diagnostics.CodeAnalysis;

namespace Focus.Graphics
{
    public class NullCache<TKey, TValue> : IConcurrentCache<TKey, TValue>
    {
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> newValueSelector)
        {
            return newValueSelector(key);
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            value = default;
            return false;
        }
    }
}
