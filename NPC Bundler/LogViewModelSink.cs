using Serilog.Core;
using Serilog.Events;
using System;

namespace NPC_Bundler
{
    public class LogViewModelSink : ILogEventSink
    {
        public LogViewModel ViewModel { get; set; }

        private readonly IFormatProvider formatProvider;

        public LogViewModelSink(IFormatProvider formatProvider = null)
        {
            this.formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            if (ViewModel == null)
                return;
            var message = logEvent.RenderMessage(formatProvider);
            ViewModel.Append(message);
        }
    }
}