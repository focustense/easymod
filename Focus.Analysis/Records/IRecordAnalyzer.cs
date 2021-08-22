namespace Focus.Analysis.Records
{
    public interface IRecordAnalyzer<T>
        where T : RecordAnalysis
    {
        RecordType RecordType { get; }

        T Analyze(string pluginName, IRecordKey key);
    }
}