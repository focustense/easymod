using Focus.Analysis.Execution;
using Focus.Apps.EasyNpc.Messages;
using Focus.Apps.EasyNpc.Profiles;
using PropertyChanged;
using Serilog;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Focus.Apps.EasyNpc.Build.Preview
{
    [AddINotifyPropertyChangedInterface]
    public class BuildPreviewViewModel : IDisposable
    {
        public delegate BuildPreviewViewModel Factory(Profile profile, LoadOrderAnalysis analysis);

        public AlertsViewModel Alerts { get; private init; }
        public AssetsViewModel Assets { get; private init; }
        public BuildSettings? CurrentSettings { get; private set; }
        [DependsOn(nameof(ErrorLevel))]
        public bool HasErrorsOrWarnings => OverallErrorLevel >= ErrorLevel.Warning;
        public bool IsAlertsExpanded { get; set; }
        public bool IsAssetsExpanded { get; set; }
        public bool IsNpcsExpanded { get; set; } 
        public bool IsOutputExpanded { get; set; }
        public bool IsPluginsExpanded { get; set; }
        public NpcSummaryViewModel Npcs { get; private init; }
        public OutputViewModel Output { get; private init; }
        public ErrorLevel OverallErrorLevel { get; private set; }
        public PluginsViewModel Plugins { get; private init; }
        [DoNotNotify]
        public double ScrollPosition { get; set; }

        private readonly Subject<bool> disposed = new();
        private readonly IMessageBus messageBus;

        public BuildPreviewViewModel(
            IMessageBus messageBus, NpcSummaryViewModel.Factory npcsFactory, PluginsViewModel.Factory pluginsFactory,
            AlertsViewModel.Factory alertsFactory, OutputViewModel.Factory outputFactory,
            AssetsViewModel.Factory assetsFactory, ILogger log, Profile profile, LoadOrderAnalysis analysis)
        {
            this.messageBus = messageBus;
            Npcs = npcsFactory(profile, analysis);
            Plugins = pluginsFactory(profile);
            Assets = assetsFactory(profile, analysis);
            Output = outputFactory(profile, analysis);
            Alerts = alertsFactory(profile, Output.BuildSettings);
            Alerts.BeginWatching();

            Observable.CombineLatest(Assets.OverallErrorLevel, Output.OverallErrorLevel, Alerts.OverallErrorLevel)
                .Select(errorLevels => errorLevels.Max())
                .TakeUntil(disposed)
                .SubscribeSafe(log, lvl => OverallErrorLevel = lvl);
            Output.BuildSettings
                .TakeUntil(disposed)
                .SubscribeSafe(log, settings => CurrentSettings = settings);
        }

        public void Dispose()
        {
            disposed.OnNext(true);
            GC.SuppressFinalize(this);
        }

        public void ExpandWarning(BuildWarning warning)
        {
            if (warning.RecordKey is not null)
                messageBus.Send(new JumpToNpc(warning.RecordKey));
        }

        public void ShowMaster(string pluginName)
        {
            messageBus.Send(new JumpToProfile(new JumpToProfile.FilterOverrides { DefaultPlugin = pluginName }));
        }

        public void ShowProfile(IRecordKey? key)
        {
            if (key is not null)
                messageBus.Send(new JumpToNpc(key));
        }
    }
}
