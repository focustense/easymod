namespace Focus.Tools.EasyFollower
{
    static class EnumerableExtensions
    {
        public static (T?, IEnumerable<T>) PickFirst<T>(this IEnumerable<T> source)
        {
            var enumerator = source.GetEnumerator();
            return enumerator.MoveNext()
                ? (enumerator.Current, Rest(enumerator))
                : (default, Enumerable.Empty<T>());
        }

        private static IEnumerable<T> Rest<T>(IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }
    }
}
