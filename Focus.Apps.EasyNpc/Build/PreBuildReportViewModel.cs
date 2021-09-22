#nullable enable

using Focus.Apps.EasyNpc.Reports;
using PropertyChanged;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Focus.Apps.EasyNpc.Build
{
    public class PreBuildReportViewModel : PreBuildReport
    {
        public ObservableCollection<SummaryItem> AlertSummaryItems { get; init; } = new();
        public IReadOnlyList<IncompatibleMod> IncompatibleMods { get; init; } =
            new List<IncompatibleMod>().AsReadOnly();
        public bool IsStale { get; set; }
        public int MasterCount => Masters.Count;
        public ObservableCollection<SummaryItem> NpcSummaryItems { get; init; } = new();
        public ObservableCollection<SummaryItem> OutputSummaryItems { get; init; } = new();
        public ObservableCollection<SummaryItem> PluginSummaryItems { get; init; } = new();
        public ResourceTree Resources { get; init; } = new ResourceTree();

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class NpcCounts : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public int Ignored { get; init; }
        public int Modded { get; init; }
        public int Total { get; init; }
        public int Unmoddable { get; init; }
        [DependsOn("Ignored", "Modded", "Total", "Unmoddable")]
        public int Unmodded => Total - Modded - Ignored - Unmoddable;
    }

    public class IncompatibleMod : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string PluginName { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;
        public string WinningModName { get; init; } = string.Empty;

        public IncompatibleMod()
        {
        }

        public IncompatibleMod(string pluginName, string winningModName, string reason)
        {
            PluginName = pluginName;
            WinningModName = winningModName;
            Reason = reason;
        }
    }

    public class ResourceDependency
    {
        public string FileName { get; init; } = string.Empty;
        public IReadOnlyList<ResourceSource> PreviousSources { get; init; } = new List<ResourceSource>().AsReadOnly();
        public ResourceSource? WinningSource { get; init; }
    }

    public class ResourceSource
    {
        public string ArchiveName { get; init; } = string.Empty;
        public string ModName { get; init; } = string.Empty;
    }

    public class ResourceTree : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public IReadOnlyList<ResourceTreeNode> RootNodes { get; init; } = new List<ResourceTreeNode>().AsReadOnly();
    }

    public class ResourceTreeNode : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public IReadOnlyList<ResourceDependency> Dependencies { get; init; } =
            new List<ResourceDependency>().AsReadOnly();
        public string RelativePath { get; init; } = string.Empty;
    }

    public class SamplePreBuildReport : PreBuildReportViewModel
    {
        public SamplePreBuildReport()
        {
            NpcSummaryItems = new(new[]
            {
                new SummaryItem(SummaryItemCategory.CountFull, "Total NPCs", 12345),
                new SummaryItem(SummaryItemCategory.CountIncluded, "Modded NPCs", 3049),
                new SummaryItem(SummaryItemCategory.CountExcluded, "Ignored NPCs", 241),
                new SummaryItem(SummaryItemCategory.CountEmpty, "Vanilla NPCs", 1763),
                new SummaryItem(SummaryItemCategory.CountUnavailable, "Unmoddable NPCs", 7292),
            });
            PluginSummaryItems = new(new []
            {
                new SummaryItem(SummaryItemCategory.StatusInfo, "Required masters", 23),
                new SummaryItem(SummaryItemCategory.StatusWarning, "Suspicious masters", 2),
                new SummaryItem(SummaryItemCategory.SpecialFlag, "Global replacers", 3),
                new SummaryItem(SummaryItemCategory.StatusInfo, "Merged overhauls", 65),
            });
            AlertSummaryItems = new(new []
            {
                new SummaryItem(SummaryItemCategory.StatusError, "Incompatible mods", 0),
                new SummaryItem(SummaryItemCategory.StatusError, "Invalid NPC profiles", 0),
                new SummaryItem(SummaryItemCategory.StatusWarning, "Missing resources", 0),
                new SummaryItem(SummaryItemCategory.StatusWarning, "Possible conflicts", 0),
                new SummaryItem(SummaryItemCategory.StatusInfo, "Suppressed warnings", 0),
            });
            OutputSummaryItems = new(new []
            {
                new SummaryItem(SummaryItemCategory.CountFull, "Output path is valid"),
                new SummaryItem(SummaryItemCategory.CountFull, "Output directory is empty"),
                new SummaryItem(SummaryItemCategory.CountFull, "GB before pack", 34.8m),
                new SummaryItem(SummaryItemCategory.CountFull, "GB packed (est.)", 3.7m),
            });
            Masters = new[]
            {
                new MasterDependency("Skyrim.esm"),
                new MasterDependency("Update.esm"),
                new MasterDependency("Dawnguard.esm"),
                new MasterDependency("HearthFires.esm"),
                new MasterDependency("Dragonborn.esm"),
                new MasterDependency("Unofficial Skyrim Special Edition Patch.esp"),
                new MasterDependency("Unofficial Skyrim Modders Patch.esp"),
                new MasterDependency("ApachiiHair.esm"),
                new MasterDependency("Serana.esp", true),
                new MasterDependency("SeranaPatch.esp", true),
            };
            IncompatibleMods = new[]
            {
                new IncompatibleMod
                {
                    PluginName = "ethereal_elven_overhaul.esp",
                    WinningModName = "Ethereal Elven Overhaul patches for SSE",
                    Reason = "Changes face attributes of a vanilla race",
                },
                new IncompatibleMod
                {
                    PluginName = "BorkBorkBork.esp",
                    WinningModName = "Some broken mod",
                    Reason = "Breaks various things"
                },
            };
        }
    }
}
