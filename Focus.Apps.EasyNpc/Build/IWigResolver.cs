using Focus.Analysis.Records;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Build
{
    public interface IWigResolver
    {
        public IEnumerable<WigMatch> ResolveAll(IEnumerable<NpcWigInfo> wigs);
    }

    public class WigMatch
    {
        public IRecordKey WigKey { get; private init; }
        public IReadOnlyList<IRecordKey> HairKeys { get; private init; }

        public WigMatch(IRecordKey wigKey, IEnumerable<IRecordKey> hairKeys)
        {
            WigKey = wigKey;
            HairKeys = hairKeys.ToList().AsReadOnly();
        }
    }
}