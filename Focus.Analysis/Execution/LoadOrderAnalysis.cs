using Focus.Analysis.Plugins;
using System.Collections.Generic;

namespace Focus.Analysis.Execution
{
    public class LoadOrderAnalysis
    {
        public IReadOnlyList<PluginAnalysis> Plugins { get; init; } = Empty.ReadOnlyList<PluginAnalysis>();
    }
}