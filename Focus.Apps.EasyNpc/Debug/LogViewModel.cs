using Focus.Apps.EasyNpc.Configuration;
using System.ComponentModel;

namespace Focus.Apps.EasyNpc.Debug
{
    public class LogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Text { get; private set; } = "";

        public void Append(string message)
        {
            Text += $"[{AssemblyProperties.Name}] {message}\n";
        }
    }
}
