using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Environment;
using Focus.ModManagers;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Focus.Apps.EasyNpc.Profiles
{
    public interface ILineupBuilder
    {
        IAsyncEnumerable<Mugshot> Build(INpcBasicInfo npc, IEnumerable<string> affectingPlugins);
    }

    public class LineupBuilder : ILineupBuilder, IDisposable
    {
        // Placeholders for tracking base game content.
        private static readonly ModComponentInfo BaseGameComponent = new(
            new ModLocatorKey(string.Empty, "Vanilla"), string.Empty, "Vanilla", string.Empty);
        private static readonly ModInfo BaseGameMod = new(string.Empty, "Vanilla");

        private readonly Subject<bool> disposed = new();
        private readonly IFileSystem fs;
        private readonly MugshotFile genericFemaleFile;
        private readonly MugshotFile genericMaleFile;
        private readonly IReadOnlyLoadOrderGraph loadOrderGraph;
        private readonly IModRepository modRepository;
        private readonly IMugshotRepository mugshotRepository;

        private ILookup<string, string> modSynonyms = Enumerable.Empty<string>().ToLookup(x => x);

        public LineupBuilder(
            IObservableAppSettings appSettings, IMugshotRepository mugshotRepository, IModRepository modRepository,
            IReadOnlyLoadOrderGraph loadOrderGraph)
            : this(new FileSystem(), appSettings, mugshotRepository, modRepository, loadOrderGraph)
        {
        }

        public LineupBuilder(
            IFileSystem fs, IObservableAppSettings appSettings, IMugshotRepository mugshotRepository,
            IModRepository modRepository, IReadOnlyLoadOrderGraph loadOrderGraph)
        {
            this.fs = fs;
            this.loadOrderGraph = loadOrderGraph;
            this.modRepository = modRepository;
            this.mugshotRepository = mugshotRepository;

            genericFemaleFile = GetPlaceholderFile(appSettings.StaticAssetsPath, true);
            genericMaleFile = GetPlaceholderFile(appSettings.StaticAssetsPath, false);

            appSettings.MugshotRedirectsObservable
                .TakeUntil(disposed)
                .Subscribe(x => modSynonyms = x.ToLookup(r => r.ModName, r => r.Mugshots));
        }

        public async IAsyncEnumerable<Mugshot> Build(INpcBasicInfo npc, IEnumerable<string> affectingPlugins)
        {
            var mugshotFiles = (await mugshotRepository.GetMugshotFiles(npc))
                // We use ToLookup here instead of ToDictionary, because duplicates are actually possible, e.g. the same
                // path but with one PNG and one JPG.
                .ToLookup(x => x.TargetModName, StringComparer.CurrentCultureIgnoreCase);
            var includedMugshotModNames = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

            var modNamesFromPlugins = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            var pluginGroups = affectingPlugins
                .SelectMany(p => loadOrderGraph.IsImplicit(p) ?
                    new[] { new { Plugin = p, ModComponent = BaseGameComponent, ModKey = BaseGameMod as IModLocatorKey } } :
                    modRepository.SearchForFiles(p, false).Select(x => new { Plugin = p, x.ModComponent, x.ModKey }))
                .GroupBy(x => x.ModKey, ModLocatorKeyComparer.Default)
                .Select(g => new
                {
                    ModKey = g.Key,
                    Plugins = g.Select(x => x.Plugin),
                    Components = g.Select(x => x.ModComponent)
                });
            foreach (var pluginGroup in pluginGroups)
            {
                var mugshotFile = GetMugshotFile(mugshotFiles, pluginGroup.ModKey, npc.IsFemale, modSynonyms);
                if (!string.IsNullOrEmpty(mugshotFile.TargetModName))
                    includedMugshotModNames.Add(mugshotFile.TargetModName);
                if (!pluginGroup.ModKey.IsEmpty() && pluginGroup.ModKey != BaseGameMod)
                    modNamesFromPlugins.Add(pluginGroup.ModKey.Name);
                yield return CreateMugshotModel(
                    mugshotFile, pluginGroup.ModKey, pluginGroup.Components, pluginGroup.Plugins);
            }

            var faceGenPath = FileStructure.GetFaceMeshFileName(npc.BasePluginName, npc.LocalFormIdHex);
            var facegenGroups = modRepository.SearchForFiles(faceGenPath, true)
                .GroupBy(x => x.ModKey, x => x.ModComponent)
                // We shouldn't need to check for synonyms or do anything with IDs here, because the included mugshot
                // mod names originally come from the mod repo, and so do the facegen mods here. Their names can't be
                // different unless the repo itself is broken, or incorrectly mocked in a test.
                .Where(x => !modNamesFromPlugins.Contains(x.Key.Name));
            foreach (var facegenGroup in facegenGroups)
            {
                var mugshotFile = GetMugshotFile(mugshotFiles, facegenGroup.Key, npc.IsFemale, modSynonyms);
                if (!string.IsNullOrEmpty(mugshotFile.TargetModName))
                    includedMugshotModNames.Add(mugshotFile.TargetModName);
                yield return CreateMugshotModel(mugshotFile, facegenGroup.Key, facegenGroup);
            }

            var extraMugshotFiles = mugshotFiles
                .Where(x => !includedMugshotModNames.Contains(x.Key))
                .SelectMany(g => g.Select(file => (g.Key, file)));
            foreach (var (modKey, file) in extraMugshotFiles)
                yield return CreateMugshotModel(file, new ModLocatorKey(string.Empty, modKey));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed.IsDisposed)
                return;
            disposed.OnNext(true);
            disposed.Dispose();
        }

        private Mugshot CreateMugshotModel(
            MugshotFile file, IModLocatorKey? modKey, IEnumerable<ModComponentInfo>? components = null,
            IEnumerable<string>? plugins = null)
        {
            var mod = ResolveMod(modKey);
            return new Mugshot
            {
                InstalledComponents = (components ?? Enumerable.Empty<ModComponentInfo>()).ToList().AsReadOnly(),
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
