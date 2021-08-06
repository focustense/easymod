using Focus.Analysis.Plugins;
using Focus.Analysis.Records;
using Focus.Environment;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Focus.Analysis.Execution
{
    public class AnalysisRunner
    {
        private delegate RecordAnalysisGroup GroupAnalyzerAction(string pluginName, IEnumerable<IRecordKey> keys);

        private static readonly RecordType[] recordTypes = Enum.GetValues<RecordType>();

        private readonly ConcurrentDictionary<RecordType, GroupAnalyzerAction?> analyzerActions = new();
        private readonly IEnumerable<string> availablePlugins;
        private readonly IReadOnlyLoadOrderGraph loadOrderGraph;
        private readonly ILogger log;
        private readonly IRecordScanner scanner;

        private Func<RecordType, GroupAnalyzerAction>? defaultAnalyzerActionFactory;

        public AnalysisRunner(
            IRecordScanner scanner, IEnumerable<string> availablePlugins, IReadOnlyLoadOrderGraph loadOrderGraph,
            ILogger log)
        {
            this.availablePlugins = availablePlugins;
            this.loadOrderGraph = loadOrderGraph;
            this.log = log;
            this.scanner = scanner;
        }

        public AnalysisRunner Configure<T>(RecordType type, IRecordAnalyzer<T> analyzer)
            where T : RecordAnalysis
        {
            if (analyzer.RecordType != type)
                throw new ArgumentException(
                    $"Analyzer type {analyzer.GetType().Name} doesn't support record type {type} " +
                    $"(expects {analyzer.RecordType}).",
                    nameof(type));
            analyzerActions[type] = MakeAnalyzerAction(analyzer);
            return this;
        }

        public AnalysisRunner ConfigureDefault<T>(Func<RecordType, IRecordAnalyzer<T>> analyzerFactory)
            where T : RecordAnalysis
        {
            defaultAnalyzerActionFactory = t => MakeAnalyzerAction(analyzerFactory(t));
            return this;
        }

        public LoadOrderAnalysis Run(bool includeBasePlugins = false)
        {
            var stopwatch = Stopwatch.StartNew();
            var loadedPlugins = availablePlugins
                .Where(p => loadOrderGraph.IsEnabled(p) && loadOrderGraph.CanLoad(p))
                .ToList();
            var pluginAnalyses = loadedPlugins
                .Where(p => includeBasePlugins || !loadOrderGraph.IsImplicit(p))
                .AsParallel()
                .Select(p => RunForPlugin(p))
                // Might be slow, but probably faster than any other part of the analysis and therefore not
                // important enough to be noticeable.
                .OrderBy(x => loadedPlugins.IndexOf(x.FileName))
                .ToList()
                .AsReadOnly();
            stopwatch.Stop();
            return new LoadOrderAnalysis
            {
                ElapsedTime = stopwatch.Elapsed,
                Plugins = pluginAnalyses
            };
        }

        private GroupAnalyzerAction? GetAnalyzerAction(RecordType type)
        {
            return analyzerActions.GetOrAdd(type, t => defaultAnalyzerActionFactory?.Invoke(t));
        }

        private static GroupAnalyzerAction MakeAnalyzerAction<T>(IRecordAnalyzer<T> analyzer)
            where T : RecordAnalysis
        {
            return (pluginName, keys) => new RecordAnalysisGroup<T>(
                analyzer.RecordType, keys.Select(key => analyzer.Analyze(pluginName, key)));
        }

        private PluginAnalysis RunForPlugin(string pluginName)
        {
            log.Information("Starting analysis of {pluginName}", pluginName);
            var recordGroups =
                from type in recordTypes
                let analyzerAction = GetAnalyzerAction(type)
                where analyzerAction != null
                let keys = scanner.GetKeys(pluginName, type)
                select analyzerAction(pluginName, keys);
            var result = new PluginAnalysis(pluginName)
            {
                ExplicitMasters = loadOrderGraph.GetAllMasters(pluginName, false).ToList().AsReadOnly(),
                ImplicitMasters = loadOrderGraph.GetAllMasters(pluginName, true).ToList().AsReadOnly(),
                IsBaseGame = loadOrderGraph.IsImplicit(pluginName),
                Groups = recordGroups.ToDictionary(g => g.Type),
            };
            log.Information("Completed analysis of {pluginName}", pluginName);
            return result;
        }
    }
}