using Focus.Apps.EasyNpc.GameData.Records;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Build
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

    public class BuildSettings<TKey> : BuildSettings
    {
        public IWigResolver<TKey> WigResolver { get; init; }
    }
}
