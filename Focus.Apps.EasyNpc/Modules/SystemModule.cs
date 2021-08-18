using Autofac;
using Focus.Files;
using System.IO.Abstractions;

namespace Focus.Apps.EasyNpc.Modules
{
    public class SystemModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<FileSystem>().As<IFileSystem>();
            builder.RegisterType<FileSync>().As<IFileSync>().SingleInstance();
        }
    }
}
