using PropertyChanged;
using Serilog;
using System;
using System.ComponentModel;

namespace NPC_Bundler
{
    public class ProgressViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int CurrentProgress { get; set; }
        public string ErrorMessage { get; set; }
        [DependsOn("ErrorMessage")]
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        [DependsOn("CurrentProgress", "MaxProgress")]
        public bool IsCompleted => MaxProgress > 0 && CurrentProgress == MaxProgress;
        public bool IsPaused { get; set; }
        [DependsOn("MaxProgress")]
        public bool IsIndeterminate => MaxProgress == 0;
        public string ItemName { get; set; }
        public int MaxProgress { get; set; }
        [DependsOn("CurrentProgress", "MaxProgress")]
        public float ProgressPercent => MaxProgress != 0 ? (float)CurrentProgress / MaxProgress : 0;
        public string StageName { get; set; }
        public string TaskName { get; private init; }

        private readonly ILogger log;

        public ProgressViewModel(
            string taskName, ILogger log = null, bool startPaused = false, string defaultStageName = "Not started")
        {
            TaskName = taskName;
            StageName = defaultStageName;
            IsPaused = startPaused;
            this.log = log;

            if (!string.IsNullOrEmpty(defaultStageName))
                LogCurrentStage();
        }

        public void AdjustRemaining(int remainingProgress, float percentOfTotal)
        {
            // Example: Remaining units for stage = 300, current progress = 60%, stage counts for 20% of total;
            // New progress will be 900/1500 - i.e. 60% now, 80% after we add 300.
            // Previous current/max are irrelevant and we can simply forget about them.
            var previousProgressPercent = ProgressPercent;
            MaxProgress = (int)Math.Ceiling(remainingProgress / percentOfTotal);
            CurrentProgress = (int)(MaxProgress * previousProgressPercent);
        }

        public void JumpTo(float percent)
        {
            CurrentProgress = (int)Math.Round(MaxProgress * percent);
        }

        public void StartStage(string stageName, string itemName = "")
        {
            // Starting a stage generally means things are happening, i.e. no longer paused
            IsPaused = false;
            StageName = stageName;
            ItemName = itemName;
            LogCurrentStage();
        }

        private void LogCurrentStage()
        {
            log?.Information("[{TaskName}] - {StageName}", TaskName, StageName);
        }
    }
}