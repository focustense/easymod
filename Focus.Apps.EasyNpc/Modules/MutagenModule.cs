using Autofac;
using Focus.Analysis.Execution;
using Focus.Analysis.Records;
using Focus.Apps.EasyNpc.Build.Pipeline;
using Focus.Apps.EasyNpc.Mutagen;
using Focus.Environment;
using Focus.Files;
using Focus.Providers.Mutagen;
using Focus.Providers.Mutagen.Analysis;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;

namespace Focus.Apps.EasyNpc.Modules
{
    public class MutagenModule : Module
    {
        public IReadOnlySet<string> BlacklistedPluginNames { get; set; } = new HashSet<string>();
        public string? DataDirectory { get; set; }
        public string GameId { get; set; } = string.Empty;

        protected override void Load(ContainerBuilder builder)
        {
            if (string.IsNullOrEmpty(GameId))
                throw new InvalidOperationException($"{nameof(GameId)} must be configured.");
            builder.Register(_ => GameInstance.FromGameId(GameId))
                .AsSelf()
                .As<GameSelection>();
            builder.RegisterType<ArchiveStatics>().As<IArchiveStatics>().SingleInstance();
            builder.RegisterType<SetupStatics>().As<ISetupStatics>().SingleInstance();
            builder.RegisterType<MutagenArchiveProvider>().As<IArchiveProvider>().SingleInstance();
            builder.RegisterDecorator<CachingArchiveProvider, IArchiveProvider>();
            builder.RegisterType<GameSetup>()
                .As<IGameSetup>()
                .OnActivating(x => x.Instance.Detect(BlacklistedPluginNames))
                .SingleInstance();
            builder.Register(ctx => ctx.Resolve<IGameSetup>().LoadOrderGraph)
                .As<IReadOnlyLoadOrderGraph>();
            builder.RegisterType<EnvironmentFactory>().As<IEnvironmentFactory>();
            builder.Register(ctx => ctx.Resolve<IEnvironmentFactory>().CreateEnvironment())
                .As<GameEnvironmentState<ISkyrimMod, ISkyrimModGetter>>()
                .SingleInstance();
            // Autofac doesn't know how to narrow open generics. Boo.
            builder.RegisterType<GameEnvironmentWrapper<ISkyrimMod, ISkyrimModGetter>>()
                .As<IMutableGameEnvironment<ISkyrimMod, ISkyrimModGetter>>()
                .As<IReadOnlyGameEnvironment<ISkyrimModGetter>>()
                .SingleInstance();
            builder.RegisterType<GameSettings<ISkyrimModGetter>>().As<IGameSettings>().SingleInstance();
            builder.RegisterType<DummyPluginBuilder>().As<IDummyPluginBuilder>();

            // Analysis types
            builder.RegisterType<GroupCache>().As<IGroupCache>();
            builder.RegisterType<RecordScanner>().As<IRecordScanner>();
            builder.RegisterType<MutagenLoadOrderAnalyzer>().As<ILoadOrderAnalyzer>();
        }
    }
}
