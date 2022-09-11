using Autofac;
using Autofac.Core;
using Focus.Analysis.Execution;
using Focus.Analysis.Records;
using Focus.Apps.EasyNpc.Build.Pipeline;
using Focus.Apps.EasyNpc.Mutagen;
using Focus.Environment;
using Focus.Files;
using Focus.Providers.Mutagen;
using Focus.Providers.Mutagen.Analysis;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Linq;

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
            builder.RegisterType<StaticGameLocations>().As<IGameLocations>().SingleInstance();
            builder.RegisterDecorator<ModManagerGameLocationDecorator, IGameLocations>();
            builder.Register(ctx => GameInstance.FromGameId(ctx.Resolve<IGameLocations>(), GameId, DataDirectory))
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
                .As<IGameEnvironment<ISkyrimMod, ISkyrimModGetter>>()
                .OnActivated(NotifyAfterGameSetup)
                .SingleInstance();
            // Autofac doesn't know how to narrow open generics. Boo.
            builder.RegisterType<GameEnvironmentWrapper<ISkyrimMod, ISkyrimModGetter>>()
                .As<IMutableGameEnvironment<ISkyrimMod, ISkyrimModGetter>>()
                .As<IReadOnlyGameEnvironment<ISkyrimModGetter>>()
                .OnActivated(NotifyAfterGameSetup)
                .SingleInstance();
            builder.RegisterType<GameSettings<ISkyrimModGetter>>()
                .As<IGameSettings>()
                .OnActivated(NotifyAfterGameSetup)
                .SingleInstance();
            builder.RegisterType<DummyPluginBuilder>().As<IDummyPluginBuilder>();

            // Futures
            RegisterAfterGameSetup<GameEnvironmentState<ISkyrimMod, ISkyrimModGetter>>(builder);
            RegisterAfterGameSetup<IGameEnvironment<ISkyrimMod, ISkyrimModGetter>>(builder);
            RegisterAfterGameSetup<IMutableGameEnvironment<ISkyrimMod, ISkyrimModGetter>>(builder);
            RegisterAfterGameSetup<IReadOnlyGameEnvironment<ISkyrimModGetter>>(builder);
            RegisterAfterGameSetup<IGameSettings>(builder);

            // Analysis types
            builder.RegisterType<GroupCache>().As<IGroupCache>();
            builder.RegisterType<RecordScanner>().As<IRecordScanner>();
            builder.RegisterType<MutagenLoadOrderAnalyzer>().As<ILoadOrderAnalyzer>();
        }

        private static void NotifyAfterGameSetup<T>(IActivatedEventArgs<T> args)
        {
            var notifyTypes = typeof(T).GetInterfaces()
                .Prepend(typeof(T))
                .Distinct()
                .Select(t => typeof(AfterGameSetup<>).MakeGenericType(t));
            foreach (var notifyType in notifyTypes)
                if (args.Context.TryResolve(notifyType, out var notifier))
                    ((IAfterGameSetupNotifier)notifier).NotifyValue();
        }

        private static void RegisterAfterGameSetup<T>(ContainerBuilder builder)
        {
            builder.RegisterType<AfterGameSetup<T>>()
                .AsSelf() // Needed for notifications
                .As<IAfterGameSetup<T>>()
                .SingleInstance();
        }
    }
}
