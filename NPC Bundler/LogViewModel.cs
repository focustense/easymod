using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public void Pause()
        {
            isActive = false;
        }

        public void Resume()
        {
            if (isActive)
                return;
            isActive = true;
            Task.Run(MonitorMessages);
        }

        private async Task MonitorMessages()
        {
            while (isActive)
            {
                var newMessages = Messages.GetMessages();
                if (!string.IsNullOrEmpty(newMessages))
                    Text += newMessages;
                await Task.Delay(PollInterval);
            }
        }
    }
}
