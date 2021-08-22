using Focus.Analysis.Records;
using Focus.Environment;
using Serilog;
using System.Collections.Generic;

namespace Focus.Analysis.Execution
{
    public abstract class LoadOrderAnalyzer : ILoadOrderAnalyzer
    {
        protected ILogger Log { get; private init; }
        protected IRecordScanner Scanner { get; private init; }

        public LoadOrderAnalyzer(IRecordScanner scanner, ILogger log)
        {
            Log = log;
            Scanner = scanner;
        }

        public LoadOrderAnalysis Analyze(
            IEnumerable<string> availablePlugins, IReadOnlyLoadOrderGraph loadOrderGraph, bool includeImplicits = false)
        {
            var runner = new AnalysisRunner(Scanner, availablePlugins, loadOrderGraph, Log);
            Configure(runner);
            var analysis = runner.Run(includeImplicits);
            OnCompleted(analysis);
            return analysis;
        }

        protected abstract void Configure(AnalysisRunner runner);

        protected virtual void OnCompleted(LoadOrderAnalysis analysis) { }
    }
}
