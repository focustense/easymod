using System.Numerics;

namespace Focus.Graphics
{
    static class NumericExtensions
    {
        public static Vector3 Sum<T>(this IEnumerable<T> source, Func<T, Vector3> selector)
        {
            return source.Aggregate(Vector3.Zero, (va, x) => va + selector(x));
        }
    }
}
