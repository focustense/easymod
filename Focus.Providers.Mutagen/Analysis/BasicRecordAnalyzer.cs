using Focus.Analysis;
using Focus.Analysis.Records;
using System.Linq;
using RecordType = Focus.Analysis.Records.RecordType;

namespace Focus.Providers.Mutagen.Analysis
{
    public class BasicRecordAnalyzer : IRecordAnalyzer<BasicRecordAnalysis>
    {
        public RecordType RecordType => recordType;

        private readonly IGroupCache groups;
        private readonly RecordType recordType;
        private readonly IReferenceChecker? referenceChecker;

        public BasicRecordAnalyzer(
            IGroupCache groups, RecordType recordType, IReferenceChecker? referenceChecker = null)
        {
            this.groups = groups;
            this.recordType = recordType;
            this.referenceChecker = referenceChecker;
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
                InvalidPaths = referenceChecker.SafeCheck(record),
                IsInjectedOrInvalid = isOverride && !groups.MasterExists(formKey, recordType),
                IsOverride = isOverride,
            };
        }
    }
}
