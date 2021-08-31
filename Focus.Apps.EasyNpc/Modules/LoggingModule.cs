using Autofac;
using AutofacSerilogIntegration;
using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Debug;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;

namespace Focus.Apps.EasyNpc.Modules
{
    public class LoggingModule : Module
    {
        public LogEventLevel Level { get; set; } = LogEventLevel.Information;
        public string LogFileName { get; set; } = "";

        protected override void Load(ContainerBuilder builder)
        {
            if (string.IsNullOrEmpty(LogFileName))
                throw new InvalidOperationException($"{nameof(LogFileName)} must be configured.");

            var loggingLevelSwitch = new LoggingLevelSwitch(Level);
            var logViewModelSink = new LogViewModelSink();
            var log = Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(loggingLevelSwitch)
                    .WriteTo.File(LogFileName,
                        buffered: true,
                        flushToDiskInterval: TimeSpan.FromMilliseconds(500),
                        outputTemplate:
                            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{ThreadId:D2}] [{Level:u3}] " +
                            "{Message:lj}{NewLine}{Exception}")
                    .WriteTo.Sink(logViewModelSink)
                    .Enrich.WithThreadId()
                    .CreateLogger();
            log.Information(
                "Initialized: {appName:l} version {version:l}, built on {buildDate}",
                AssemblyProperties.Name, AssemblyProperties.Version, AssemblyProperties.BuildTimestampUtc);
            if (Level <= LogEventLevel.Debug)
                log.Debug("Debug mode enabled");

            builder.RegisterInstance(logViewModelSink).AsSelf();
            builder.RegisterType<LogViewModel>()
                .OnActivated(e => e.Context.Resolve<LogViewModelSink>().ViewModel = e.Instance)
                .SingleInstance();
            builder.RegisterLogger();
        }
    }
}
