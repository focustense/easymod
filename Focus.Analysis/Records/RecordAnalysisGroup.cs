using System.Collections.Generic;
using System.Linq;

namespace Focus.Analysis.Records
{
    public abstract class RecordAnalysisGroup
    {
        public IReadOnlyList<RecordAnalysis> Records => GetRecords();
        public RecordType Type { get; private init; }

        public RecordAnalysisGroup(RecordType type)
        {
            Type = type;
        }

        protected abstract IReadOnlyList<RecordAnalysis> GetRecords();
    }

    public class RecordAnalysisGroup<T> : RecordAnalysisGroup
        where T : RecordAnalysis
    {
        public new IReadOnlyList<T> Records { get; private init; }

        public RecordAnalysisGroup(RecordType type, IEnumerable<T> records)
            : base(type)
        {
            Records = records.ToList().AsReadOnly();
        }

        protected override IReadOnlyList<RecordAnalysis> GetRecords()
        {
            return Records;
        }
    }
}