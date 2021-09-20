using Focus.Analysis.Execution;
using Focus.Analysis.Plugins;
using Focus.Analysis.Records;
using Focus.Apps.EasyNpc.Build;
using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Environment;
using Focus.Files;
using Focus.ModManagers;
using System;
using System.Collections.Concurrent;
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

        private readonly ArchiveIndex archiveIndex;
        private readonly ConcurrentDictionary<string, AssetSource?> archiveSources =
            new(StringComparer.CurrentCultureIgnoreCase);
        private readonly IArchiveProvider archiveProvider;
        // Facegen editor must be lazy because it depends on the created environment.
        private readonly Lazy<IFaceGenEditor> faceGenEditor;
        private readonly IFileSystem fs;
        // Game settings must be lazy because it depends on the created environment.
        private readonly Lazy<IGameSettings> gameSettings;
        // Load order analyzer must be lazy because it depends on the created environment.
        private readonly Lazy<ILoadOrderAnalyzer> loadOrderAnalyzer;
        private readonly IConfigurableModRepository<ComponentPerDirectoryConfiguration> modRepository;
        private readonly IModSettings modSettings;
        private readonly IGameSetup setup;
        private readonly BehaviorSubject<string> status = new("Waiting to start");

        private Dictionary<IRecordKey, HeadPartAnalysis> headParts = new();
        private readonly ModComponentInfo vanillaComponent;

        public PostBuildReportGenerator(
            IConfigurableModRepository<ComponentPerDirectoryConfiguration> modRepository, IModSettings modSettings,
            IGameSetup setup, Lazy<IGameSettings> gameSettings, IFileSystem fs, IArchiveProvider archiveProvider,
            Lazy<ILoadOrderAnalyzer> loadOrderAnalyzer, Lazy<IFaceGenEditor> faceGenEditor)
        {
            this.archiveProvider = archiveProvider;
            this.faceGenEditor = faceGenEditor;
            this.fs = fs;
            this.gameSettings = gameSettings;
            this.loadOrderAnalyzer = loadOrderAnalyzer;
            this.modRepository = modRepository;
            this.modSettings = modSettings;
            this.setup = setup;

            archiveIndex = new(archiveProvider);
            vanillaComponent = new ModComponentInfo(ModLocatorKey.Empty, "Vanilla", "Vanilla", setup.DataDirectory);
        }

        public async Task<PostBuildReport> CreateReport()
        {
            status.OnNext("Starting up");
            await Task.Run(() => setup.Confirm());
            // Start mod indexing right away so we can do other things while it's running.
            status.OnNext("Beginning mod indexing");
            var modRepositoryConfigTask = Task.Run(() => modRepository.Configure(new(modSettings.RootDirectory)));
            var archiveIndexTask = Task.Run(() =>
            {
                var archivePaths = gameSettings.Value.ArchiveOrder
                    .Select(f => fs.Path.Combine(gameSettings.Value.DataDirectory, f));
                archiveIndex.AddArchives(archivePaths);
            });
            // Start the analysis right away so we can do other things while it's running.
            status.OnNext("Beginning analysis");
            var analysisTask = Task.Run(() => loadOrderAnalyzer.Value.Analyze(
                setup.AvailablePlugins.Select(x => x.FileName), setup.LoadOrderGraph, true));
            status.OnNext("Checking plugins and archives");
            var mainPluginName = FileStructure.MergeFileName;
            var archives = await Task.Run(() => CheckArchives(mainPluginName)
                .OrderBy(x => x.ArchiveName?.IndexOf("- Textures") ?? -1)
                .ThenBy(x => x.ArchiveName)
                .ToList()
                .AsReadOnly());
            status.OnNext("Waiting for analysis to finish");
            var analysis = await analysisTask;
            headParts = analysis.ExtractChains<HeadPartAnalysis>(RecordType.HeadPart)
                .ToDictionary(x => x.Key, x => x.Winner, RecordKeyComparer.Default);
            status.OnNext("Waiting for mod indexing to finish");
            await Task.WhenAll(archiveIndexTask, modRepositoryConfigTask);
            var mergeComponents = modRepository.SearchForFiles(mainPluginName, false)
                .Select(x => x.ModComponent)
                .Distinct()
                .ToList();
            status.OnNext("Checking NPC consistency");
            var mainMergeComponent = mergeComponents.Count == 1 ? mergeComponents[0] : null;
            var npcs = (await Task.Run(() => CheckNpcs(analysis, mainPluginName, mainMergeComponent)))
                .ToList().AsReadOnly();
            status.OnNext("Finishing the report");
            var report = new PostBuildReport
            {
                MainPluginMissingMasters = setup.LoadOrderGraph.GetMissingMasters(mainPluginName).ToList().AsReadOnly(),
                MainPluginName = mainPluginName,
                MainPluginState = GetPluginState(mainPluginName),
                ActiveMergeComponents = mergeComponents,
                Archives = archives,
                Npcs = npcs,
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

        private async Task<NpcConsistencyInfo> CheckNpc(
            RecordAnalysisChain<NpcAnalysis> chain, string mainPluginName, ModComponentInfo? mergeComponent)
        {
            var main = chain[mainPluginName];
            var winner = chain.Winner;
            var winningPluginName = chain[^1].PluginName;
            var pluginSource = FindAssetSource(winningPluginName, false);
            var faceGenPath = FileStructure.GetFaceMeshFileName(chain.Key);
            var faceTintPath = FileStructure.GetFaceTintFileName(chain.Key);
            var faceGenSource = FindAssetSource(faceGenPath, true);
            var faceTintSource = FindAssetSource(faceTintPath, true);
            string? faceGenArchivePath = null;
            string? faceGenLoosePath = null;
            string? faceTintArchivePath = null;
            string? faceTintLoosePath = null;
            if (mergeComponent is not null)
            {
                var mergedFaceGens = modRepository.SearchForFiles(new[] { mergeComponent }, faceGenPath, true).ToList();
                faceGenArchivePath = mergedFaceGens
                    .Where(x => !string.IsNullOrEmpty(x.ArchiveName))
                    .Select(x => fs.Path.Combine(mergeComponent.Path, x.ArchiveName))
                    .FirstOrDefault();
                faceGenLoosePath = mergedFaceGens
                    .Where(x => string.IsNullOrEmpty(x.ArchiveName))
                    .Select(x => fs.Path.Combine(mergeComponent.Path, x.RelativePath))
                    .FirstOrDefault();
                var mergedFaceTints = modRepository.SearchForFiles(new[] { mergeComponent }, faceTintPath, true).ToList();
                faceTintArchivePath = mergedFaceTints
                    .Where(x => !string.IsNullOrEmpty(x.ArchiveName))
                    .Select(x => fs.Path.Combine(mergeComponent.Path, x.ArchiveName))
                    .FirstOrDefault();
                faceTintLoosePath = mergedFaceTints
                    .Where(x => string.IsNullOrEmpty(x.ArchiveName))
                    .Select(x => fs.Path.Combine(mergeComponent.Path, x.RelativePath))
                    .FirstOrDefault();
            }
            return new NpcConsistencyInfo
            {
                BasePluginName = chain.Key.BasePluginName,
                LocalFormIdHex = chain.Key.LocalFormIdHex,
                EditorId = chain.Winner.EditorId,
                FaceGenArchivePath = faceGenArchivePath,
                FaceGenLoosePath = faceGenLoosePath,
                FaceTintArchivePath = faceTintArchivePath,
                FaceTintLoosePath = faceTintLoosePath,
                HasConsistentFaceTint =
                    // Don't care about the face tint unless EasyNPC is (or could be) providing the facegen.
                    string.IsNullOrEmpty(faceGenArchivePath) || string.IsNullOrEmpty(faceGenLoosePath) ||
                    faceTintSource?.ModComponent == faceGenSource?.ModComponent,
                HasConsistentHeadParts = await HasConsistentHeadParts(winner, pluginSource, faceGenSource),
                Name = chain.Winner.Name,
                WinningPluginName = winningPluginName,
                WinningPluginSource = pluginSource,
                WinningFaceGenSource = faceGenSource,
                WinningFaceTintSource = faceTintSource,
            };
        }

        private async Task<IEnumerable<NpcConsistencyInfo>> CheckNpcs(
            LoadOrderAnalysis analysis, string mainPluginName, ModComponentInfo? mergeComponent)
        {
            var loadOrder = setup.AvailablePlugins.Select(x => x.FileName);
            var npcTasks = analysis
                .ExtractChains<NpcAnalysis>(RecordType.Npc)
                .AsParallel()
                .Where(x => x.Winner.TemplateInfo?.InheritsTraits != true && x.Contains(mainPluginName))
                .Select(x => CheckNpc(x, mainPluginName, mergeComponent));
            var npcs = await Task.WhenAll(npcTasks);
            return npcs.OrderByLoadOrder(x => x, loadOrder);
        }

        private bool FileContentsEqual(IFileInfo info1, IFileInfo info2)
        {
            const int BUFFER_SIZE = 8192;

            using var fs1 = info1.OpenRead();
            using var fs2 = info2.OpenRead();
            var buffer1 = new byte[BUFFER_SIZE];
            var buffer2 = new byte[BUFFER_SIZE];
            var span1 = new ReadOnlySpan<byte>(buffer1);
            var span2 = new ReadOnlySpan<byte>(buffer2);
            while (true)
            {
                int length1 = fs1.Read(buffer1);
                int length2 = fs2.Read(buffer2);
                if (length1 != length2)
                    return false;
                if (length1 == 0)
                    return true;
                if (!span1.SequenceEqual(span2))
                    return false;
            }
        }

        private AssetSource? FindAssetSource(string assetPath, bool checkArchives)
        {
            var settings = gameSettings.Value;
            var loosePath = fs.Path.Combine(settings.DataDirectory, assetPath);
            if (fs.File.Exists(loosePath))
                return FindAssetSource(assetPath, loosePath);
            if (!checkArchives)
                return null;
            var containingArchiveName = archiveIndex.FindInBuckets(assetPath)
                .Select(x => fs.Path.GetFileName(x.Key))
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(containingArchiveName))
            {
                if (gameSettings.Value.IsBaseGameArchive(containingArchiveName))
                    return new AssetSource
                    {
                        ArchiveName = containingArchiveName,
                        ModComponent = vanillaComponent,
                        RelativePath = assetPath,
                    };
                var archiveSource = archiveSources.GetOrAdd(
                    containingArchiveName,
                    _ => FindAssetSource(
                        containingArchiveName, fs.Path.Combine(settings.DataDirectory, containingArchiveName)));
                if (archiveSource is not null)
                    return new AssetSource
                    {
                        ArchiveName = containingArchiveName,
                        ModComponent = archiveSource.ModComponent,
                        RelativePath = assetPath,
                    };
            }
            return null;
        }

        private AssetSource? FindAssetSource(string assetPath, string absoluteGamePath)
        {
            var targetInfo = fs.FileInfo.FromFileName(absoluteGamePath);
            var allResults = modRepository.SearchForFiles(assetPath, false).ToList();
            if (allResults.Count == 1)
                return ResultToSource(allResults[0]);
            // If search results are in listed (reverse priority) order, this is accurate, and if they aren't in order,
            // then there is probably no way to be more accurate.
            for (int i = allResults.Count - 1; i >= 0; i--)
            {
                var result = allResults[i];
                var resultPath = fs.Path.Combine(result.ModComponent.Path, assetPath);
                var resultInfo = fs.FileInfo.FromFileName(resultPath);
                if (targetInfo.Length != resultInfo.Length)
                    continue;
                if (FileContentsEqual(targetInfo, resultInfo))
                    return ResultToSource(result);
            }
            return null;
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

        private async Task<bool> HasConsistentHeadParts(
            NpcAnalysis npc, AssetSource? pluginSource, AssetSource? faceGenSource)
        {
            if (faceGenSource is null)
                return npc.ComparisonToBase?.ModifiesHeadParts != true;
            if (pluginSource is null)
                return false;
            if (faceGenSource.ModComponent == pluginSource.ModComponent)
                // Shortcut/optimization: assume that each mod is self-consistent. If the plugin source and facegen
                // source both belong to the same component, don't deep-check for consistency.
                return true;
            var recordHeadParts = npc.MainHeadParts.Select(x => headParts.GetOrDefault(x)).NotNull().ToList();
            var extraPartKeys = recordHeadParts.SelectMany(x => x.ExtraPartKeys);
            while (extraPartKeys.Any())
            {
                var extraParts = extraPartKeys.Select(x => headParts.GetOrDefault(x)).NotNull().ToList();
                recordHeadParts.AddRange(extraParts);
                extraPartKeys = extraParts.SelectMany(x => x.ExtraPartKeys);
            }
            // TODO: Should this set use a case-insensitive comparer? Does the game care when it comes to Editor IDs?
            // Some head parts are "invisible", i.e. are used as placeholders and don't have a model. These won't appear
            // in the facegen file.
            var recordHeadPartNames = recordHeadParts
                .Where(x => !string.IsNullOrEmpty(x.ModelFileName))
                .Select(x => x.EditorId)
                .ToHashSet();
            var faceGenData = await ReadAllBytes(faceGenSource);
            var faceGenPath = FileStructure.GetFaceMeshFileName(npc);
            var faceGenHeadPartNames = faceGenEditor.Value.GetHeadPartNames(faceGenPath, faceGenData);
            return recordHeadPartNames.SetEquals(faceGenHeadPartNames);
        }

        private bool IsMainArchive(string archivePath, string mainBaseName)
        {
            var fileName = fs.Path.GetFileNameWithoutExtension(archivePath);
            return
                fileName.Equals(mainBaseName, StringComparison.CurrentCultureIgnoreCase) ||
                fileName.Equals($"{mainBaseName} - Textures", StringComparison.CurrentCultureIgnoreCase);
        }

        private async Task<byte[]> ReadAllBytes(AssetSource source)
        {
            if (!string.IsNullOrEmpty(source.ArchiveName))
            {
                var archivePath = fs.Path.Combine(source.ModComponent.Path, source.ArchiveName);
                return archiveProvider.ReadBytes(archivePath, source.RelativePath).ToArray();
            }

            var filePath = fs.Path.Combine(source.ModComponent.Path, source.RelativePath);
            return await fs.File.ReadAllBytesAsync(filePath);
        }

        private static AssetSource ResultToSource(ModSearchResult result)
        {
            return new AssetSource
            {
                ArchiveName = result.ArchiveName,
                ModComponent = result.ModComponent,
                RelativePath = result.RelativePath,
            };
        }
    }
}
