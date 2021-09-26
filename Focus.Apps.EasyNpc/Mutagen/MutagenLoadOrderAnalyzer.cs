using Focus.Analysis.Execution;
using Focus.Analysis.Records;
using Focus.Providers.Mutagen.Analysis;
using Mutagen.Bethesda.Skyrim;
using Serilog;
using System.Linq;

namespace Focus.Apps.EasyNpc.Mutagen
{
    public class MutagenLoadOrderAnalyzer : LoadOrderAnalyzer
    {
        private readonly IGroupCache groupCache;
        private readonly bool purgeOnComplete;

        public MutagenLoadOrderAnalyzer(IGroupCache groupCache, ILogger log)
            : this(groupCache, log, true) { }

        public MutagenLoadOrderAnalyzer(IGroupCache groupCache, ILogger log, bool purgeOnComplete = true)
            : base(new RecordScanner(groupCache), log)
        {
            this.groupCache = groupCache;
            this.purgeOnComplete = purgeOnComplete;
        }

        protected override void Configure(AnalysisRunner runner)
        {
            var referenceChecker = new ReferenceChecker<INpcGetter>(groupCache)
                .Configure(f => f
                    .Follow(x => x.HairColor)
                    .Follow(x => x.HeadParts, headPart => headPart
                        .Follow(x => x.Model?.AlternateTextures?.Select(t => t.NewTexture))
                        .Follow(x => x.Color)
                        .FollowSelf(x => x.ExtraParts)
                        .Follow(x => x.TextureSet))
                    .Follow(x => x.HeadTexture)
                    .Follow(x => x.WornArmor, armor => armor
                        .Follow(x => x.Armature, addon => addon
                            .Follow(x => x.AdditionalRaces)
                            .Follow(x => x.ArtObject, artObject => artObject
                                .Follow(x => x.Model?.AlternateTextures?.Select(t => t.NewTexture)))
                            .Follow(x => x.FirstPersonModel, g => g.AlternateTextures?.Select(x => x.NewTexture))
                            .Follow(x => x.Race)
                            .Follow(x => x.SkinTexture)
                            .Follow(x => x.TextureSwapList, swapList => swapList
                                .Follow(x => x.Items
                                    .Where(x => x.Type == typeof(ITextureSetGetter))
                                    .Select(x => x.FormKey.AsLinkGetter<ITextureSetGetter>())))
                            .Follow(x => x.WorldModel, g => g.AlternateTextures?.Select(x => x.NewTexture)))
                        .Follow(x => x.Keywords)
                        .FollowSelf(x => x.TemplateArmor)
                        .Follow(x => x.WorldModel, g => g.Model?.AlternateTextures?.Select(t => t.NewTexture))));
            runner
                .Configure(RecordType.Npc, new NpcAnalyzer(groupCache, referenceChecker))
                .Configure(RecordType.HeadPart, new HeadPartAnalyzer(groupCache));
        }

        protected override void OnCompleted(LoadOrderAnalysis analysis)
        {
            base.OnCompleted(analysis);
            if (purgeOnComplete)
                groupCache.Purge();
        }
    }
}
