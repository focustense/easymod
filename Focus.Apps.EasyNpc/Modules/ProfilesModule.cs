using Autofac;
using Focus.Analysis.Execution;
using Focus.Apps.EasyNpc.Profiles;
using System;

namespace Focus.Apps.EasyNpc.Modules
{
    public class ProfilesModule : Module
    {
        public string AutosavePath { get; set; } = string.Empty;

        protected override void Load(ContainerBuilder builder)
        {
            if (string.IsNullOrEmpty(AutosavePath))
                throw new InvalidOperationException($"{nameof(AutosavePath)} must be configured.");

            builder.RegisterType<ProfileEventLog>()
                .WithParameter(new NamedParameter("fileName", AutosavePath))
                .As<ISuspendableProfileEventLog>()
                .As<IProfileEventLog>()
                .As<IReadOnlyProfileEventLog>()
                .SingleInstance();
            builder.RegisterType<ProfilePolicy>()
                .As<IProfilePolicy>()
                .As<ILoadOrderAnalysisReceiver>()
                .SingleInstance();
            builder.RegisterType<ProfileFactory>().As<IProfileFactory>().SingleInstance();
            builder.RegisterType<FileSystemMugshotRepository>().As<IMugshotRepository>()
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                // Why does Autofac like to pass an empty array for IEnumerable<T>?
                .WithParameter(new NamedParameter("extensions", null))
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                .SingleInstance();
            builder.RegisterType<LineupBuilder>().As<ILineupBuilder>();
            builder.RegisterType<ProfileViewModel>();
        }
    }
}
