using Focus.Analysis.Execution;
using Focus.Analysis.Records;
using Focus.Providers.Mutagen;
using Focus.Providers.Mutagen.Analysis;
using Mutagen.Bethesda.Skyrim;
using Serilog;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Mutagen
{
    public class MutagenLoadOrderAnalyzer : LoadOrderAnalyzer
    {
        private readonly IReadOnlyList<string> baseMasters;
        private readonly IGroupCache groupCache;
        private readonly bool purgeOnComplete;

        public MutagenLoadOrderAnalyzer(IGroupCache groupCache, ISetupStatics setup, GameSelection game, ILogger log)
            : this(groupCache, setup, game, log, true) { }

        public MutagenLoadOrderAnalyzer(
            IGroupCache groupCache, ISetupStatics setup, GameSelection game, ILogger log, bool purgeOnComplete = true)
            : base(new RecordScanner(groupCache), log)
        {
            this.groupCache = groupCache;
            this.purgeOnComplete = purgeOnComplete;
            baseMasters = setup.GetBaseMasters(game.GameRelease).Select(x => x.FileName.String).ToList().AsReadOnly();
        }

        protected override void Configure(AnalysisRunner runner)
        {
            var referenceChecker = new ReferenceChecker<INpcGetter>(groupCache)
                .Configure(ConfigureNpcReferences);
            var assetPathConfig = AssetPathConfiguration.Builder()
                .For<IArmorAddonGetter>(armorAddon => armorAddon
                    .Add(AssetKind.Mesh, x => x.FirstPersonModel, x => x.File)
                    .Add(AssetKind.Mesh, x => x.WorldModel, x => x.File))
                .For<IArmorGetter>(armor => armor
                    .Add(AssetKind.Mesh, x => x.WorldModel, x => x.Model?.File)
                    .Add(
                        AssetKind.Icon, x => x.WorldModel,
                        x => new[] { x.Icons?.SmallIconFilename, x.Icons?.LargeIconFilename }))
                .For<IArtObjectGetter>(artObject => artObject
                    .Add(AssetKind.Mesh, x => x.Model?.File))
                .For<IHeadPartGetter>(headPart => headPart
                    .Add(AssetKind.Mesh, x => x.Model?.File)
                    .Add(AssetKind.Morph, x => x.Parts.Select(p => p.FileName)))
                .For<ITextureSetGetter>(textureSet => textureSet
                    .Add(AssetKind.Texture, x => new[]
                    {
                        x.Diffuse,
                        x.NormalOrGloss,
                        x.EnvironmentMaskOrSubsurfaceTint,
                        x.GlowOrDetailMap,
                        x.Height,
                        x.Environment,
                        x.Multilayer,
                        x.BacklightMaskOrSpecular,
                    }))
                .Build();
            var assetPathExtractor = new AssetPathExtractor<INpcGetter>(groupCache, assetPathConfig)
                .ConfigureRoutes(ConfigureNpcReferences);
            runner
                .Configure(RecordType.Npc, new NpcAnalyzer(groupCache, referenceChecker, assetPathExtractor))
                .Configure(RecordType.HeadPart, new HeadPartAnalyzer(groupCache));
        }

        protected override void OnCompleted(LoadOrderAnalysis analysis)
        {
            base.OnCompleted(analysis);
            if (purgeOnComplete)
                groupCache.Purge();
        }

        private static void ConfigureArmorReferences(IReferenceFollower<IArmorGetter> armor)
        {
            armor
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
                            .Select(x => x.FormKey.ToLinkGetter<ITextureSetGetter>())))
                    .Follow(x => x.WorldModel, g => g.AlternateTextures?.Select(x => x.NewTexture)))
                .Follow(x => x.Keywords)
                .FollowSelf(x => x.TemplateArmor)
                .Follow(x => x.WorldModel, g => g.Model?.AlternateTextures?.Select(t => t.NewTexture));
        }

        private static void ConfigureHeadPartReferences(IReferenceFollower<IHeadPartGetter> headPart)
        {
            headPart
                .Follow(x => x.Model?.AlternateTextures?.Select(t => t.NewTexture))
                .Follow(x => x.Color)
                .FollowSelf(x => x.ExtraParts)
                .Follow(x => x.TextureSet);
        }

        private void ConfigureNpcReferences(IReferenceFollower<INpcGetter> follower)
        {
            follower
                .WithPluginExclusions(baseMasters)
                .Follow(x => x.HairColor)
                .Follow(x => x.HeadParts, ConfigureHeadPartReferences)
                .Follow(x => x.HeadTexture)
                .Follow(x => x.Race, race => race
                    .Follow(x => x.HeadData, g => g.HeadParts.Select(x => x.Head), ConfigureHeadPartReferences)
                    .Follow(x => x.Skin, ConfigureArmorReferences))
                .Follow(x => x.WornArmor, ConfigureArmorReferences);
        }
    }
}
