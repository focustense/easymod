using Autofac;
using Focus.ModManagers;

namespace Focus.Apps.EasyNpc.Modules
{
    public class UnknownModManagerModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(new ManualModManagerConfiguration(string.Empty))
                .As<IModManagerConfiguration>()
                .SingleInstance();
            builder.RegisterType<IndexedModRepository>().As<IIndexedModRepository>().SingleInstance();
            builder.RegisterType<ModPerComponentRepository>()
                .As<IConfigurableModRepository<ComponentPerDirectoryConfiguration>>()
                .As<IModRepository>()
                .SingleInstance();
        }
    }
}
