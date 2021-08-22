namespace Focus.Analysis.Records
{
    public abstract class RecordAnalysis : IRecordKey
    {
        public string BasePluginName { get; init; } = string.Empty;
        public string EditorId { get; init; } = string.Empty;
        public bool Exists { get; init; } = false;
        public bool IsInjectedOrInvalid { get; init; }
        public bool IsOverride { get; init; }
        public string LocalFormIdHex { get; init; } = string.Empty;
        public abstract RecordType Type { get; }
    }
}
