using Focus.Apps.EasyNpc.Build.Checks;
using Focus.Apps.EasyNpc.Build.Pipeline;
using Focus.Apps.EasyNpc.Build.UI;
using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Apps.EasyNpc.Messages;
using Focus.Apps.EasyNpc.Profiles;
using Focus.Files;
using Focus.ModManagers;
using PropertyChanged;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build
{
    public class BuildViewModel : INotifyPropertyChanged
    {
        public delegate BuildViewModel Factory(Profile profile);

        public event PropertyChangedEventHandler? PropertyChanged;

        public BuildReport? BuildReport { get; private set; }
        public bool EnableDewiggify { get; set; } = true;
        [DependsOn("Problems")]
        public bool HasProblems => PreBuildReport?.Warnings.Any() ?? false;
        public bool HasWigs { get; private set; }
        public bool IsBuildCompleted { get; private set; }
        [DependsOn("Progress")]
        public bool IsBuilding => Progress != null;
        [DependsOn("OutputModName")]
        public bool IsModOverwriteWarningVisible => ModDirectoryIsNotEmpty(OutputModName);
        public bool IsProblemCheckerEnabled => !IsProblemCheckingInProgress && !IsBuilding && !IsBuildCompleted;
        public bool IsProblemCheckerVisible { get; set; } = true;
        public bool IsProblemCheckingInProgress { get; set; }
        public bool IsProblemReportVisible { get; set; }
        public bool IsReadyToBuild { get; set; }
        [DependsOn("SelectedWarning")]
        public bool IsWarningInfoVisible => SelectedWarning != null;
        public string OutputDirectory { get; private set; } = string.Empty;
        public string OutputModName { get; set; } = $"NPC Merge {DateTime.Now:yyyy-MM-dd}";
        public string OutputPluginName => FileStructure.MergeFileName;
        public PreBuildReport? PreBuildReport { get; private set; }
        public BuildProgressViewModel<BuildReport>? Progress { get; private set; }
        public BuildWarning? SelectedWarning { get; set; }

        private readonly IBuildChecker checker;
        private readonly IMessageBus messageBus;
        private readonly IModRepository modRepository;
        private readonly IModSettings modSettings;
        private readonly IBuildPipeline<BuildSettings, BuildReport> pipeline;
        private readonly Profile profile;
        private readonly BuildProgressViewModel<BuildReport>.Factory progressFactory;

        public BuildViewModel(
            Profile profile, IBuildChecker checker, IBuildPipeline<BuildSettings, BuildReport> pipeline,
            IModSettings modSettings, IModRepository modRepository, IMessageBus messageBus,
            BuildProgressViewModel<BuildReport>.Factory progressFactory)
        {
            this.checker = checker;
            this.messageBus = messageBus;
            this.modRepository = modRepository;
            this.modSettings = modSettings;
            this.pipeline = pipeline;
            this.profile = profile;
            this.progressFactory = progressFactory;

            // We don't want to prevent users from switching back and forth between the build warnings and profile, and
            // using that to fix warnings in the profile. That's an intended flow. However, due to the way the UI is
            // currently designed, we DO want to persuade them to run a check again IF they dismissed the previous
            // checks (or there were no previous warnings) AND they've changed the profile since then.
            //
            // An improved pre-build UI could eliminate the need for this hack, i.e. we could show a warning that
            // "profile" has been changed without actually flipping back a few steps. But currently there's no logical
            // place to put this in the UI.
            messageBus.Subscribe<NpcConfigurationChanged>(_ =>
            {
                if (IsReadyToBuild)
                    Reset();
            });
        }

        public async void BeginBuild()
        {
            var watchableRepository = modRepository as IWatchable;
            if (watchableRepository is not null)
                watchableRepository.PauseWatching();
            try
            {
                OutputDirectory = Path.Combine(modSettings.RootDirectory, OutputModName);
                var buildSettings = GetBuildSettings();
                var progressModel = pipeline.Start(buildSettings);
                Progress = progressFactory(progressModel);
                BuildReport = await Progress.Outcome.ConfigureAwait(true);
                IsReadyToBuild = false;
                IsBuildCompleted = true;
            }
            finally
            {
                if (watchableRepository is not null)
                    watchableRepository.ResumeWatching();
            }
        }

        private BuildSettings GetBuildSettings()
        {
            return new BuildSettings
            {
                EnableDewiggify = EnableDewiggify,
                OutputModName = OutputModName,
                OutputDirectory = OutputDirectory,
                Profile = profile,
            };
        }

        // TODO: Add a check for missing textures - requires much deeper inspection of both plugins and meshes.
        public async void CheckForProblems()
        {
            Reset();
            IsProblemCheckerVisible = false;
            IsProblemCheckingInProgress = true;
            var buildSettings = GetBuildSettings();
            PreBuildReport = await Task.Run(() => checker.CheckAll(profile, buildSettings));
            IsProblemCheckingInProgress = false;
            IsProblemReportVisible = true;
        }

        public void DismissProblems()
        {
            IsProblemReportVisible = false;
            IsReadyToBuild = true;
        }

        public void ExpandMasterDependency(PreBuildReport.MasterDependency masterDependency)
        {
            messageBus.Send(new JumpToProfile(new JumpToProfile.FilterOverrides
            {
                DefaultPlugin = masterDependency.PluginName
            }));
        }

        public void ExpandWarning(BuildWarning warning)
        {
            messageBus.Send(new JumpToNpc(warning.RecordKey));
        }

        public void OpenBuildOutput()
        {
            if (!Directory.Exists(OutputDirectory)) // In case user moved/deleted after the build
                return;
            var psi = new ProcessStartInfo() { FileName = OutputDirectory, UseShellExecute = true };
            Process.Start(psi);
        }

        public void QuickRefresh()
        {
            HasWigs = profile.Npcs.Any(x => x.FaceOption.HasWig);
        }

        private bool ModDirectoryIsNotEmpty(string modName)
        {
            var modDirectory = Path.Combine(modSettings.RootDirectory, modName);
            return Directory.Exists(modDirectory) &&
                Directory.EnumerateFiles(modDirectory, "*", SearchOption.AllDirectories).Any();
        }

        private void Reset()
        {
            Progress = null;
            IsProblemCheckerVisible = true;
            IsProblemReportVisible = false;
            IsReadyToBuild = false;
            PreBuildReport = null;
        }
    }
}