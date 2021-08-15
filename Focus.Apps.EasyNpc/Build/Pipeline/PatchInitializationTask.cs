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

        public static readonly string MergeFileName = "NPC Appearances Merged.esp";

        public override string Name => "Initialize Patch";

        private readonly GameSelection game;
        private readonly RecordImporter.Factory importerFactory;

        public PatchInitializationTask(GameSelection game, RecordImporter.Factory importerFactory)
        {
            this.game = game;
            this.importerFactory = importerFactory;
        }

        protected override Task<Result> Run(BuildSettings settings)
        {
            var mod = new SkyrimMod(ModKey.FromNameAndExtension(MergeFileName), game.GameRelease.ToSkyrimRelease());
            var importer = importerFactory(mod);
            return Task.FromResult(new Result(mod, importer));
        }
    }
}
