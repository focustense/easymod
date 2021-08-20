using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;

namespace Focus.Apps.EasyNpc.Debug
{
    public class LogViewModelSink : ILogEventSink
    {
        public LogViewModel? ViewModel
        {
            get { return viewModel; }
            set
            {
                viewModel = value;
                if (value is not null)
                {
                    foreach (var logEvent in pendingEvents)
                        Emit(logEvent);
                    pendingEvents.Clear();
                }
            }
        }

        private readonly IFormatProvider? formatProvider;
        private readonly List<LogEvent> pendingEvents = new();

        private LogViewModel? viewModel;

        public LogViewModelSink(IFormatProvider? formatProvider = null)
        {
            this.formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            if (ViewModel is null)
            {
                pendingEvents.Add(logEvent);
                return;
            }
            var message = logEvent.RenderMessage(formatProvider);
            ViewModel.Append(message);
        }
    }
}