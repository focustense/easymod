using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Providers.Mutagen;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public class PatchInitializationTask : BuildTask<PatchInitializationTask.Result>
    {
        public class Result
        {
            public RecordImporter Importer { get; private init; }
            public SkyrimMod Mod { get; private init; }

            public Result(SkyrimMod mod, RecordImporter importer)
            {
                Mod = mod;
                Importer = importer;
            }
        }

        public delegate PatchInitializationTask Factory();

        private readonly GameSelection game;
        private readonly RecordImporter.Factory importerFactory;
        private readonly IBuildReporter reporter;

        public PatchInitializationTask(
            GameSelection game, IBuildReporter reporter, RecordImporter.Factory importerFactory)
        {
            this.game = game;
            this.importerFactory = importerFactory;
            this.reporter = reporter;
        }

        protected override Task<Result> Run(BuildSettings settings)
        {
            // Kind of lazy to purge the old report here, but the alternative is spamming another build task just for
            // this trivial thing, or putting it outside the build. Seems like the least bad option right now.
            reporter.Delete();
            var mod = new SkyrimMod(
                ModKey.FromNameAndExtension(FileStructure.MergeFileName), game.GameRelease.ToSkyrimRelease());
            var importer = importerFactory(mod);
            return Task.FromResult(new Result(mod, importer));
        }
    }
}
