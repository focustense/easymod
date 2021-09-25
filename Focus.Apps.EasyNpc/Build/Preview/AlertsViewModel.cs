using Focus.Apps.EasyNpc.Build.Checks;
using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Profiles;
using Focus.Apps.EasyNpc.Reports;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;

namespace Focus.Apps.EasyNpc.Build.Preview
{
    [AddINotifyPropertyChangedInterface]
    public class AlertsViewModel
    {
        public delegate AlertsViewModel Factory(Profile profile, IObservable<BuildSettings> buildSettings);

        public IEnumerable<BuildWarning> GlobalWarnings { get; private set; } = Enumerable.Empty<BuildWarning>();
        public IEnumerable<BuildWarning> NpcWarnings { get; private set; } = Enumerable.Empty<BuildWarning>();
        public BuildWarning? SelectedWarning { get; set; }
        public IEnumerable<SummaryItem> SummaryItems { get; private set; } = Enumerable.Empty<SummaryItem>();

        [DependsOn(nameof(GlobalWarnings), nameof(NpcWarnings))]
        public IEnumerable<BuildWarning> Warnings => GlobalWarnings.Concat(NpcWarnings)
            .OrderBy(x => x.Id)
            .ThenBy(x => x.PluginName, StringComparer.CurrentCultureIgnoreCase)
            .ThenByLoadOrder(x => x.RecordKey ?? RecordKey.Null, gameSettings.PluginLoadOrder);

        private readonly IObservableAppSettings appSettings;
        private readonly IObservable<BuildSettings> buildSettings;
        private readonly IGameSettings gameSettings;
        private readonly IEnumerable<IGlobalBuildCheck> globalChecks;
        private readonly IEnumerable<INpcBuildCheck> npcChecks;
        private readonly Dictionary<IRecordKey, IReadOnlyList<BuildWarning>> npcWarnings =
            new(RecordKeyComparer.Default);
        private readonly Profile profile;

        public AlertsViewModel(
            IObservableAppSettings appSettings, IGameSettings gameSettings, IEnumerable<IGlobalBuildCheck> globalChecks,
            IEnumerable<INpcBuildCheck> npcChecks, Profile profile, IObservable<BuildSettings> buildSettings)
        {
            this.appSettings = appSettings;
            this.buildSettings = buildSettings;
            this.gameSettings = gameSettings;
            this.globalChecks = globalChecks;
            this.npcChecks = npcChecks;
            this.profile = profile;
        }

        public void BeginWatching()
        {
            buildSettings
                .SubscribeOn(NewThreadScheduler.Default)
                .Select(settings => globalChecks.SelectMany(x => x.Run(profile, settings)).ToList())
                .ObserveOn(Application.Current.Dispatcher)
                .Subscribe(x => GlobalWarnings = x.AsReadOnly());
            foreach (var npc in profile.Npcs)
            {
                Observable
                    .CombineLatest(
                        buildSettings, npc.DefaultOptionObservable, npc.FaceOptionObservable,
                        npc.FaceGenOverrideObservable,
                        (settings, _, _, _) => (npc, settings))
                    .SubscribeOn(NewThreadScheduler.Default)
                    .Select(x => npcChecks.SelectMany(c => c.Run(x.npc, x.settings)).ToList())
                    .ObserveOn(Application.Current.Dispatcher)
                    .Subscribe(warnings =>
                    {
                        if (warnings.Count > 0)
                            npcWarnings[npc] = warnings;
                        else
                            npcWarnings.Remove(npc);
                        NpcWarnings = npcWarnings.Values.SelectMany(warnings => warnings);
                        UpdateSummaryItems();
                    });
            }
            appSettings.BuildWarningWhitelistObservable.Subscribe(_ => UpdateSummaryItems());
        }

        protected void OnWarningsChanged()
        {
            UpdateSummaryItems();
        }

        private ILookup<string, BuildWarningId> GetBuildWarningSuppressions()
        {
            return appSettings.BuildWarningWhitelist
                .SelectMany(x => x.IgnoredWarnings.Select(id => new { Plugin = x.PluginName, Id = id }))
                .ToLookup(x => x.Plugin, x => x.Id);
        }

        private static SummaryItemCategory PickCategory(int issueCount, SummaryItemCategory nonZeroCategory)
        {
            return issueCount > 0 ? nonZeroCategory : SummaryItemCategory.StatusOk;
        }

        private void UpdateSummaryItems()
        {
            int criticalCount = 0, conflictCount = 0, missingResourceCount = 0, otherCount = 0, suppressedCount = 0;
            var suppressions = GetBuildWarningSuppressions();
            foreach (var warning in Warnings)
            {
                if (!string.IsNullOrEmpty(warning.PluginName) &&
                    warning.Id != null &&
                    suppressions[warning.PluginName].Contains(warning.Id.Value))
                {
                    suppressedCount++;
                    continue;
                }
                switch (warning.Id)
                {
                    case BuildWarningId.ModDirectoryNotFound:
                    case BuildWarningId.ModDirectoryNotSpecified:
                        criticalCount++;
                        break;
                    case BuildWarningId.MissingFaceGen:
                        conflictCount++;
                        break;
                    default:
                        otherCount++;
                        break;

                }
            }
            SummaryItems = new SummaryItem[]
            {
                new(PickCategory(criticalCount, SummaryItemCategory.StatusError), "Critical issues", criticalCount),
                new(
                    PickCategory(missingResourceCount, SummaryItemCategory.StatusError),
                    "Missing resources", missingResourceCount),
                new(PickCategory(conflictCount, SummaryItemCategory.StatusError), "NPC conflicts", conflictCount),
                new(PickCategory(otherCount, SummaryItemCategory.StatusWarning), "Other issues", otherCount),
                new(SummaryItemCategory.StatusInfo, "Suppressed warnings", suppressedCount),
            };
        }
    }
}
