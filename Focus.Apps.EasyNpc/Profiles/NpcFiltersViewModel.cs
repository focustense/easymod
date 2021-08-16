using PropertyChanged;
using System.Collections.Generic;
using System.ComponentModel;

namespace Focus.Apps.EasyNpc.Profiles
{
    public class NpcFiltersViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public IReadOnlyList<string> AvailablePlugins { get; set; } = new List<string>().AsReadOnly();
        public bool Conflicts { get; set; }
        public string? DefaultPlugin { get; set; }
        public bool FaceChanges { get; set; }
        public string? FacePlugin { get; set; }
        public bool HasForcedFilter { get; set; }
        public bool Missing { get; set; }
        public bool MultipleChoices { get; set; }
        public bool NonDlc { get; set; }
        public NpcSexFilter Sex { get; set; }
        public bool Wigs { get; set; }

        [DependsOn(
            "Conflicts", "DefaultPlugin", "FaceChange", "FacePlugin", "Missing", "MultipleChoices", "NonDlc",
            "Sex", "Wigs")]
        public bool IsNonDefault =>
            !NonDlc || Conflicts || Missing || Wigs ||
            Sex != NpcSexFilter.None ||
            !string.IsNullOrEmpty(DefaultPlugin) ||
            !string.IsNullOrEmpty(FacePlugin);

        public NpcFiltersViewModel()
        {
            ResetToDefault();
        }

        public void ResetToDefault()
        {
            Conflicts = false;
            DefaultPlugin = null;
            FaceChanges = false;
            FacePlugin = null;
            Missing = false;
            MultipleChoices = false;
            NonDlc = true;
            Wigs = false;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (propertyName != nameof(HasForcedFilter))
                HasForcedFilter = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum NpcSexFilter
    {
        None = 0,
        Male,
        Female,
    }
}
