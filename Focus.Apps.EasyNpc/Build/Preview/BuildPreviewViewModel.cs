using Focus.Analysis.Execution;
using Focus.Apps.EasyNpc.Messages;
using Focus.Apps.EasyNpc.Profiles;
using PropertyChanged;

namespace Focus.Apps.EasyNpc.Build.Preview
{
    [AddINotifyPropertyChangedInterface]
    public class BuildPreviewViewModel
    {
        public delegate BuildPreviewViewModel Factory(Profile profile, LoadOrderAnalysis analysis);

        public AlertsViewModel Alerts { get; private init; }
        public bool IsAlertsExpanded { get; set; }
        public bool IsNpcsExpanded { get; set; } 
        public bool IsOutputExpanded { get; set; }
        public bool IsPluginsExpanded { get; set; }
        public NpcSummaryViewModel Npcs { get; private init; }
        public OutputViewModel Output { get; private init; }
        public PluginsViewModel Plugins { get; private init; }
        [DoNotNotify]
        public double ScrollPosition { get; set; }

        private readonly IMessageBus messageBus;

        public BuildPreviewViewModel(
            IMessageBus messageBus, NpcSummaryViewModel.Factory npcsFactory, PluginsViewModel.Factory pluginsFactory,
            AlertsViewModel.Factory alertsFactory, OutputViewModel.Factory outputFactory, Profile profile,
            LoadOrderAnalysis analysis)
        {
            this.messageBus = messageBus;
            Npcs = npcsFactory(profile, analysis);
            Plugins = pluginsFactory(profile);
            Output = outputFactory(profile);
            Alerts = alertsFactory(profile, Output.BuildSettings);
            Alerts.BeginWatching();
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
