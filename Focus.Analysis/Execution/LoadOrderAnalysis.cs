using Focus.Analysis.Plugins;
using Focus.Analysis.Records;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Analysis.Execution
{
    public interface ILoadOrderAnalysisReceiver
    {
        void Receive(LoadOrderAnalysis analysis);
    }

    public class LoadOrderAnalysis
    {
        public TimeSpan ElapsedTime { get; init; }
        public IReadOnlyList<PluginAnalysis> Plugins { get; init; } = Empty.ReadOnlyList<PluginAnalysis>();

        public IEnumerable<RecordAnalysisChain<T>> ExtractChains<T>(RecordType recordType)
            where T : RecordAnalysis
        {
            return from plugin in Plugins
                   let recordGroup = plugin.Groups.TryGetValue(recordType, out var foundGroup) ? foundGroup : null
                   where recordGroup != null
                   from rec in recordGroup.Records.OfType<T>()
                   group new Sourced<T>(plugin.FileName, rec) by new RecordKey(rec) into rg
                   select new RecordAnalysisChain<T>(rg);
        }
    }
}