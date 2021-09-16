using Autofac;
using Focus.Apps.EasyNpc.Reports;

namespace Focus.Apps.EasyNpc.Modules
{
    public class PostBuildModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<PostBuildReportGenerator>();
            builder.RegisterType<PostBuildReportViewModel>();
        }
    }
}
