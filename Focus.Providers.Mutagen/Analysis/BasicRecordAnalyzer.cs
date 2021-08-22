using Focus.Analysis.Records;
using RecordType = Focus.Analysis.Records.RecordType;

namespace Focus.Providers.Mutagen.Analysis
{
    public class BasicRecordAnalyzer : IRecordAnalyzer<BasicRecordAnalysis>
    {
        public RecordType RecordType => recordType;

        private readonly IGroupCache groups;
        private readonly RecordType recordType;

        public BasicRecordAnalyzer(IGroupCache groups, RecordType recordType)
        {
            this.groups = groups;
            this.recordType = recordType;
        }

        public BasicRecordAnalysis Analyze(string pluginName, IRecordKey key)
        {
            var group = groups.Get(pluginName, recordType);
            var formKey = key.ToFormKey();
            var record = group?.TryGetValue(formKey);
            var isOverride = !key.PluginEquals(pluginName);
            return new BasicRecordAnalysis(recordType)
            {
                BasePluginName = key.BasePluginName,
                LocalFormIdHex = key.LocalFormIdHex,
                EditorId = record?.EditorID ?? string.Empty,
                Exists = record != null,
                IsInjectedOrInvalid = isOverride && !groups.MasterExists(formKey, recordType),
                IsOverride = isOverride,
            };
        }
    }
}
