using Focus.Analysis.Execution;
using Focus.Analysis.Plugins;
using Focus.Analysis.Records;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Apps.EasyNpc.Profiles;
using Focus.Apps.EasyNpc.Reports;
using Focus.Files;
using Focus.ModManagers;
using PropertyChanged;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Focus.Apps.EasyNpc.Build.Preview
{
    record FaceGenInfo(long FaceGenSizeBytes, long FaceTintSizeBytes);

    [AddINotifyPropertyChangedInterface]
    public class AssetsViewModel : IDisposable
    {
        public delegate AssetsViewModel Factory(Profile profile, LoadOrderAnalysis analysis);

        private static readonly RecordType[] NonCriticalSourceRecordTypes = new[] { RecordType.HeadPart };

        private const int KB = 1024;
        private const int MB = KB * 1024;
        private const int GB = MB * 1024;

        public IReadOnlyList<AssetReference> MissingAssets { get; private set; } =
            new List<AssetReference>().AsReadOnly();
        public ulong UncompressedSizeBytes { get; private set; }
        [DependsOn(nameof(UncompressedSizeBytes))]
        public decimal UncompressedSizeGb => Math.Round((decimal)UncompressedSizeBytes / GB, 1);

        [DependsOn(nameof(MissingAssets))]
        public IEnumerable<SummaryItem> AlertSummaryItems => new SummaryItem[]
        {
            isInitialSizeComputed ?
                MissingAssets.Count > 0 ?
                    new(SummaryItemCategory.StatusWarning, "Missing assets", MissingAssets.Count) :
                    new(SummaryItemCategory.StatusOk, "All assets found") :
                new(SummaryItemCategory.StatusInfo, "Checking assets..."),
        };

        [DependsOn(nameof(UncompressedSizeGb))]
        public IEnumerable<SummaryItem> SummaryItems => new SummaryItem[]
        {
            isInitialSizeComputed ?
                new(SummaryItemCategory.StatusInfo, "GB assets", UncompressedSizeGb) :
                new(SummaryItemCategory.StatusInfo, "Calculating mod size..."),
            isInitialSizeComputed ?
                new(SummaryItemCategory.StatusInfo, "GB packed (est.)", 0) :
                new(SummaryItemCategory.StatusInfo, "Calculating compressed size..."),
        };

        private readonly IArchiveProvider archiveProvider;
        private readonly ConcurrentDictionary<string, ulong> assetSizes = new(PathComparer.Default);
        private readonly Subject<bool> changes = new();
        private readonly Subject<bool> disposed = new();
        private readonly IFileProvider fileProvider;
        private readonly IFileSystem fs;
        private bool isInitialSizeComputed;
        private readonly IModRepository modRepository;
        private readonly ConcurrentDictionary<IRecordKey, IEnumerable<AssetReference>> npcAssets = new();
        private readonly Dictionary<IRecordKey, RecordAnalysisChain<NpcAnalysis>> npcChains;
        private readonly ConcurrentDictionary<IRecordKey, FaceGenInfo> npcFaceGens = new();

        public AssetsViewModel(
            IFileSystem fs, IFileProvider fileProvider, IArchiveProvider archiveProvider, IModRepository modRepository,
            Profile profile, LoadOrderAnalysis analysis)
        {
            this.archiveProvider = archiveProvider;
            this.fileProvider = fileProvider;
            this.fs = fs;
            this.modRepository = modRepository;
            npcChains = analysis.ExtractChains<NpcAnalysis>(RecordType.Npc)
                .ToDictionary(x => x.Key, RecordKeyComparer.Default);
            changes
                .Throttle(TimeSpan.FromMilliseconds(100))
                .ObserveOn(NewThreadScheduler.Default)
                .Synchronize()
                .TakeUntil(disposed)
                .Subscribe(_ => RefreshAssetSizes());
            Observable
                .Merge(
                    profile.Npcs.SelectMany(npc => new[]
                    {
                        npc.FaceOptionObservable.Select(obs => npc),
                        npc.FaceGenOverrideObservable.Select(obs => npc),
                    }))
                .ObserveOn(NewThreadScheduler.Default)
                .TakeUntil(disposed)
                .Subscribe(npc =>
                {
                    if (!npcChains.TryGetValue(npc, out var chain))
                        return;
                    // Chain should always contain the plugin for it to have been selectable in the first place, but
                    // check anyway to be safe.
                    npcAssets[chain.Key] = chain.Contains(npc.FaceOption.PluginName) ?
                        chain[npc.FaceOption.PluginName].Analysis.ReferencedAssets : Enumerable.Empty<AssetReference>();
                    npcFaceGens[chain.Key] = GetFaceGenInfo(npc);
                    changes.OnNext(true);
                });
        }

        public void Dispose()
        {
            disposed.OnNext(true);
            GC.SuppressFinalize(this);
        }

        private FaceGenInfo GetFaceGenInfo(Npc npc)
        {
            var faceGenPath = FileStructure.GetFaceMeshFileName(npc);
            var faceTintPath = FileStructure.GetFaceTintFileName(npc);
            var candidateComponents = npc.FaceGenOverride is not null ?
                npc.FaceGenOverride.Components :
                modRepository.SearchForFiles(npc.FaceOption.PluginName, false).Select(x => x.ModComponent).ToList();
            var faceGenResult = modRepository.SearchForFiles(candidateComponents, faceGenPath, true).FirstOrDefault();
            var faceTintResult = modRepository.SearchForFiles(candidateComponents, faceTintPath, true).FirstOrDefault();
            return new(GetFileSize(faceGenResult), GetFileSize(faceTintResult));
        }

        private long GetFileSize(ModSearchResult? result)
        {
            if (result is null)
                return 0;
            return !string.IsNullOrEmpty(result.ArchiveName) ?
                archiveProvider.GetArchiveFileSize(
                    fs.Path.Combine(result.ModComponent.Path, result.ArchiveName), result.RelativePath) :
                fs.FileInfo.FromFileName(fs.Path.Combine(result.ModComponent.Path, result.RelativePath)).Length;
        }

        private void RefreshAssetSizes()
        {
            var allAssets = npcAssets.Values
                .SelectMany(refs => refs)
                .Distinct()
                .ToList();
            var sharedAssetsSizeBytes = allAssets
                // AsParallel would speed this up considerably on the first run, but can make the UI seem sluggish on
                // startup due to hogging cores, and since the user will usually take at least a few seconds to actually
                // get to this screen, the tradeoff isn't really worth it.
                .Select(x => assetSizes.GetOrAdd(x.NormalizedPath, path => fileProvider.GetSize(path)))
                .Aggregate((sum, value) => sum + value);
            var faceGenAssetsSizeBytes = npcFaceGens.Values.Sum(x => x.FaceGenSizeBytes + x.FaceTintSizeBytes);
            isInitialSizeComputed = true;
            UncompressedSizeBytes = sharedAssetsSizeBytes + (ulong)faceGenAssetsSizeBytes;
            MissingAssets = allAssets
                .Where(x => !x.SourceRecordTypes.SetEquals(NonCriticalSourceRecordTypes))
                .Where(x => assetSizes.GetOrDefault(x.NormalizedPath) == 0)
                .ToList()
                .AsReadOnly();
        }
    }
}
