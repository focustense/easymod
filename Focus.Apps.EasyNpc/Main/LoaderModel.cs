using Focus.Analysis.Execution;
using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Profiles;
using Focus.Environment;
using Focus.ModManagers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Main
{
    public class LoaderTasks
    {
        public Task<LoadOrderAnalysis> LoadOrderAnalysis { get; init; }
        public Task<IModRepository> ModRepository { get; init; }
        public Task<Profile> Profile { get; init; }
    }

    public class LoaderModel
    {
        private readonly IEnumerable<ILoadOrderAnalysisReceiver> analysisReceivers;
        private readonly Lazy<ILoadOrderAnalyzer> analyzer;
        private readonly IConfigurableModRepository<ComponentPerDirectoryConfiguration> modRepository;
        private readonly ILogger log;
        private readonly IProfileFactory profileFactory;
        private readonly IAppSettings settings;
        private readonly IGameSetup setup;

        private Task modRepositoryConfigureTask;

        public LoaderModel(
            IAppSettings settings, IGameSetup setup, Lazy<ILoadOrderAnalyzer> analyzer, IProfileFactory profileFactory,
            IConfigurableModRepository<ComponentPerDirectoryConfiguration> modRepository,
            IEnumerable<ILoadOrderAnalysisReceiver> analysisReceivers, ILogger logger)
        {
            this.analysisReceivers = analysisReceivers;
            this.analyzer = analyzer;
            this.log = logger;
            this.modRepository = modRepository;
            this.profileFactory = profileFactory;
            this.settings = settings;
            this.setup = setup;
        }

        public LoaderTasks Complete()
        {
            log.Information("Load order confirmed");
            var modRepositoryTask = modRepositoryConfigureTask.ContinueWith(_ => modRepository as IModRepository);
            var loadOrderAnalysisTask = AnalyzeLoadOrder();
            var profileTask = Task.WhenAll(loadOrderAnalysisTask, modRepositoryTask)
                .ContinueWith(t => profileFactory.RestoreSaved(loadOrderAnalysisTask.Result, out _, out _));

            return new LoaderTasks
            {
                LoadOrderAnalysis = loadOrderAnalysisTask,
                ModRepository = modRepositoryTask,
                Profile = profileTask,
            };
        }

        public void Prepare()
        {
            modRepositoryConfigureTask = !string.IsNullOrEmpty(settings.ModRootDirectory) ?
                Task.Run(async () =>
                {
                    log.Information("Beginning mod indexing");
                    await modRepository.Configure(new(settings.ModRootDirectory));
                    log.Information("Finished mod indexing");
                }) :
                Task.CompletedTask;
        }

        private async Task<LoadOrderAnalysis> AnalyzeLoadOrder()
        {
            log.Information("Creating load order analyzer");
            var analyzer = await Task.Run(() => this.analyzer.Value).ConfigureAwait(false);
            log.Information("Beginning load order analysis");
            var availablePluginNames = setup.AvailablePlugins.Select(p => p.FileName);
            var result = analyzer.Analyze(availablePluginNames, setup.LoadOrderGraph, true);
            log.Information(
                "Analysis completed in {elapsedSeconds} sec",
                Math.Round(result.ElapsedTime.TotalSeconds, 2));
            foreach (var receiver in analysisReceivers)
                receiver.Receive(result);
            return result;
        }
    }
}
