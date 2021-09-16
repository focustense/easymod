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
                .RegisterModule(GetModManagerModule(options, startupInfo))
                .RegisterModule(new MutagenModule
                {
                    BlacklistedPluginNames = options.PostBuild ?
                        new HashSet<string>(StringComparer.OrdinalIgnoreCase) :
                        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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
                .RegisterModule<PostBuildModule>()
                .RegisterModule<MaintenanceModule>()
                .RegisterModule<MainModule>();
            return builder.Build();
        }

        private static Module GetModManagerModule(CommandLineOptions options, StartupInfo startupInfo)
        {
            // Vortex manifest is a command-line option, so it automatically overrides detection-based mechanisms.
            if (!string.IsNullOrEmpty(options.VortexManifest))
                return new VortexModule { BootstrapFilePath = options.VortexManifest };
            return startupInfo.Launcher switch
            {
                ModManager.ModOrganizer => new ModOrganizerModule
                {
                    ExecutablePath = !string.IsNullOrEmpty(options.ModOrganizerExecutablePath) ?
                                           options.ModOrganizerExecutablePath : startupInfo.ParentProcessPath,
                },
                _ => new UnknownModManagerModule(),
            };
        }
    }
}
