using Autofac;
using Focus.Analysis.Execution;
using Focus.Apps.EasyNpc.Build;
using Focus.Apps.EasyNpc.Build.Checks;
using Focus.Apps.EasyNpc.Build.Pipeline;
using Focus.Apps.EasyNpc.Build.UI;
using Focus.Apps.EasyNpc.Nifly;
using Focus.Files;

namespace Focus.Apps.EasyNpc.Modules
{
    public class BuildModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<GameFileProvider>().As<IFileProvider>().SingleInstance();
            builder.RegisterType<NiflyFaceGenEditor>().As<IFaceGenEditor>().SingleInstance();
            builder.RegisterType<SimpleWigResolver>()
                .As<IWigResolver>()
                .As<ILoadOrderAnalysisReceiver>()
                .SingleInstance();
            builder.RegisterType<BadArchives>().As<IBuildCheck>();
            builder.RegisterType<FaceGenConsistency>().As<IBuildCheck>();
            builder.RegisterType<MissingPlugins>().As<IBuildCheck>();
            builder.RegisterType<ModSettings>().As<IBuildCheck>();
            builder.RegisterType<OrphanedNpcs>().As<IBuildCheck>();
            builder.RegisterType<OverriddenArchives>().As<IBuildCheck>();
            builder.RegisterType<WigConversions>().As<IBuildCheck>();
            builder.RegisterType<BuildChecker>().As<IBuildChecker>().SingleInstance();
            builder.RegisterType<BuildTaskViewModel>();
            builder.RegisterType<BuildProgressViewModel<BuildReport>>();
            builder.RegisterType<BuildViewModel>();

            builder.RegisterType<FileCopier>().As<IFileCopier>();
            builder.RegisterType<RecordImporter>();

            builder.RegisterType<ArchiveCreationTask>();
            builder.RegisterType<DewiggifyRecordsTask>();
            builder.RegisterType<FaceGenCopyTask>();
            builder.RegisterType<HeadPartResourceCopyTask>();
            builder.RegisterType<NpcDefaultsTask>();
            builder.RegisterType<NpcFacesTask>();
            builder.RegisterType<PatchInitializationTask>();
            builder.RegisterType<PatchSaveTask>();
            builder.RegisterType<ReportTask>();
            builder.RegisterType<TextureCopyTask>();
            builder.RegisterType<TexturePathExtractionTask>();

            builder.RegisterType<BuildPipelineConfiguration<BuildReport>>();
            builder
                .Register(ctx => ctx.Resolve<BuildPipelineConfiguration<BuildReport>>()
                    .RegisterTask<PatchInitializationTask.Factory>()
                    .RegisterTask<NpcDefaultsTask.Factory>()
                    .RegisterTask<NpcFacesTask.Factory>()
                    .RegisterTask<PatchSaveTask.Factory>()
                    .RegisterTask<HeadPartResourceCopyTask.Factory>()
                    .RegisterTask<FaceGenCopyTask.Factory>()
                    .RegisterTask<DewiggifyRecordsTask.Factory>()
                    .RegisterTask<TexturePathExtractionTask.Factory>()
                    .RegisterTask<TextureCopyTask.Factory>()
                    .RegisterTask<ArchiveCreationTask.Factory>()
                    .RegisterTask<ReportTask.Factory>()
                    .CreatePipeline<BuildSettings>())
                .As <IBuildPipeline<BuildSettings, BuildReport>>();
        }
    }
}
