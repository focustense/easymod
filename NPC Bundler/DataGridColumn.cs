using System;
using System.ComponentModel;

namespace NPC_Bundler
{
    public class DataGridColumn : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string FilterText { get; set; }
        public string HeaderText { get; private init; }

        public DataGridColumn(string headerText)
        {
            HeaderText = headerText;
        }
    }
}