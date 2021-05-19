using PropertyChanged;
using System.ComponentModel;

namespace NPC_Bundler
{
    public class ProgressViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int CurrentProgress { get; set; }
        [DependsOn("CurrentProgress", "MaxProgress")]
        public bool IsCompleted => CurrentProgress == MaxProgress;
        [DependsOn("MaxProgress")]
        public bool IsIndeterminate => MaxProgress == 0;
        public string ItemName { get; set; }
        public int MaxProgress { get; set; }
        [DependsOn("CurrentProgress", "MaxProgress")]
        public float ProgressPercent => MaxProgress != 0 ? (float)CurrentProgress / MaxProgress : 0;
        public string StageName { get; set; }
        public string TaskName { get; init; }

        public ProgressViewModel(string taskName)
        {
            TaskName = taskName;
        }

        public void StartStage(string stageName, string itemName = "")
        {
            StageName = stageName;
            ItemName = itemName;
        }
    }
}