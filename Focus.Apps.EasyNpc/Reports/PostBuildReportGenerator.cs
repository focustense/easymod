using Focus.Analysis.Execution;
using Focus.Apps.EasyNpc.Build;
using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Environment;
using Focus.Files;
using Focus.ModManagers;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Reports
{
    public class PostBuildReportGenerator
    {
        public IObservable<string> Status => status;

        private readonly IArchiveProvider archiveProvider;
        // Facegen editor must be lazy because it depends on the created environment.
        private readonly Lazy<IFaceGenEditor> faceGenEditor;
        // File provider must be lazy because it depends on the created environment.
        private readonly Lazy<IFileProvider> fileProvider;
        private readonly IFileSystem fs;
        // Load order analyzer must be lazy because it depends on the created environment.
        private readonly Lazy<ILoadOrderAnalyzer> loadOrderAnalyzer;
        private readonly IConfigurableModRepository<ComponentPerDirectoryConfiguration> modRepository;
        private readonly IModSettings modSettings;
        private readonly IGameSetup setup;
        private readonly BehaviorSubject<string> status = new("Waiting to start");

        public PostBuildReportGenerator(
            IConfigurableModRepository<ComponentPerDirectoryConfiguration> modRepository, IModSettings modSettings,
            IGameSetup setup, IFileSystem fs, Lazy<IFileProvider> fileProvider, IArchiveProvider archiveProvider,
            Lazy<ILoadOrderAnalyzer> loadOrderAnalyzer, Lazy<IFaceGenEditor> faceGenEditor)
        {
            this.archiveProvider = archiveProvider;
            this.faceGenEditor = faceGenEditor;
            this.fileProvider = fileProvider;
            this.fs = fs;
            this.loadOrderAnalyzer = loadOrderAnalyzer;
            this.modRepository = modRepository;
            this.modSettings = modSettings;
            this.setup = setup;
        }

        public async Task<PostBuildReport> CreateReport()
        {
            status.OnNext("Starting up");
            await Task.Run(() => setup.Confirm());
            // Start mod indexing right away so we can do other things while it's running.
            status.OnNext("Beginning mod indexing");
            var modRepositoryConfigTask = Task.Run(() => modRepository.Configure(new(modSettings.RootDirectory)));
            // Start the analysis right away so we can do other things while it's running.
            status.OnNext("Beginning analysis");
            var analysisTask = Task.Run(() => loadOrderAnalyzer.Value.Analyze(
                setup.AvailablePlugins.Select(x => x.FileName), setup.LoadOrderGraph, true));
            status.OnNext("Checking plugins and archives");
            var mainPluginName = FileStructure.MergeFileName;
            var archives = await Task.Run(() => CheckArchives(mainPluginName)
                .OrderBy(x => x.ArchiveName?.IndexOf("- Textures") ?? -1)
                .ThenBy(x => x.ArchiveName)
                .ToList());
            status.OnNext("Waiting for analysis to finish");
            var analysis = await analysisTask;
            status.OnNext("Waiting for mod indexing to finish");
            await modRepositoryConfigTask;
            status.OnNext("Finishing the report");
            var mergeComponents = modRepository.SearchForFiles(mainPluginName, false)
                .Select(x => x.ModComponent)
                .Distinct()
                .ToList();
            var report = new PostBuildReport
            {
                MainPluginMissingMasters = setup.LoadOrderGraph.GetMissingMasters(mainPluginName).ToList().AsReadOnly(),
                MainPluginName = mainPluginName,
                MainPluginState = GetPluginState(mainPluginName),
                ActiveMergeComponents = mergeComponents,
                Archives = archives,
            };
            return report;
        }

        private IEnumerable<MergeArchiveInfo> CheckArchives(string mainPluginName)
        {
            var regex = new Regex(
                $@"^{fs.Path.GetFileNameWithoutExtension(mainPluginName)}( - Textures)?\d*$", RegexOptions.Compiled);
            var archiveFilePaths = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            var pluginFilePaths = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            foreach (var fileName in fs.Directory.EnumerateFiles(setup.DataDirectory))
            {
                var baseName = fs.Path.GetFileNameWithoutExtension(fileName);
                if (!regex.IsMatch(baseName))
                    continue;
                if (archiveProvider.IsArchiveFile(fileName))
                    archiveFilePaths.Add(baseName, fileName);
                else if (fs.Path.GetExtension(fileName).Equals(".esp", StringComparison.OrdinalIgnoreCase))
                    pluginFilePaths.Add(baseName, fileName);
            }
            // The only "checking" for bad archives happens when we try to read the file list - so do a complete dummy
            // read on all of the found archives.
            Parallel.ForEach(archiveFilePaths.Values, f => _ = archiveProvider.GetArchiveFileNames(f).ToList());
            var badArchivePaths = archiveProvider.GetBadArchivePaths().ToHashSet(StringComparer.CurrentCultureIgnoreCase);
            var mainBaseName = fs.Path.GetFileNameWithoutExtension(mainPluginName);
            return archiveFilePaths.Keys.Concat(pluginFilePaths.Keys)
                .Distinct()
                .Select(baseName => new MergeArchiveInfo
                {
                    ArchiveName = archiveFilePaths.TryGetValue(baseName, out var archivePath) ?
                        fs.Path.GetFileName(archivePath) : null,
                    DummyPluginName = pluginFilePaths.TryGetValue(baseName, out var pluginPath) ?
                        fs.Path.GetFileName(pluginPath) : null,
                    DummyPluginState = !string.IsNullOrEmpty(pluginPath) ?
                        GetPluginState(pluginPath) : PluginState.Missing,
                    IsReadable = !string.IsNullOrEmpty(archivePath) && !badArchivePaths.Contains(archivePath),
                    RequiresDummyPlugin =
                        !string.IsNullOrEmpty(archivePath) && !IsMainArchive(archivePath, mainBaseName),
                })
                .OrderBy(x => x.ArchiveName ?? x.DummyPluginName);
        }

        private PluginState GetPluginState(string fileNameOrPath)
        {
            var fileName = fs.Path.GetFileName(fileNameOrPath);
            // Checking the data directory to see if a plugin is present is more reliable than checking the load order.
            // There may be a bad plugins.txt referencing plugins that don't exist, especially if e.g. the data
            // directory is configured incorrectly.
            var path = fs.Path.Combine(setup.DataDirectory, fileName);
            if (!fs.File.Exists(path))
                return PluginState.Missing;
            if (!setup.LoadOrderGraph.IsEnabled(fileName))
                return PluginState.Disabled;
            return setup.LoadOrderGraph.CanLoad(fileName) ? PluginState.Enabled : PluginState.Unloadable;
        }

        private bool IsMainArchive(string archivePath, string mainBaseName)
        {
            var fileName = fs.Path.GetFileNameWithoutExtension(archivePath);
            return
                fileName.Equals(mainBaseName, StringComparison.CurrentCultureIgnoreCase) ||
                fileName.Equals($"{mainBaseName} - Textures", StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
