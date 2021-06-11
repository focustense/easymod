using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc
{
    public class MutagenAdapter : IGameDataEditor<FormKey>
    {
        public IArchiveProvider ArchiveProvider { get; private set; }
        public GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> Environment { get; private set; }
        public string GameDataFolder { get; private set; }
        // Mutagen doesn't have an internal log, like XEdit Lib. (Because it doesn't need to, as it's a .NET library and
        // works with ordinary exception handling)
        public IExternalLog Log { get; init; } = new NullExternalLog();
        public IMergedPluginBuilder<FormKey> MergedPluginBuilder { get; private set; }
        public IEnumerable<ISkyrimModGetter> Mods => Environment.LoadOrder.Select(x => x.Value.Mod);
        public IModPluginMapFactory ModPluginMapFactory { get; private set; }

        private readonly ILogger log;

        public MutagenAdapter(ILogger log)
        {
            if (!GameLocations.TryGetDataFolder(GameRelease.SkyrimSE, out var dataFolder))
                throw new Exception("Couldn't find SkyrimSE game data folder");
            GameDataFolder = dataFolder;
            this.log = log;
        }

        public IEnumerable<Tuple<string, bool>> GetAvailablePlugins()
        {
            return LoadOrder.GetListings(GameRelease.SkyrimSE, GameDataFolder, true)
                .Select(x => Tuple.Create(x.ModKey.FileName, x.Enabled));
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
                MergedPluginBuilder = new MutagenMergedPluginBuilder(Environment, log);
                ModPluginMapFactory = new MutagenModPluginMapFactory(Environment);
            });
        }

        public IEnumerable<Hair<FormKey>> ReadHairRecords(string pluginName)
        {
            var modKey = ModKey.FromNameAndExtension(pluginName);
            var listing = Environment.LoadOrder.GetIfEnabled(modKey);
            return listing?.Mod?.HeadParts
                ?.Where(x => x.Type == HeadPart.TypeEnum.Hair && !x.Flags.HasFlag(HeadPart.Flag.IsExtraPart))
                ?.Select(x => new Hair<FormKey>
                {
                    Key = x.FormKey,
                    EditorId = x.EditorID,
                    Name = x.Name?.ToString(),
                    ModelFileName = x.Model?.File,
                    IsFemale = x.Flags.HasFlag(HeadPart.Flag.Female),
                    IsMale = x.Flags.HasFlag(HeadPart.Flag.Male),
                    ValidRaces = GetValidRaces(x).ToHashSet(),
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
                    var wigInfo = GetWigInfo(npcContext.Record);
                    npc.AddOverride(new NpcOverride<FormKey>(
                        modKey.FileName, faceOverrides, affectsFaceGen, itpoFileName, wigInfo));
                }
                else
                {
                    cache.Add(formKey, new NpcInfo<FormKey>
                    {
                        BasePluginName = modKey.FileName,
                        EditorId = npcContext.Record.EditorID,
                        IsFemale = npcContext.Record.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female),
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

        private IEnumerable<VanillaRace> GetValidRaces(IHeadPartGetter headPart)
        {
            if (headPart.ValidRaces.IsNull)
                return Enumerable.Empty<VanillaRace>();
            var raceList = headPart.ValidRaces.FormKey.AsLink<IFormListGetter>().Resolve(Environment.LinkCache);
            return raceList.Items
                .Select(x => x.FormKey.AsLink<IRaceGetter>().Resolve(Environment.LinkCache))
                .Select(x => InferRace(x.EditorID));
        }

        private NpcWigInfo<FormKey> GetWigInfo(INpcGetter npc)
        {
            if (npc.WornArmor.IsNull)
                return null;
            var isBald = npc.HeadParts
                .Select(x => x.FormKey.AsLink<IHeadPartGetter>().Resolve(Environment.LinkCache))
                .Where(x => x.Type == HeadPart.TypeEnum.Hair)
                .All(x => string.IsNullOrEmpty(x.Model?.File));
            var wornArmor = npc.WornArmor.Resolve(Environment.LinkCache);
            return wornArmor.Armature
                .Select(fk => fk.Resolve(Environment.LinkCache))
                .Where(x =>
                    // Search for ONLY hair, because some sadistic modders add hair flags to other parts.
                    x.BodyTemplate.FirstPersonFlags == BipedObjectFlag.Hair ||
                    x.BodyTemplate.FirstPersonFlags == BipedObjectFlag.LongHair ||
                    x.BodyTemplate.FirstPersonFlags == (BipedObjectFlag.Hair | BipedObjectFlag.LongHair))
                .Select(x => {
                    var modelFileName = x.WorldModel?.Where(x => x != null)?.FirstOrDefault()?.File;
                    var modelName = !string.IsNullOrEmpty(modelFileName) ?
                        Path.GetFileNameWithoutExtension(modelFileName) : null;
                    return new NpcWigInfo<FormKey>(x.FormKey, modelName, isBald);
                })
                .FirstOrDefault();
        }

        private static VanillaRace InferRace(string editorId)
        {
            return editorId switch
            {
                "NordRace" => VanillaRace.Nord,
                "ImperialRace" => VanillaRace.Imperial,
                "RedguardRace" => VanillaRace.Redguard,
                "BretonRace" => VanillaRace.Breton,
                "HighElfRace" => VanillaRace.HighElf,
                "DarkElfRace" => VanillaRace.DarkElf,
                "WoodElfRace" => VanillaRace.WoodElf,
                "OrcRace" => VanillaRace.Orc,
                "ElderRace" => VanillaRace.Elder,
                _ => 0,
            };
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