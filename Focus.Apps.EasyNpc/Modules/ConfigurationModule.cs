using Autofac;
using Focus.Apps.EasyNpc.Configuration;

namespace Focus.Apps.EasyNpc.Modules
{
    public class ConfigurationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Settings>()
                .WithParameter("path", ProgramData.SettingsPath)
                .As<IAppSettings>()
                .As<IMutableAppSettings>()
                .As<IObservableAppSettings>()
                // Currently don't need OnActivated because we use Settings.Default.
                //.OnActivated(settings => settings.Instance.Load())
                .SingleInstance();
            builder.RegisterType<ModSettings>()
                .As<IModSettings>()
                .SingleInstance();
            builder.RegisterType<SettingsViewModel>();
        }
    }
}
