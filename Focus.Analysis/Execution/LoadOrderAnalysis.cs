using Focus.Analysis.Plugins;
using System;
using System.Collections.Generic;

namespace Focus.Analysis.Execution
{
    public class LoadOrderAnalysis
    {
        public TimeSpan ElapsedTime { get; init; }
        public IReadOnlyList<PluginAnalysis> Plugins { get; init; } = Empty.ReadOnlyList<PluginAnalysis>();
    }
}