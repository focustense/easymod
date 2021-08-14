#nullable enable

using Serilog.Core;
using Serilog.Events;
using System;

namespace Focus.Apps.EasyNpc.Debug
{
    public class LogViewModelSink : ILogEventSink
    {
        public LogViewModel? ViewModel { get; set; }

        private readonly IFormatProvider? formatProvider;

        public LogViewModelSink(IFormatProvider? formatProvider = null)
        {
            this.formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            if (ViewModel is null)
                return;
            var message = logEvent.RenderMessage(formatProvider);
            ViewModel.Append(message);
        }
    }
}