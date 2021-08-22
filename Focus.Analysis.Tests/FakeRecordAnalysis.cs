using Focus.Analysis.Records;

namespace Focus.Analysis.Tests
{
    class FakeRecordAnalysis : RecordAnalysis
    {
        public override RecordType Type => type;

        private readonly RecordType type;

        public FakeRecordAnalysis(RecordType type)
        {
            this.type = type;
        }
    }
}
