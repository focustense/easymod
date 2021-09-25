using Focus.Apps.EasyNpc.Profiles;
using Focus.Apps.EasyNpc.Reports;
using Focus.Environment;
using Focus.ModManagers;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Focus.Apps.EasyNpc.Build.Preview
{
    [AddINotifyPropertyChangedInterface]
    public class PluginViewModel
    {
        public static bool IsSuspiciousMasterCategory(PluginCategory category) => category switch
        {
            PluginCategory.NpcOverhaul or PluginCategory.NpcOverhaulPatch => true,
            _ => false,
        };

        public PluginCategory Category { get; private init; }
        public string CategoryDescription => Description.Of(Category);
        public ModComponentInfo? Component { get; private init; }
        [DependsOn(nameof(Category))]
        public bool IsSuspiciousMaster => IsSuspiciousMasterCategory(Category);
        public string PluginName { get; private init; }

        public PluginViewModel(string pluginName, ModComponentInfo? component, PluginCategory category)
        {
            PluginName = pluginName;
            Category = category;
            Component = component;
        }
    }

    [AddINotifyPropertyChangedInterface]
    public class PluginsViewModel
    {
        public delegate PluginsViewModel Factory(Profile profile);

        public IReadOnlyDictionary<string, PluginViewModel> FacePlugins { get; private set; } =
            new Dictionary<string, PluginViewModel>();
        [DependsOn(nameof(MasterPlugins))]
        public int MasterCount => MasterPlugins.Count;
        public IReadOnlyDictionary<string, PluginViewModel> MasterPlugins { get; private set; } =
            new Dictionary<string, PluginViewModel>();
        [DependsOn(nameof(FacePlugins), nameof(MasterPlugins))]
        public int MergedCount => FacePlugins.Keys.Count(p => !MasterPlugins.ContainsKey(p));
        [DependsOn(nameof(SuspiciousMasters))]
        public int SuspiciousMasterCount => SuspiciousMasters.Count();
        [DependsOn(nameof(MasterPlugins))]
        public IEnumerable<PluginViewModel> SuspiciousMasters => MasterPlugins.Values.Where(x => x.IsSuspiciousMaster);

        // Probably slow to regen entire list each time, only used as a test.
        [DependsOn(nameof(MasterCount), nameof(MergedCount), nameof(SuspiciousMasterCount))]
        public IEnumerable<SummaryItem> SummaryItems => new List<SummaryItem>
        {
            new(SummaryItemCategory.StatusInfo, "Required masters", MasterCount),
            // TODO: Only show "suspicious" if it's non-zero
            new(
                SuspiciousMasterCount > 0 ? SummaryItemCategory.StatusWarning : SummaryItemCategory.StatusOk,
                "Suspicious masters", SuspiciousMasterCount),
            new(SummaryItemCategory.StatusInfo, "Merged overhauls", MergedCount),
        }.AsReadOnly();

        private readonly IModRepository modRepository;
        private readonly IPluginCategorizer pluginCategorizer;

        public PluginsViewModel(
            IReadOnlyLoadOrderGraph loadOrderGraph, IPluginCategorizer pluginCategorizer, IModRepository modRepository,
            Profile profile)
        {
            this.modRepository = modRepository;
            this.pluginCategorizer = pluginCategorizer;

            Observable.CombineLatest(profile.Npcs.Select(x => x.DefaultOptionObservable))
                .SubscribeOn(NewThreadScheduler.Default)
                .Subscribe(defaultOptions =>
                {
                    var defaultPlugins = defaultOptions
                        .Select(x => x.PluginName)
                        .Distinct(StringComparer.CurrentCultureIgnoreCase);
                    var allMasters = defaultPlugins
                        .SelectMany(p => loadOrderGraph.GetAllMasters(p))
                        .Concat(defaultPlugins);
                    MasterPlugins = CreatePluginLookup(allMasters);
                });
            Observable.CombineLatest(profile.Npcs.Select(x => x.FaceOptionObservable))
                .SubscribeOn(NewThreadScheduler.Default)
                .Subscribe(faceOptions => FacePlugins = CreatePluginLookup(faceOptions.Select(x => x.PluginName)));
        }

        private IReadOnlyDictionary<string, PluginViewModel> CreatePluginLookup(IEnumerable<string> pluginNames)
        {
            return pluginNames
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .Select(DescribePlugin)
                .ToDictionary(x => x.PluginName, StringComparer.CurrentCultureIgnoreCase);
        }

        private PluginViewModel DescribePlugin(string pluginName)
        {
            var category = pluginCategorizer.GetCategory(pluginName);
            var providingComponent = modRepository.SearchForFiles(pluginName, false)
                .Select(x => x.ModComponent)
                .FirstOrDefault();
            return new(pluginName, providingComponent, category);
        }
    }
}
