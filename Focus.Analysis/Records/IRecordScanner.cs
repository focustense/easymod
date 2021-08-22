using System.Collections.Generic;

namespace Focus.Analysis.Records
{
    public interface IRecordScanner
    {
        IEnumerable<IRecordKey> GetKeys(string pluginName, RecordType type);
    }
}