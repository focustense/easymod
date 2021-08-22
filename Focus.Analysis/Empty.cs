using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Focus.Analysis
{
    public static class Empty
    {
        public static IReadOnlyDictionary<TKey, TValue> ReadOnlyDictionary<TKey, TValue>()
            where TKey : notnull
        {
            return new ReadOnlyDictionary<TKey, TValue>(new Dictionary<TKey, TValue>(0));
        }

        public static IReadOnlyList<T> ReadOnlyList<T>()
        {
            return new List<T>(0).AsReadOnly();
        }
    }
}