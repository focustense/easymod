using Focus.Analysis.Records;
using System.Collections.Generic;

namespace Focus.Analysis.Plugins
{
    public class PluginAnalysis
    {
        public string FileName { get; private init; }
        public bool IsBaseGame { get; init; }   // Vanilla or DLC

        public IReadOnlyList<string> ExplicitMasters { get; init; } = Empty.ReadOnlyList<string>();
        public IReadOnlyDictionary<RecordType, RecordAnalysisGroup> Groups { get; init; } =
            Empty.ReadOnlyDictionary<RecordType, RecordAnalysisGroup>();
        public IReadOnlyList<string> ImplicitMasters { get; init; } = Empty.ReadOnlyList<string>();

        public PluginAnalysis(string fileName)
        {
            FileName = fileName;
        }
    }
}
