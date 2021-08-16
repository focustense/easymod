using Autofac;
using Focus.ModManagers;
using Focus.ModManagers.Vortex;
using System;

namespace Focus.Apps.EasyNpc.Modules
{
    public class VortexModule : Module
    {
        public string BootstrapFilePath { get; set; } = string.Empty;

        protected override void Load(ContainerBuilder builder)
        {
            if (string.IsNullOrEmpty(BootstrapFilePath))
                throw new InvalidOperationException($"{nameof(BootstrapFilePath)} must be configured.");

            builder.Register(ctx => ModManifest.LoadFromFile(BootstrapFilePath))
                .AsSelf()
                .As<IModManagerConfiguration>()
                .SingleInstance();
            builder.RegisterType<IndexedModRepository>().As<IIndexedModRepository>().SingleInstance();
            builder.RegisterType<VortexModRepository>()
                .As<IConfigurableModRepository<ComponentPerDirectoryConfiguration>>()
                .As<IModRepository>()
                .SingleInstance();
        }
    }
}
