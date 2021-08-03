using Focus.Environment;
using System.Collections.Generic;

namespace Focus.Analysis.Execution
{
    public interface ILoadOrderAnalyzer
    {
        LoadOrderAnalysis Analyze(
            IEnumerable<string> availablePlugins, IReadOnlyLoadOrderGraph loadOrderGraph,
            bool includeImplicits = false);
    }
}
