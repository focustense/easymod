using Focus.Apps.EasyNpc.Build.Checks;
using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Profiles;
using Focus.Apps.EasyNpc.Reports;
using PropertyChanged;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Preview
{
    [AddINotifyPropertyChangedInterface]
    public class AlertsViewModel : IDisposable
    {
        public delegate AlertsViewModel Factory(Profile profile, IObservable<BuildSettings> buildSettings);

        public IEnumerable<BuildWarning> GlobalWarnings { get; private set; } = Enumerable.Empty<BuildWarning>();
        public IEnumerable<SummaryItem> HeaderSummaryItems { get; private set; } = Enumerable.Empty<SummaryItem>();
        public IEnumerable<SummaryItem> MainSummaryItems { get; private set; } = Enumerable.Empty<SummaryItem>();
        public IEnumerable<BuildWarning> NpcWarnings { get; private set; } = Enumerable.Empty<BuildWarning>();
        public IObservable<ErrorLevel> OverallErrorLevel => overallErrorLevel;
        public BuildWarning? SelectedWarning { get; set; }

        [DependsOn(nameof(GlobalWarnings), nameof(NpcWarnings))]
        public IEnumerable<BuildWarning> Warnings => GlobalWarnings.Concat(NpcWarnings)
            .OrderByDescending(x => x.Severity)
            .ThenBy(x => x.Id)
            .ThenBy(x => x.PluginName, StringComparer.CurrentCultureIgnoreCase)
            .ThenByLoadOrder(x => x.RecordKey ?? RecordKey.Null, gameSettings.PluginLoadOrder);

        private readonly IObservableAppSettings appSettings;
        private readonly IObservable<BuildSettings> buildSettings;
        private readonly Subject<bool> disposed = new();
        private readonly IGameSettings gameSettings;
        private readonly IEnumerable<IGlobalBuildCheck> globalChecks;
        private readonly IObservableModSettings modSettings;
        private readonly IEnumerable<INpcBuildCheck> npcChecks;
        private readonly ConcurrentDictionary<IRecordKey, IReadOnlyList<BuildWarning>> npcWarnings =
            new(RecordKeyComparer.Default);
        private readonly BehaviorSubject<ErrorLevel> overallErrorLevel = new(ErrorLevel.None);
        private readonly Profile profile;

        public AlertsViewModel(
            IObservableAppSettings appSettings, IGameSettings gameSettings, IObservableModSettings modSettings,
            IEnumerable<IGlobalBuildCheck> globalChecks, IEnumerable<INpcBuildCheck> npcChecks, Profile profile,
            IObservable<BuildSettings> buildSettings)
        {
            this.appSettings = appSettings;
            this.buildSettings = buildSettings;
            this.gameSettings = gameSettings;
            this.globalChecks = globalChecks;
            this.modSettings = modSettings;
            this.npcChecks = npcChecks;
            this.profile = profile;
        }

        public void BeginWatching()
        {
            var throttledBuildSettings = buildSettings
                .Throttle(TimeSpan.FromSeconds(0.5))
                .DistinctUntilChanged(x => x.EnableDewiggify)
                .Publish();
            throttledBuildSettings
                .ObserveOn(NewThreadScheduler.Default)
                .TakeUntil(disposed)
                .Subscribe(settings =>
                {
                    Parallel.ForEach(profile.Npcs, npc => RunNpcChecks(npc, settings));
                    NpcWarnings = npcWarnings.Values.SelectMany(warnings => warnings).ToList().AsReadOnly();
                    UpdateResults();
                });
            var throttledModSettings = modSettings.RootDirectoryObservable.Throttle(TimeSpan.FromSeconds(1));
            Observable.CombineLatest(throttledBuildSettings, throttledModSettings, (settings, _) => settings)
                .ObserveOn(NewThreadScheduler.Default)
                .TakeUntil(disposed)
                .Subscribe(settings =>
                {
                    RunGlobalChecks(settings);
                    UpdateResults();
                });
            var modifiedNpcs = Observable
                .Merge(profile.Npcs.SelectMany(npc => new[]
                {
                    npc.DefaultOptionObservable.Select(_ => npc),
                    npc.FaceOptionObservable.Select(_ => npc),
                    npc.FaceGenOverrideObservable.Select(_ => npc),
                }));
            modifiedNpcs
                .WithLatestFrom(throttledBuildSettings, (npc, settings) => (npc, settings))
                .ObserveOn(NewThreadScheduler.Default)
                .TakeUntil(disposed)
                .Subscribe(res =>
                {
                    RunNpcChecks(res.npc, res.settings);
                    NpcWarnings = npcWarnings.Values.SelectMany(warnings => warnings).ToList().AsReadOnly();
                    UpdateResults();
                });
            throttledBuildSettings.Connect();
            appSettings.BuildWarningWhitelistObservable
                .TakeUntil(disposed)
                .Subscribe(_ => UpdateSummaryItems());
        }

        public void Dispose()
        {
            disposed.OnNext(true);
            GC.SuppressFinalize(this);
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

        private ErrorLevel GetErrorLevel()
        {
            var maxSeverity = GlobalWarnings.Concat(NpcWarnings).Max(x => x.Severity);
            return maxSeverity switch
            {
                BuildWarningSeverity.High => ErrorLevel.Fatal,
                BuildWarningSeverity.Medium => ErrorLevel.Warning,
                _ => ErrorLevel.None
            };
        }

        private static SummaryItemCategory PickCategory(int issueCount, SummaryItemCategory nonZeroCategory)
        {
            return issueCount > 0 ? nonZeroCategory : SummaryItemCategory.StatusOk;
        }

        private void RunGlobalChecks(BuildSettings settings)
        {
            GlobalWarnings = globalChecks.SelectMany(x => x.Run(profile, settings)).ToList().AsReadOnly();
        }

        private void RunNpcChecks(INpc npc, BuildSettings settings)
        {
            var warnings = npcChecks.SelectMany(c => c.Run(npc, settings)).ToList();
            if (warnings.Count > 0)
                npcWarnings[npc] = warnings;
            else
                npcWarnings.TryRemove(npc, out _);
        }

        private void UpdateResults()
        {
            UpdateSummaryItems();
            overallErrorLevel.OnNext(GetErrorLevel());
        }

        private void UpdateSummaryItems()
        {
            int criticalCount = 0, conflictCount = 0, otherCount = 0, suppressedCount = 0;
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
                if (warning.Severity == BuildWarningSeverity.High)
                {
                    criticalCount++;
                    continue;
                }
                switch (warning.Id)
                {
                    case BuildWarningId.MissingFaceGen:
                        conflictCount++;
                        break;
                    default:
                        otherCount++;
                        break;

                }
            }
            HeaderSummaryItems = new SummaryItem[]
            {
                new(PickCategory(criticalCount, SummaryItemCategory.StatusError), "Critical issues", criticalCount),
                new(PickCategory(conflictCount, SummaryItemCategory.StatusWarning), "NPC conflicts", conflictCount),
            };
            MainSummaryItems = new SummaryItem[]
            {
                new(PickCategory(otherCount, SummaryItemCategory.StatusInfo), "Other issues", otherCount),
                new(SummaryItemCategory.StatusInfo, "Suppressed warnings", suppressedCount),
            };
        }
    }
}
