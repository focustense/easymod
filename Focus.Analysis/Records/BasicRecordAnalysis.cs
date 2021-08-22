namespace Focus.Analysis.Records
{
    public class BasicRecordAnalysis : RecordAnalysis
    {
        public override RecordType Type => type;

        private readonly RecordType type;

        public BasicRecordAnalysis(RecordType type)
        {
            this.type = type;
        }
    }
}