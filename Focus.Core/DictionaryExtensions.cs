using System;
using System.Collections.Generic;

namespace Focus
{
    public static class DictionaryExtensions
    {
        public static TValue GetOrAdd<TKey, TValue>(
            this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> newValueSelector)
        {
            if (!dict.TryGetValue(key, out var value))
            {
                value = newValueSelector();
                dict.Add(key, value);
            }
            return value;
        }

        public static TValue? GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            return dict.TryGetValue(key, out var value) ? value : default;
        }
    }
}
