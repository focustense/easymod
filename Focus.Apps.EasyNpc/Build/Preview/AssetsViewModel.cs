using Focus.Analysis.Execution;
using Focus.Analysis.Plugins;
using Focus.Analysis.Records;
using Focus.Apps.EasyNpc.Profiles;
using Focus.Apps.EasyNpc.Reports;
using Focus.Files;
using PropertyChanged;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Focus.Apps.EasyNpc.Build.Preview
{
    [AddINotifyPropertyChangedInterface]
    public class AssetsViewModel : IDisposable
    {
        public delegate AssetsViewModel Factory(Profile profile, LoadOrderAnalysis analysis);

        private const int KB = 1024;
        private const int MB = KB * 1024;
        private const int GB = MB * 1024;

        public ulong UncompressedSizeBytes { get; private set; }
        [DependsOn(nameof(UncompressedSizeBytes))]
        public decimal UncompressedSizeGb => Math.Round((decimal)UncompressedSizeBytes / GB, 1);

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

        private readonly ConcurrentDictionary<string, ulong> assetSizes = new(PathComparer.Default);
        private readonly Subject<bool> changes = new();
        private readonly Subject<bool> disposed = new();
        private readonly IFileProvider fileProvider;
        private bool isInitialSizeComputed;
        private readonly ConcurrentDictionary<IRecordKey, IEnumerable<AssetReference>> npcAssets = new();
        private readonly Dictionary<IRecordKey, RecordAnalysisChain<NpcAnalysis>> npcChains;

        public AssetsViewModel(IFileProvider fileProvider, Profile profile, LoadOrderAnalysis analysis)
        {
            this.fileProvider = fileProvider;
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
                    changes.OnNext(true);
                });
        }

        public void Dispose()
        {
            disposed.OnNext(true);
            GC.SuppressFinalize(this);
        }

        private void RefreshAssetSizes()
        {
            UncompressedSizeBytes = npcAssets.Values
                // AsParallel would speed this up considerably on the first run, but can make the UI seem sluggish on
                // startup due to hogging cores, and since the user will usually take at least a few seconds to actually
                // get to this screen, the tradeoff isn't really worth it.
                .SelectMany(refs => refs)
                .Distinct()
                .Select(x => assetSizes.GetOrAdd(x.NormalizedPath, path => fileProvider.GetSize(path)))
                .Aggregate((sum, value) => sum + value);
            isInitialSizeComputed = true;
        }
    }
}
