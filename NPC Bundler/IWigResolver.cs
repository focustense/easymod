using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc
{
    public interface IWigResolver<TKey>
    {
        public IEnumerable<WigMatch<TKey>> ResolveAll(IEnumerable<NpcWigInfo<TKey>> wigs);
    }

    public class WigMatch<TKey>
    {
        public TKey WigKey { get; private init; }
        public IReadOnlyList<TKey> HairKeys { get; private init; }

        public WigMatch(TKey wigKey, IEnumerable<TKey> hairKeys)
        {
            WigKey = wigKey;
            HairKeys = hairKeys.ToList().AsReadOnly();
        }
    }
}