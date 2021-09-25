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

        public NpcSummaryViewModel Npcs { get; private init; }
        public PluginsViewModel Plugins { get; private init; }

        private readonly IMessageBus messageBus;

        public BuildPreviewViewModel(
            IMessageBus messageBus, NpcSummaryViewModel.Factory npcsFactory, PluginsViewModel.Factory pluginsFactory,
            Profile profile, LoadOrderAnalysis analysis)
        {
            this.messageBus = messageBus;
            Npcs = npcsFactory(profile, analysis);
            Plugins = pluginsFactory(profile);
        }

        public void ShowProfile(IRecordKey? key)
        {
            if (key is not null)
                messageBus.Send(new JumpToNpc(key));
        }
    }
}
