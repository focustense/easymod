using Focus.Apps.EasyNpc.Build.Pipeline;
using Focus.Apps.EasyNpc.Build.Preview;
using Focus.Apps.EasyNpc.Build.UI;
using Focus.Files;
using Focus.ModManagers;
using PropertyChanged;
using System.Diagnostics;
using System.IO;

namespace Focus.Apps.EasyNpc.Build
{
    [AddINotifyPropertyChangedInterface]
    public class BuildViewModel
    {
        public delegate BuildViewModel Factory(BuildPreviewViewModel preview);

        public BuildReport? BuildReport { get; private set; }
        public BuildSettings? BuildSettings { get; private set; }
        public bool IsBuildCompleted { get; private set; }
        [DependsOn(nameof(IsBuildCompleted), nameof(Progress))]
        public bool IsBuildInProgress => Progress is not null && !IsBuildCompleted;
        [DependsOn(nameof(Progress))]
        public bool IsBuildStarted => Progress is not null;
        public BuildPreviewViewModel Preview { get; private init; }
        public BuildProgressViewModel<BuildReport>? Progress { get; private set; }

        private readonly IModRepository modRepository;
        private readonly IBuildPipeline<BuildSettings, BuildReport> pipeline;
        private readonly BuildProgressViewModel<BuildReport>.Factory progressFactory;

        public BuildViewModel(
            IModRepository modRepository, IBuildPipeline<BuildSettings, BuildReport> pipeline,
            BuildPreviewViewModel preview, BuildProgressViewModel<BuildReport>.Factory progressFactory)
        {
            this.modRepository = modRepository;
            this.pipeline = pipeline;
            this.progressFactory = progressFactory;
            Preview = preview;
        }

        public async void BeginBuild()
        {
            if (Preview.CurrentSettings is null)
                return;
            var watchableRepository = modRepository as IWatchable;
            if (watchableRepository is not null)
                watchableRepository.PauseWatching();
            try
            {
                BuildSettings = Preview.CurrentSettings;
                var progressModel = pipeline.Start(BuildSettings);
                Progress = progressFactory(progressModel);
                BuildReport = await Progress.Outcome.ConfigureAwait(true);
                IsBuildCompleted = true;
            }
            finally
            {
                if (watchableRepository is not null)
                    watchableRepository.ResumeWatching();
            }
        }

        public void OpenBuildOutput()
        {
            if (BuildSettings is null)
                return;
            if (!Directory.Exists(BuildSettings.OutputDirectory)) // In case user moved/deleted after the build
                return;
            var psi = new ProcessStartInfo() { FileName = BuildSettings.OutputDirectory, UseShellExecute = true };
            Process.Start(psi);
        }
    }
}