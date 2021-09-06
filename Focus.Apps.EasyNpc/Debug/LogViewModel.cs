using Focus.Apps.EasyNpc.Configuration;
using PropertyChanged;

namespace Focus.Apps.EasyNpc.Debug
{
    [AddINotifyPropertyChangedInterface]
    public class LogViewModel
    {
        public string Text { get; private set; } = "";

        public void Append(string message)
        {
            Text += $"[{AssemblyProperties.Name}] {message}\n";
        }
    }
}
