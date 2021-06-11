using System;
using System.ComponentModel;

namespace Focus.Apps.EasyNpc
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