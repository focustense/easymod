using Focus.Analysis.Execution;
using Focus.Analysis.Plugins;
using Focus.Analysis.Records;
using Focus.Apps.EasyNpc.Profiles;
using Focus.Apps.EasyNpc.Reports;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Build.Preview
{
    public enum NpcSummaryFilter { All, Ignored, Modded, Unmoddable, Unmodded }

    [AddINotifyPropertyChangedInterface]
    public class NpcSummaryRow : IRecordKey
    {
        public string BasePluginName { get; private init; }
        public string DefaultPluginName { get; private set; } = string.Empty;
        public string EditorId { get; private init; }
        public string? FaceGenModName { get; private set; }
        public string FacePluginName { get; private set; } = string.Empty;
        public NpcSummaryFilter Filter { get; private init; }
        public string LocalFormIdHex { get; private init; }
        public string Name { get; private init; }

        public NpcSummaryRow(Npc npc)
        {
            BasePluginName = npc.BasePluginName;
            LocalFormIdHex = npc.LocalFormIdHex;
            EditorId = npc.EditorId;
            Name = npc.Name;
            Filter = npc.HasAvailableFaceCustomizations ? NpcSummaryFilter.Modded : NpcSummaryFilter.Unmodded;
            if (npc.HasAvailableFaceCustomizations)
            {
                npc.DefaultOptionObservable.Subscribe(x => DefaultPluginName = x.PluginName);
                npc.FaceGenOverrideObservable.Subscribe(x => FaceGenModName = x?.Name);
                npc.FaceOptionObservable.Subscribe(x => FacePluginName = x.PluginName);
            }
        }

        public NpcSummaryRow(RecordAnalysisChain<NpcAnalysis> chain)
        {
            BasePluginName = chain.Key.BasePluginName;
            LocalFormIdHex = chain.Key.LocalFormIdHex;
            EditorId = chain.Winner.EditorId;
            Name = chain.Winner.Name;
            // Modding-related columns aren't populated for these. Using this constructor implies that the NPC is not
            // in the profile and therefore can't be modded.
            Filter = NpcSummaryFilter.Unmoddable;
        }
    }

    [AddINotifyPropertyChangedInterface]
    public class NpcSummaryViewModel
    {
        public delegate NpcSummaryViewModel Factory(Profile profile, LoadOrderAnalysis analysis);

        public NpcSummaryFilter Filter { get; set; } = NpcSummaryFilter.All;
        [DependsOn(nameof(Filter))]
        public IEnumerable<NpcSummaryRow> FilteredRows =>
            Filter == NpcSummaryFilter.All ? rows : rows.Where(x => x.Filter == Filter);
        public int IgnoredCount { get; private set; }
        public int ModdedCount { get; private set; }
        public int TotalCount { get; private set; }
        [DependsOn(nameof(IgnoredCount), nameof(ModdedCount), nameof(TotalCount), nameof(UnmoddedCount))]
        public int UnmoddableCount => TotalCount - IgnoredCount - ModdedCount - UnmoddedCount;
        public int UnmoddedCount { get; private set; }

        [DependsOn(nameof(IgnoredCount), nameof(ModdedCount), nameof(TotalCount), nameof(UnmoddableCount), nameof(UnmoddedCount))]
        public IEnumerable<SummaryItem> SummaryItems => new List<SummaryItem>
        {
            new(SummaryItemCategory.CountFull, "Total NPCs", TotalCount),
            new(SummaryItemCategory.CountIncluded, "Modded NPCs", ModdedCount),
            new(SummaryItemCategory.CountExcluded, "Ignored NPCs", IgnoredCount),
            new(SummaryItemCategory.CountEmpty, "Vanilla NPCs", UnmoddedCount),
            new(SummaryItemCategory.CountUnavailable, "Unmoddable NPCs", UnmoddableCount),
        }.AsReadOnly();

        private readonly IReadOnlyList<NpcSummaryRow> rows;

        public NpcSummaryViewModel(
            IGameSettings gameSettings, IProfilePolicy policy, Profile profile, LoadOrderAnalysis analysis)
        {
            var npcChains = analysis.ExtractChains<NpcAnalysis>(RecordType.Npc).ToList();
            ModdedCount = profile.Count;
            UnmoddedCount = profile.Hidden.Count;
            rows = profile.Npcs.Concat(profile.Hidden.Npcs)
                .Select(x => new NpcSummaryRow(x))
                .Concat(npcChains.Where(x => !policy.IsModdable(x)).Select(x => new NpcSummaryRow(x)))
                .OrderByLoadOrder(x => x, gameSettings.PluginLoadOrder)
                .ToList()
                .AsReadOnly();
            TotalCount = rows.Count;
        }
    }
}
