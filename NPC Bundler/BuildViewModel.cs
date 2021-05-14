using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPC_Bundler
{
    public class BuildViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsProblemCheckerEnabled => !IsProblemCheckingInProgress;
        public bool IsProblemCheckerVisible { get; set; } = true;
        public bool IsProblemCheckingInProgress { get; set; }
        public IReadOnlyList<NpcConfiguration> Npcs { get; init; }

        public BuildViewModel(IEnumerable<NpcConfiguration> npcs)
        {
            Npcs = npcs.ToList().AsReadOnly();
        }

        public async void CheckForProblems()
        {
            IsProblemCheckerVisible = false;
            IsProblemCheckingInProgress = true;
            await Task.Delay(3000);
            IsProblemCheckingInProgress = false;
        }
    }
}