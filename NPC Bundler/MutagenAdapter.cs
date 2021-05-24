using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace NPC_Bundler
{
    public class MutagenAdapter : IGameDataEditor<FormKey>
    {
        public IArchiveProvider ArchiveProvider { get; private set; }
        public GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> Environment { get; private set; }
        public string GameDataFolder { get; private set; }
        public IEnumerable<ISkyrimModGetter> Mods => Environment.LoadOrder.Select(x => x.Value.Mod);
        public IModPluginMapFactory ModPluginMapFactory { get; private set; }

        public MutagenAdapter()
        {
            if (!GameLocations.TryGetDataFolder(GameRelease.SkyrimSE, out var dataFolder))
                throw new Exception("Couldn't find SkyrimSE game data folder");
            GameDataFolder = dataFolder;
        }

        public IEnumerable<string> GetAvailablePlugins()
        {
            return LoadOrder.GetListings(GameRelease.SkyrimSE, GameDataFolder, true)
                .Select(x => x.ModKey.FileName);
        }

        public IEnumerable<string> GetLoadedPlugins()
        {
            return Environment.LoadOrder.Select(x => x.Key.FileName);
        }

        public int GetLoadOrderIndex(string pluginName)
        {
            var modKey = ModKey.FromNameAndExtension(pluginName);
            return Environment.LoadOrder.IndexOf(modKey);
        }

        public bool IsMaster(string pluginName)
        {
            var modKey = ModKey.FromNameAndExtension(pluginName);
            return Environment.LoadOrder.TryGetValue(modKey, out var listing) &&
                listing.Mod.ModHeader.Flags.HasFlag(SkyrimModHeader.HeaderFlag.Master);
        }

        public Task Load(IEnumerable<string> pluginNames)
        {
            return Task.Run(() =>
            {
                var loadOrderKeys = pluginNames.Select(pluginName => ModKey.FromNameAndExtension(pluginName));
                var loadOrder = LoadOrder.Import<ISkyrimModGetter>(GameDataFolder, loadOrderKeys, GameRelease.SkyrimSE);
                var linkCache = loadOrder.ToImmutableLinkCache<ISkyrimMod, ISkyrimModGetter>();
                Environment =
                    new GameEnvironmentState<ISkyrimMod, ISkyrimModGetter>(GameDataFolder, loadOrder, linkCache, true);
                Environment.LinkCache.Warmup<Npc>();
                ArchiveProvider = new MutagenArchiveProvider(Environment);
                ModPluginMapFactory = new MutagenModPluginMapFactory(Environment);
            });
        }

        public void ReadNpcRecords(string pluginName, IDictionary<FormKey, IMutableNpc<FormKey>> cache)
        {
            var modKey = ModKey.FromNameAndExtension(pluginName);
            var listing = Environment.LoadOrder.GetIfEnabled(modKey);
            if (listing == null)
                return;

            var npcContexts = listing.Mod.EnumerateMajorRecordContexts<INpc, INpcGetter>(Environment.LinkCache);
            foreach (var npcContext in npcContexts)
            {
                var formKey = npcContext.Record.FormKey;
                if (formKey.ModKey != modKey)
                {
                    var npc = cache[formKey];
                    var faceOverrides = GetFaceOverrides(npcContext, out bool affectsFaceGen, out var itpoFileName);
                    npc.AddOverride(new NpcOverride<FormKey>(modKey.FileName, faceOverrides, affectsFaceGen, itpoFileName));
                }
                else
                {
                    cache.Add(formKey, new NpcInfo<FormKey>
                    {
                        BasePluginName = modKey.FileName,
                        EditorId = npcContext.Record.EditorID,
                        Key = formKey,
                        LocalFormIdHex = formKey.ID.ToString("X6"),
                        Name = npcContext.Record.Name?.ToString(),
                    });
                }
            }
        }

        private NpcFaceData<FormKey> GetFaceOverrides(
            IModContext<ISkyrimMod, ISkyrimModGetter, INpc, INpcGetter> npcContext, out bool affectsFaceGen,
            out string itpoPluginName)
        {
            affectsFaceGen = false;
            itpoPluginName = null;

            var npcRecord = npcContext.Record;
            var formLink = npcRecord.FormKey.AsLink<INpcGetter>();
            var previousOverride = formLink
                .ResolveAllContexts<ISkyrimMod, ISkyrimModGetter, INpc, INpcGetter>(Environment.LinkCache)
                .SkipWhile(x => x.ModKey != npcContext.ModKey)
                .Skip(1)
                .FirstOrDefault();

            if (previousOverride == null)   // We were already on the master
                return null;
            if (npcRecord.Equals(previousOverride.Record))
                itpoPluginName = previousOverride.ModKey.FileName;
            var previousNpcRecord = !string.IsNullOrEmpty(itpoPluginName) ?
                Environment.LoadOrder.GetMasterNpc(npcRecord.FormKey) : previousOverride.Record;


            var overrideFaceData = ReadFaceData(npcRecord);
            var previousFaceData = ReadFaceData(previousNpcRecord);
            var overrideRace = npcRecord.Race.FormKeyNullable;
            var previousRace = previousNpcRecord.Race.FormKeyNullable;
            affectsFaceGen = !NpcFaceData.EqualsForFaceGen(overrideFaceData, previousFaceData);
            return (!NpcFaceData.Equals(overrideFaceData, previousFaceData) || (overrideRace != previousRace)) ?
                overrideFaceData : null;
        }

        private static NpcFaceData<FormKey> ReadFaceData(INpcGetter npc)
        {
            return new NpcFaceData<FormKey>
            {
                FaceMorphs = ReadFaceMorphs(npc),
                FaceParts = ReadFaceParts(npc),
                FaceTextureSetId = npc.HeadTexture.FormKeyNullable,
                FaceTints = ReadFaceTints(npc),
                HairColorId = npc.HairColor.FormKeyNullable,
                HeadPartIds = npc.HeadParts.Select(x => x.FormKey).ToArray(),
                SkinTone = ReadSkinTone(npc),
            };
        }

        private static NpcFaceMorphs ReadFaceMorphs(INpcGetter npc)
        {
            if (npc.FaceMorph == null)
                return null;
            return new NpcFaceMorphs
            {
                BrowsForwardBack = npc.FaceMorph.BrowsForwardVsBack,
                BrowsInOut = npc.FaceMorph.BrowsInVsOut,
                BrowsUpDown = npc.FaceMorph.BrowsUpVsDown,
                CheeksForwardBack = npc.FaceMorph.CheeksForwardVsBack,
                CheeksUpDown = npc.FaceMorph.CheeksUpVsDown,
                ChinThinWide = npc.FaceMorph.ChinNarrowVsWide,
                ChinUnderbiteOverbite = npc.FaceMorph.ChinUnderbiteVsOverbite,
                ChinUpDown = npc.FaceMorph.ChinUpVsDown,
                EyesForwardBack = npc.FaceMorph.EyesForwardVsBack,
                EyesInOut = npc.FaceMorph.EyesInVsOut,
                EyesUpDown = npc.FaceMorph.EyesUpVsDown,
                JawForwardBack = npc.FaceMorph.JawForwardVsBack,
                JawNarrowWide = npc.FaceMorph.JawNarrowVsWide,
                JawUpDown = npc.FaceMorph.JawUpVsDown,
                LipsInOut = npc.FaceMorph.LipsInVsOut,
                LipsUpDown = npc.FaceMorph.LipsUpVsDown,
                NoseLongShort = npc.FaceMorph.NoseLongVsShort,
                NoseUpDown = npc.FaceMorph.NoseUpVsDown,
            };
        }

        private static NpcFaceParts ReadFaceParts(INpcGetter npc)
        {
            return npc.FaceParts != null ?
                new NpcFaceParts(npc.FaceParts.Nose, npc.FaceParts.Eyes, npc.FaceParts.Mouth) : null;
        }

        private static NpcFaceTint[] ReadFaceTints(INpcGetter npc)
        {
            return npc.TintLayers
                .Select(x => new NpcFaceTint(
                    x.Index.Value,
                    new NpcFaceTintColor(x.Color.Value.R, x.Color.Value.G, x.Color.Value.B, x.Color.Value.A),
                    x.InterpolationValue.Value))
                .ToArray();
        }

        private static NpcSkinTone ReadSkinTone(INpcGetter npc)
        {
            return npc.TextureLighting is Color c ? new NpcSkinTone(c.R, c.G, c.B) : null;
        }
    }
}