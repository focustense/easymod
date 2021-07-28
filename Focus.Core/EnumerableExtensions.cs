using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> sequence) where T : class
        {
            return sequence.Where(x => x != null)!;
        }

        public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> sequence) where T : struct
        {
            return sequence.Where(x => x.HasValue).Select(x => x!.Value);
        }

        public static IEnumerable<T>? OrderBySafe<T, TKey>(this IEnumerable<T>? sequence, Func<T, TKey> keySelector)
        {
            // If we use null-projection inline, then C# interprets the final expression differently. This essentially
            // hides it from the compiler.
            return sequence?.OrderBy(keySelector);
        }

        public static bool SequenceEqualSafe<T>(this IEnumerable<T>? first, IEnumerable<T>? second)
        {
            if (ReferenceEquals(first, second))
                return true;
            if (first == null ^ second == null)
                return false;
            return first!.SequenceEqual(second!);
        }

        public static IEnumerable<T> Tap<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            foreach (var item in sequence)
            {
                action(item);
                yield return item;
            }
        }
    }
}
