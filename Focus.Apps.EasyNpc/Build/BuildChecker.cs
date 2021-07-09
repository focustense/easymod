using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Apps.EasyNpc.GameData.Records;
using Focus.Apps.EasyNpc.Profile;
using Focus.ModManagers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Focus.Apps.EasyNpc.Build
{
    public class BuildChecker<TKey>
        where TKey : struct
    {
        private readonly ArchiveFileMap archiveFileMap;
        private readonly IArchiveProvider archiveProvider;
        private readonly IReadOnlyList<string> loadOrder;
        private readonly IModPluginMapFactory modPluginMapFactory;
        private readonly IModResolver modResolver;
        private readonly IDictionary<Tuple<string, string>, NpcConfiguration<TKey>> npcConfigs;
        private readonly IReadOnlyProfileEventLog profileEventLog;

        public BuildChecker(
            IReadOnlyList<string> loadOrder, IEnumerable<NpcConfiguration<TKey>> npcConfigs,
            IModResolver modResolver, IModPluginMapFactory modPluginMapFactory, IArchiveProvider archiveProvider,
            IReadOnlyProfileEventLog profileEventLog)
        {
            this.npcConfigs = npcConfigs.ToDictionary(x => Tuple.Create(x.BasePluginName, x.LocalFormIdHex));
            this.loadOrder = loadOrder;
            this.modResolver = modResolver;
            this.modPluginMapFactory = modPluginMapFactory;
            this.archiveProvider = archiveProvider;
            this.profileEventLog = profileEventLog;
            archiveFileMap = new ArchiveFileMap(archiveProvider);
        }

        public IReadOnlyList<BuildWarning> CheckAll(
            IReadOnlyList<NpcConfiguration<TKey>> npcs, BuildSettings<TKey> buildSettings)
        {
            var warnings = new List<BuildWarning>();
            warnings.AddRange(CheckModSettings());
            var profileEvents = profileEventLog.ToList();
            warnings.AddRange(CheckOrphanedNpcs(npcs, profileEvents));
            warnings.AddRange(CheckMissingPlugins(profileEvents));
            warnings.AddRange(CheckForOverriddenArchives());
            warnings.AddRange(CheckModPluginConsistency(npcs));
            warnings.AddRange(CheckWigs(npcs, buildSettings.WigResolver, buildSettings.EnableDewiggify));
            return warnings.AsReadOnly();
        }

        private IEnumerable<BuildWarning> CheckForOverriddenArchives()
        {
            // It is not - necessarily - a major problem for the game itself if multiple mods provide the same BSA.
            // The game, and this program, will simply use whichever version is actually loaded, i.e. from the last mod
            // in the list. However, there's no obvious way to tell *which* mod is currently providing that BSA.
            // This means that the user might select a mod for some NPC, and we might believe we are pulling facegen
            // data from that mod's archive, but in fact we are pulling it from a different version of the archive
            // provided by some other mod, which might be fine, or might be totally broken.
            // It's also extremely rare, with the only known instance (at the time of writing) being a patch to the
            // Sofia follower mod that removes a conflicting script, i.e. doesn't affect facegen data at all.
            // So it may be an obscure theoretical problem that never comes up in practice, but if we do see it, then
            // it at least merits a warning, which the user can ignore if it's on purpose.
            var modPluginMap = modPluginMapFactory.DefaultMap();
            return archiveProvider.GetLoadedArchivePaths()
                .AsParallel()
                .Select(path => Path.GetFileName(path))
                .Select(f => new
                {
                    Name = f,
                    ProvidingMods = modPluginMap.GetModsForArchive(f).ToList()
                })
                .Where(x => x.ProvidingMods.Count > 1)
                .Select(x => new BuildWarning(
                    BuildWarningId.MultipleArchiveSources,
                    WarningMessages.MultipleArchiveSources(x.Name, x.ProvidingMods)));
        }

        private IEnumerable<BuildWarning> CheckMissingPlugins(IEnumerable<ProfileEvent> events)
        {
            return events
                .MostRecentByNpc()
                .WithMissingPlugins(loadOrder.ToHashSet(StringComparer.OrdinalIgnoreCase))
                .Select(x => npcConfigs.TryGetValue(Tuple.Create(x.BasePluginName, x.LocalFormIdHex), out var npc) ?
                    new
                    {
                        npc.BasePluginName,
                        npc.LocalFormIdHex,
                        npc.EditorId,
                        npc.Name,
                        FieldName = x.Field == NpcProfileField.FacePlugin ? "face" : "default",
                        PluginName = x.NewValue,
                    } : null)
                .Where(x => x != null)
                .Select(x => new BuildWarning(
                    new RecordKey(x.BasePluginName, x.LocalFormIdHex),
                    BuildWarningId.SelectedPluginRemoved,
                    WarningMessages.SelectedPluginRemoved(x.EditorId, x.Name, x.FieldName, x.PluginName)));
        }

        private IEnumerable<BuildWarning> CheckModPluginConsistency(IEnumerable<NpcConfiguration<TKey>> npcs)
        {
            archiveFileMap.EnsureInitialized();
            var modPluginMap = modPluginMapFactory.DefaultMap();
            return npcs.AsParallel()
                .Select(npc => CheckModPluginConsistency(npc, modPluginMap))
                .SelectMany(warnings => warnings);
        }

        private IEnumerable<BuildWarning> CheckModPluginConsistency(
            NpcConfiguration<TKey> npc, ModPluginMap modPluginMap)
        {
            // Our job is to keep NPC records and facegen data consistent. That means we do NOT care about any NPCs that
            // are either still using the master/vanilla plugin as the face source, or have identical face attributes,
            // e.g. UESP records that generally don't touch faces - UNLESS the current profile also uses a facegen data
            // mod for that NPC, which is covered later.
            if (string.IsNullOrEmpty(npc.FaceModName))
            {
                if (npc.RequiresFacegenData())
                    yield return new BuildWarning(
                        new RecordKey(npc),
                        BuildWarningId.FaceModNotSpecified,
                        WarningMessages.FaceModNotSpecified(npc.EditorId, npc.Name));
                yield break;
            }

            var modsProvidingFacePlugin = modPluginMap.GetModsForPlugin(npc.FacePluginName);
            if (!modPluginMap.IsModInstalled(npc.FaceModName))
            {
                yield return new BuildWarning(
                    new RecordKey(npc),
                    BuildWarningId.FaceModNotInstalled,
                    WarningMessages.FaceModNotInstalled(npc.EditorId, npc.Name, npc.FaceModName));
                yield break;
            }
            if (!modsProvidingFacePlugin.Contains(npc.FaceModName))
                yield return new BuildWarning(
                    npc.FacePluginName,
                    new RecordKey(npc),
                    BuildWarningId.FaceModPluginMismatch,
                    WarningMessages.FaceModPluginMismatch(npc.EditorId, npc.Name, npc.FaceModName, npc.FacePluginName));
            var faceMeshFileName = FileStructure.GetFaceMeshFileName(npc.BasePluginName, npc.LocalFormIdHex);
            var hasLooseFacegen = modResolver.GetModDirectories(npc.FaceModName)
                .Select(dir => Path.Combine(Settings.Default.ModRootDirectory, dir, faceMeshFileName))
                .Any(File.Exists);
            var hasArchiveFacegen = modPluginMap.GetArchivesForMod(npc.FaceModName)
                .Select(f => archiveFileMap.ContainsFile(f, faceMeshFileName))
                .Any(exists => exists);
            // If the selected plugin has overrides, then we want to see facegen data. On the other hand, if the
            // selected plugin does NOT have overrides, then a mod providing facegens will probably break something.
            if (npc.RequiresFacegenData() && !hasLooseFacegen && !hasArchiveFacegen)
                // This can mean the mod is missing the facegen, but can also happen if the BSA that would normally
                // include it isn't loaded, i.e. due to the mod or plugin being disabled.
                yield return new BuildWarning(
                    npc.FacePluginName,
                    new RecordKey(npc),
                    BuildWarningId.FaceModMissingFaceGen,
                    WarningMessages.FaceModMissingFaceGen(npc.EditorId, npc.Name, npc.FaceModName));
            else if (!npc.RequiresFacegenData() && (hasLooseFacegen || hasArchiveFacegen))
                yield return new BuildWarning(
                    npc.FacePluginName,
                    new RecordKey(npc),
                    BuildWarningId.FaceModExtraFaceGen,
                    WarningMessages.FaceModExtraFaceGen(npc.EditorId, npc.Name, npc.FaceModName));
            else if (hasLooseFacegen && hasArchiveFacegen)
                yield return new BuildWarning(
                    npc.FacePluginName,
                    new RecordKey(npc),
                    BuildWarningId.FaceModMultipleFaceGen,
                    WarningMessages.FaceModMultipleFaceGen(npc.EditorId, npc.Name, npc.FaceModName));
        }

        private static IEnumerable<BuildWarning> CheckModSettings()
        {
            var modRootDirectory = Settings.Default.ModRootDirectory;
            if (string.IsNullOrWhiteSpace(modRootDirectory))
                yield return new BuildWarning(
                    BuildWarningId.ModDirectoryNotSpecified,
                    WarningMessages.ModDirectoryNotSpecified());
            else if (!Directory.Exists(modRootDirectory))
                yield return new BuildWarning(
                    BuildWarningId.ModDirectoryNotFound,
                    WarningMessages.ModDirectoryNotFound(modRootDirectory));
        }

        private static IEnumerable<BuildWarning> CheckOrphanedNpcs(
            IEnumerable<NpcConfiguration<TKey>> npcs, IEnumerable<ProfileEvent> events)
        {
            var allPluginsInProfile = events.Select(x => x.BasePluginName).Distinct().ToList();
            var currentPlugins = npcs
                .Select(x => x.BasePluginName)
                .Distinct()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            return allPluginsInProfile
                .Where(p => !currentPlugins.Contains(p))
                .Select(p => new BuildWarning(
                    p, BuildWarningId.MasterPluginRemoved, WarningMessages.MasterPluginRemoved(p)));
        }

        private static IEnumerable<BuildWarning> CheckWigs(
            IEnumerable<NpcConfiguration<TKey>> npcs, IWigResolver<TKey> wigResolver, bool enableDewiggify)
        {
            var wigKeys = npcs.Select(x => x.FaceConfiguration?.Wig).Where(x => x != null).Distinct();
            var matchedWigKeys = wigResolver.ResolveAll(wigKeys)
                .Where(x => x.HairKeys.Any())
                .Select(x => x.WigKey)
                .ToHashSet();
            return npcs
                .Select(x => new { Npc = x, x.FaceConfiguration?.Wig })
                .Where(x => x.Wig != null && (!enableDewiggify || !matchedWigKeys.Contains(x.Wig.Key)))
                .Select(x => enableDewiggify ?
                    new BuildWarning(
                        new RecordKey(x.Npc),
                        x.Wig.IsBald ? BuildWarningId.FaceModWigNotMatchedBald : BuildWarningId.FaceModWigNotMatched,
                        x.Wig.IsBald ?
                            WarningMessages.FaceModWigNotMatchedBald(
                                x.Npc.EditorId, x.Npc.Name, x.Npc.FacePluginName, x.Wig.ModelName) :
                            WarningMessages.FaceModWigNotMatched(
                                x.Npc.EditorId, x.Npc.Name, x.Npc.FacePluginName, x.Wig.ModelName)
                        ) :
                        new BuildWarning(
                            BuildWarningId.FaceModWigConversionDisabled,
                            WarningMessages.FaceModWigConversionDisabled(
                                x.Npc.EditorId, x.Npc.Name, x.Npc.FacePluginName, x.Wig.IsBald)));
        }
    }
}
