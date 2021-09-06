using Autofac;
using Focus.ModManagers;
using Focus.ModManagers.ModOrganizer;
using System;

namespace Focus.Apps.EasyNpc.Modules
{
    public class ModOrganizerModule : Module
    {
        public string? ExecutablePath { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            if (string.IsNullOrEmpty(ExecutablePath))
                throw new InvalidOperationException($"{nameof(ExecutablePath)} must be configured.");

            builder.Register(_ => IniConfiguration.AutoDetect(ExecutablePath))
                .As<IModOrganizerConfiguration>()
                .As<IModManagerConfiguration>()
                .SingleInstance();
            builder.RegisterType<IndexedModRepository>().As<IIndexedModRepository>().SingleInstance();
            builder.RegisterType<ModOrganizerModRepository>()
                .As<IConfigurableModRepository<ComponentPerDirectoryConfiguration>>()
                .As<IModRepository>()
                .SingleInstance();
        }
    }
}
