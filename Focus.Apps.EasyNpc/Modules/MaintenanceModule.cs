using Autofac;
using Focus.Apps.EasyNpc.Maintenance;

namespace Focus.Apps.EasyNpc.Modules
{
    public class MaintenanceModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MaintenanceViewModel>();
        }
    }
}
