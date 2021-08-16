using Autofac;
using Focus.Apps.EasyNpc.Configuration;

namespace Focus.Apps.EasyNpc.Modules
{
    public class ConfigurationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(Settings.Default)
                .As<IAppSettings>()
                .As<IObservableAppSettings>()
                // Currently don't need OnActivated because we use Settings.Default.
                //.OnActivated(settings => settings.Instance.Load())
                .SingleInstance();
            builder.RegisterType<SettingsViewModel>();
        }
    }
}
