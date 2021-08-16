using Autofac;
using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Apps.EasyNpc.Modules;
using Serilog.Events;
using System;
using System.Collections.Generic;

namespace Focus.Apps.EasyNpc.Main
{
    static class AppContainer
    {
        public static IContainer Build(CommandLineOptions options, StartupInfo startupInfo)
        {
            var builder = new ContainerBuilder();
            builder
                .RegisterModule(new LoggingModule
                {
                    Level = options.DebugMode ? LogEventLevel.Debug : LogEventLevel.Information,
                    LogFileName = ProgramData.LogFileName,
                })
                .RegisterModule<SystemModule>()
                .RegisterModule<ConfigurationModule>()
                .RegisterModule<MessagingModule>()
                // TODO: Reinstate Vortex ASAP
                .RegisterModule(new ModOrganizerModule
                {
                    ExecutablePath = !string.IsNullOrEmpty(options.ModOrganizerExecutablePath) ?
                        options.ModOrganizerExecutablePath : startupInfo.ParentProcessPath,
                })
                .RegisterModule(new MutagenModule
                {
                    BlacklistedPluginNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        FileStructure.MergeFileName,
                    },
                    DataDirectory = options.GamePath,
                    GameId = options.GameName,
                })
                .RegisterModule(new ProfilesModule
                {
                    AutosavePath = ProgramData.ProfileLogFileName,
                })
                .RegisterModule<BuildModule>()
                .RegisterModule<MaintenanceModule>()
                .RegisterModule<MainModule>();
            return builder.Build();
        }
    }
}
