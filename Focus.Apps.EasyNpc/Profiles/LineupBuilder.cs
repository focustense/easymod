#nullable enable

using Focus.Apps.EasyNpc.GameData.Files;
using Focus.ModManagers;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

namespace Focus.Apps.EasyNpc.Profiles
{
    public interface ILineupBuilder
    {
        IAsyncEnumerable<MugshotModel> Build(
            INpcBasicInfo npc, IEnumerable<string> affectingPlugins, ILookup<string, string> modSynonyms);
    }

    public class LineupBuilder : ILineupBuilder
    {
        // Placeholder mod for tracking base game plugins.
        private static readonly ModInfo BaseGameMod = new(string.Empty, "Vanilla");

        private readonly IReadOnlySet<string> basePluginNames;
        private readonly IFileSystem fs;
        private readonly MugshotFile genericFemaleFile;
        private readonly MugshotFile genericMaleFile;
        private readonly IModRepository modRepository;
        private readonly IMugshotRepository mugshotRepository;

        public LineupBuilder(
            IMugshotRepository mugshotRepository, IModRepository modRepository, string appAssetsPath,
            IReadOnlySet<string> basePluginNames)
            : this(new FileSystem(), mugshotRepository, modRepository, appAssetsPath, basePluginNames)
        {
        }

        public LineupBuilder(
            IFileSystem fs, IMugshotRepository mugshotRepository, IModRepository modRepository, string appAssetsPath,
            IReadOnlySet<string> basePluginNames)
        {
            this.fs = fs;
            this.basePluginNames = basePluginNames;
            this.modRepository = modRepository;
            this.mugshotRepository = mugshotRepository;

            genericFemaleFile = GetPlaceholderFile(appAssetsPath, true);
            genericMaleFile = GetPlaceholderFile(appAssetsPath, false);
        }

        public async IAsyncEnumerable<MugshotModel> Build(
            INpcBasicInfo npc, IEnumerable<string> affectingPlugins, ILookup<string, string> modSynonyms)
        {
            var mugshotFiles = (await mugshotRepository.GetMugshotFiles(npc))
                // We use ToLookup here instead of ToDictionary, because duplicates are actually possible, e.g. the same
                // path but with one PNG and one JPG.
                .ToLookup(x => x.TargetModName, StringComparer.CurrentCultureIgnoreCase);
            var includedMugshotModNames = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

            var modNamesFromPlugins = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            var pluginGroups = affectingPlugins
                .SelectMany(p => basePluginNames.Contains(p) ?
                    new[] { new { Plugin = p, ModKey = BaseGameMod as IModLocatorKey } } :
                    modRepository.SearchForFiles(p, false).Select(x => new { Plugin = p, x.ModKey }))
                .GroupBy(x => x.ModKey, x => x.Plugin, ModLocatorKeyComparer.Default)
                .Select(g => new { ModKey = g.Key, Plugins = g });
            foreach (var pluginGroup in pluginGroups)
            {
                var mugshotFile = GetMugshotFile(mugshotFiles, pluginGroup.ModKey, npc.IsFemale, modSynonyms);
                if (!string.IsNullOrEmpty(mugshotFile.TargetModName))
                    includedMugshotModNames.Add(mugshotFile.TargetModName);
                if (!pluginGroup.ModKey.IsEmpty() && pluginGroup.ModKey != BaseGameMod)
                    modNamesFromPlugins.Add(pluginGroup.ModKey.Name);
                yield return CreateMugshotModel(mugshotFile, pluginGroup.ModKey, pluginGroup.Plugins);
            }

            var faceGenPath = FileStructure.GetFaceMeshFileName(npc.BasePluginName, npc.LocalFormIdHex);
            var facegenMods = modRepository.SearchForFiles(faceGenPath, false)
                .Select(x => x.ModKey)
                // We shouldn't need to check for synonyms or do anything with IDs here, because the included mugshot
                // mod names originally come from the mod repo, and so do the facegen mods here. Their names can't be
                // different unless the repo itself is broken, or incorrectly mocked in a test.
                .Where(x => !modNamesFromPlugins.Contains(x.Name));
            foreach (var facegenMod in facegenMods)
            {
                var mugshotFile = GetMugshotFile(mugshotFiles, facegenMod, npc.IsFemale, modSynonyms);
                if (!string.IsNullOrEmpty(mugshotFile.TargetModName))
                    includedMugshotModNames.Add(mugshotFile.TargetModName);
                yield return CreateMugshotModel(mugshotFile, facegenMod);
            }

            var extraMugshotFiles = mugshotFiles
                .Where(x => !includedMugshotModNames.Contains(x.Key))
                .SelectMany(g => g.Select(file => (g.Key, file)));
            foreach (var (modKey, file) in extraMugshotFiles)
                yield return CreateMugshotModel(file, new ModLocatorKey(string.Empty, modKey));
        }

        private MugshotModel CreateMugshotModel(
            MugshotFile file, IModLocatorKey? modKey, IEnumerable<string>? plugins = null)
        {
            var mod = ResolveMod(modKey);
            return new MugshotModel
            {
                InstalledMod = mod,
                InstalledPlugins = (plugins ?? Enumerable.Empty<string>()).ToList().AsReadOnly(),
                IsPlaceholder = file.IsPlaceholder,
                ModName = GetMugshotModName(mod) ?? modKey?.Name ?? file.TargetModName,
                Path = file.Path,
            };
        }

        private MugshotFile GetMugshotFile(
            ILookup<string, MugshotFile> providedFiles, IModLocatorKey? modKey, bool female,
            ILookup<string, string> modSynonyms)
        {
            var mod = ResolveMod(modKey);
            if (mod is null)
                return female ? genericFemaleFile : genericMaleFile;
            // TODO: Currently only name-to-name mod synonyms are supported, but we could extend the capabilities by
            // adding some metadata to the mugshot packs containing the mod ID (which is also in the mod info), and
            // match on that.
            var exactMatch = providedFiles[mod.Name].FirstOrDefault();
            if (exactMatch is not null)
                return exactMatch;
            var synonymMatch = mod.Components
                .Select(x => x.Name)
                .Prepend(mod.Name)
                .SelectMany(x => modSynonyms[x])
                .Select(synonymName => providedFiles[synonymName].FirstOrDefault())
                .NotNull()
                .FirstOrDefault();
            if (synonymMatch is not null)
                return synonymMatch;
            return female ? genericFemaleFile : genericMaleFile;
        }

        private static string? GetMugshotModName(ModInfo? mod)
        {
            if (mod is null)
                return null;
            return !string.IsNullOrEmpty(mod.Name) ?
                mod.Name :
                mod.Components.FirstOrDefault(x => !string.IsNullOrEmpty(x.Name))?.Name;
        }

        private MugshotFile GetPlaceholderFile(string assetsDirectory, bool female)
        {
            var filePrefix = female ? "female" : "male";
            return new MugshotFile(fs.Path.Combine(assetsDirectory, $"{filePrefix}-silhouette.png"));
        }

        private ModInfo? ResolveMod(IModLocatorKey? modKey)
        {
            if (modKey is null)
                return null;
            if (modKey == BaseGameMod)
                return BaseGameMod;
            return modRepository.FindByKey(modKey);
        }
    }
}
