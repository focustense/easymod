using Focus.Analysis.Records;
using System.Collections.Generic;
using System.Linq;
using RecordType = Focus.Analysis.Records.RecordType;

namespace Focus.Providers.Mutagen.Analysis
{
    public class RecordScanner : IRecordScanner
    {
        private readonly GroupCache groups;

        public RecordScanner(GroupCache groups)
        {
            this.groups = groups;
        }

        public IEnumerable<IRecordKey> GetKeys(string pluginName, RecordType type)
        {
            var group = groups.Get(pluginName, type);
            return group?.Keys.Select(x => x.ToRecordKey()) ?? Enumerable.Empty<IRecordKey>();
        }
    }
}
