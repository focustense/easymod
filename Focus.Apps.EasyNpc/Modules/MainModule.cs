using Autofac;
using Focus.Apps.EasyNpc.Main;
using Focus.Apps.EasyNpc.Reports;

namespace Focus.Apps.EasyNpc.Modules
{
    public class MainModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<LoaderModel>();
            builder.RegisterType<LoaderViewModel>();
            builder.RegisterType<StartupReportViewModel>();
            builder.RegisterType<MainViewModel>();
        }
    }
}
