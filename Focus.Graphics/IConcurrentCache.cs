using System.Diagnostics.CodeAnalysis;

namespace Focus.Graphics
{
    public interface IConcurrentCache<TKey, TValue>
    {
        TValue GetOrAdd(TKey key, Func<TKey, TValue> newValueSelector);
        bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value);
    }
}
