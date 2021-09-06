using PropertyChanged;
using System;
using System.ComponentModel;

namespace Focus.Apps.EasyNpc
{
    [AddINotifyPropertyChangedInterface]
    public class DataGridColumn
    {
        public string FilterText { get; set; } = string.Empty;
        public string HeaderText { get; private init; }

        public DataGridColumn(string headerText)
        {
            HeaderText = headerText;
        }
    }
}