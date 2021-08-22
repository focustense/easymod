using Autofac;
using Focus.Apps.EasyNpc.Main;

namespace Focus.Apps.EasyNpc.Modules
{
    public class MainModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<LoaderModel>();
            builder.RegisterType<LoaderViewModel>();
            builder.RegisterType<MainViewModel>();
        }
    }
}
