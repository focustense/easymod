using System;
using System.Collections.Generic;

namespace Focus.Providers.Mutagen
{
    class TupleEqualityComparer<T1, T2> : IEqualityComparer<Tuple<T1, T2>>
    {
        private readonly IEqualityComparer<T1> comparer1;
        private readonly IEqualityComparer<T2> comparer2;

        public TupleEqualityComparer(IEqualityComparer<T1>? comparer1, IEqualityComparer<T2>? comparer2 = null)
        {
            this.comparer1 = comparer1 ?? EqualityComparer<T1>.Default;
            this.comparer2 = comparer2 ?? EqualityComparer<T2>.Default;
        }

        public bool Equals(Tuple<T1, T2>? x, Tuple<T1, T2>? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;
            return comparer1.Equals(x.Item1, y.Item1) && comparer2.Equals(x.Item2, y.Item2);
        }

        public int GetHashCode(Tuple<T1, T2> tuple)
        {
            if (tuple == null)
                throw new ArgumentNullException(nameof(tuple));
            return comparer1.GetHashCode(tuple.Item1!) ^ comparer2.GetHashCode(tuple.Item2!);
        }

    }
}