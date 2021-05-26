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

        private readonly IExternalLog externalLog;

        private bool isActive = false;

        public LogViewModel(IExternalLog externalLog)
        {
            this.externalLog = externalLog;
        }

        public void Append(string message)
        {
            // In case we're not actively monitoring, this preserves chronological order between xEdit messages and
            // app-originated messages.
            if (!isActive)
                RefreshExternalMessages();
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
            Task.Run(MonitorExternalMessages);
        }

        private async Task MonitorExternalMessages()
        {
            while (isActive)
            {
                RefreshExternalMessages();
                await Task.Delay(PollInterval);
            }
        }

        private void RefreshExternalMessages()
        {
            var externalText = string.Join(Environment.NewLine, externalLog.GetMessages());
            if (!string.IsNullOrEmpty(externalText))
                Text += externalText + Environment.NewLine;
        }
    }
}
