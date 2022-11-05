using PropertyChanged;
using System.Collections.Generic;
using System.ComponentModel;

namespace Focus.Apps.EasyNpc.Profiles
{
    public class NpcFiltersViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string? AvailablePlugin { get; set; }
        public IReadOnlyList<string> AvailablePlugins { get; set; } = new List<string>().AsReadOnly();
        public bool Conflicts { get; set; }
        public bool HasForcedFilter { get; set; }
        public bool Missing { get; set; }
        public bool MultipleChoices { get; set; }
        public bool NonDlc { get; set; }
        public string? SelectedDefaultPlugin { get; set; }
        public string? SelectedFacePlugin { get; set; }
        public NpcSexFilter Sex { get; set; }
        public bool Wigs { get; set; }

        [DependsOn(
            nameof(Conflicts), nameof(SelectedDefaultPlugin), nameof(SelectedFacePlugin),
            nameof(AvailablePlugin), nameof(AvailablePlugin), nameof(Missing),
            nameof(MultipleChoices), nameof(NonDlc), nameof(Sex), nameof(Wigs))]
        public bool IsNonDefault =>
            !NonDlc || Conflicts || Missing || Wigs ||
            Sex != NpcSexFilter.None ||
            !string.IsNullOrEmpty(SelectedDefaultPlugin) ||
            !string.IsNullOrEmpty(SelectedFacePlugin);

        public NpcFiltersViewModel()
        {
            ResetToDefault();
        }

        public void ResetToDefault()
        {
            Conflicts = false;
            AvailablePlugin = null;
            SelectedDefaultPlugin = null;
            SelectedFacePlugin = null;
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
