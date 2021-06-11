using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Debug
{
    public class LogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int PollInterval { get; set; } = 100;
        public string Text { get; private set; } = "";

        private readonly IExternalLog externalLog;

        private bool isExternalMonitoringActive = false;

        public LogViewModel(IExternalLog externalLog)
        {
            this.externalLog = externalLog;
        }

        public void Append(string message)
        {
            // In case we're not actively monitoring, this preserves chronological order between xEdit messages and
            // app-originated messages.
            if (!isExternalMonitoringActive)
                RefreshExternalMessages();
            Text += $"[NpcBundler] {message}\n";
        }

        public void PauseExternalMonitoring()
        {
            isExternalMonitoringActive = false;
        }

        public void ResumeExternalMonitoring()
        {
            if (isExternalMonitoringActive)
                return;
            isExternalMonitoringActive = true;
            Task.Run(MonitorExternalMessages);
        }

        private async Task MonitorExternalMessages()
        {
            while (isExternalMonitoringActive)
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
