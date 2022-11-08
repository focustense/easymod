using Autofac;
using Focus.Analysis.Execution;
using Focus.Apps.EasyNpc.Build;
using Focus.Apps.EasyNpc.Build.Checks;
using Focus.Apps.EasyNpc.Build.Pipeline;
using Focus.Apps.EasyNpc.Build.Preview;
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
            builder.RegisterType<CompressionEstimator>().As<ICompressionEstimator>();
            builder.RegisterType<BadArchives>().As<IGlobalBuildCheck>();
            builder.RegisterType<FaceGenConsistency>().As<INpcBuildCheck>();
            builder.RegisterType<MissingPlugins>().As<INpcBuildCheck>();
            builder.RegisterType<ModSettings>().As<IGlobalBuildCheck>();
            builder.RegisterType<OrphanedNpcs>().As<IGlobalBuildCheck>();
            builder.RegisterType<OverriddenArchives>().As<IGlobalBuildCheck>();
            builder.RegisterType<WigConversions>().As<INpcBuildCheck>();
            builder.RegisterType<BuildTaskViewModel>();
            builder.RegisterType<BuildProgressViewModel<BuildReport>>();
            builder.RegisterType<BuildViewModel>();

            // Intra-pipeline dependencies
            builder.RegisterType<BuildReporter>().As<IBuildReporter>().InstancePerLifetimeScope();
            builder.RegisterType<FileCopier>().As<IFileCopier>().InstancePerLifetimeScope();
            builder.RegisterType<RecordImporter>().InstancePerLifetimeScope();
            builder.RegisterType<VanillaTextureOverrideExclusion>().As<ITexturePathFilter>().InstancePerLifetimeScope();

            // Build tasks
            builder.RegisterType<ArchiveCreationTask>();
            builder.RegisterType<DewiggifyFaceGensTask>();
            builder.RegisterType<DewiggifyRecordsTask>();
            builder.RegisterType<FaceGenCopyTask>();
            builder.RegisterType<SharedResourceCopyTask>();
            builder.RegisterType<NpcDefaultsTask>();
            builder.RegisterType<NpcFacesTask>();
            builder.RegisterType<PatchInitializationTask>();
            builder.RegisterType<PatchSaveTask>();
            builder.RegisterType<ReportTask>();
            builder.RegisterType<TextureCopyTask>();
            builder.RegisterType<TexturePathExtractionTask>();
            builder.RegisterType<WriteMetadataTask>();

            // Realtime preview
            builder.RegisterType<PluginCategorizer>().As<IPluginCategorizer>();
            builder.RegisterType<AlertsViewModel>();
            builder.RegisterType<AssetsViewModel>();
            builder.RegisterType<BuildPreviewViewModel>();
            builder.RegisterType<NpcSummaryViewModel>();
            builder.RegisterType<OutputViewModel>();
            builder.RegisterType<PluginsViewModel>();

            builder.RegisterType<BuildPipelineConfiguration<BuildReport>>();
            builder
                .Register(ctx => ctx.Resolve<BuildPipelineConfiguration<BuildReport>>()
                    .RegisterTask<PatchInitializationTask.Factory>("Initialize Patch")
                    .RegisterTask<NpcDefaultsTask.Factory>("Import NPC Defaults")
                    .RegisterTask<NpcFacesTask.Factory>("Apply Face Customizations")
                    .RegisterTask<PatchSaveTask.Factory>("Save Patch")
                    .RegisterTask<SharedResourceCopyTask.Factory>("Copy Shared Resources")
                    .RegisterTask<FaceGenCopyTask.Factory>("Copy FaceGen Data")
                    .RegisterTask<DewiggifyRecordsTask.Factory>("De-wiggify Records")
                    .RegisterTask<DewiggifyFaceGensTask.Factory>("De-wiggify FaceGens")
                    .RegisterTask<TexturePathExtractionTask.Factory>("Extract Texture Paths")
                    .RegisterTask<TextureCopyTask.Factory>("Copy Textures")
                    .RegisterTask<ArchiveCreationTask.Factory>("Pack BSA Archive")
                    .RegisterTask<WriteMetadataTask.Factory>("Write Metadata")
                    .RegisterTask<ReportTask.Factory>("Report Results")
                    .CreatePipeline<BuildSettings>())
                .As<IBuildPipeline<BuildSettings, BuildReport>>();
        }
    }
}
