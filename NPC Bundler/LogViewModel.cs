using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using XeLib.API;

namespace NPC_Bundler
{
    public class LogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int PollInterval { get; set; } = 100;
        public string Text { get; private set; } = "";

        private bool isActive = false;

        public LogViewModel()
        {
        }

        public void Append(string message)
        {
            // In case we're not actively monitoring, this preserves chronological order between xEdit messages and
            // app-originated messages.
            if (!isActive)
                RefreshMessages();
            Text += $"[NpcBundler] {message}\n";
        }

        public void Pause()
        {
            isActive = false;
        }

        public void Resume()
        {
            if (isActive)
                return;
            isActive = true;
            Task.Run(MonitorXeLibMessages);
        }

        private void RefreshMessages()
        {
            var newMessages = Messages.GetMessages();
            if (!string.IsNullOrEmpty(newMessages))
                Text += string.Join('\n', newMessages
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(msg => $"[XeLib] {msg}")) + '\n';
        }

        private async Task MonitorXeLibMessages()
        {
            while (isActive)
            {
                RefreshMessages();
                await Task.Delay(PollInterval);
            }
        }
    }
}
