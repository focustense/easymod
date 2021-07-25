using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Apps.EasyNpc.Profile;
using Focus.Files;
using Focus.ModManagers;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build
{
    static class MergedFolder
    {
        private static readonly Regex TexturePathExpression = new(
                @"[\w\s\p{P}]+\.dds",
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        public static void Build<TKey>(
            IReadOnlyList<NpcConfiguration<TKey>> npcs, MergedPluginResult mergeInfo, IArchiveProvider archiveProvider,
            IFaceGenEditor faceGenEditor, ModPluginMap modPluginMap, IModResolver modResolver,
            BuildSettings<TKey> buildSettings, ProgressViewModel progress, ILogger logger)
            where TKey : struct
        {
            var log = logger.ForContext("Type", "MergedFolder");

            var modRootDirectory = Settings.Default.ModRootDirectory;
            if (string.IsNullOrEmpty(modRootDirectory))
            {
                progress.ErrorMessage = "No mod directory specified; cannot build merged mod.";
                return;
            }

            // Several adjustments will be made to the progress current/max to accommodate different stages of unknown
            // precise sizes. The amount of progress accounted for by each stage is arbitrary; here's what it's set at:
            //   - Indexing: 10%
            //   - Support meshes/morphs: 20%
            //   - Support textures: 20%
            //   - Facegen meshes/textures: 50%

            progress.StartStage("Creating merge output directory");
            var outDir = Path.Combine(Settings.Default.ModRootDirectory, buildSettings.OutputModName);
            Directory.CreateDirectory(outDir);
            log.Information("Merge folder is ready at {MergeDirectoryName}", outDir);

            // The best way to avoid making this process obscenely complicated and having to iterate through all the
            // mod directories and BSAs multiple times is build an index of ALL of the files in ALL included mods ahead
            // of time. This takes up a decent chunk of memory, but, hey, we're talking about users who are running
            // modded Skyrim and incorporating NPC mods full of 4K and 8K textures, so they can probably afford it.
            progress.StartStage("Building file index");
            var allFaceMods = npcs
                .Select(x => x.FaceModName)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            // For progress reporting, we select an entirely arbitrary 5% of the total for building the index. We won't
            // know what the real total is until we actually read the NIFs, but the max can be adjusted as we go along.
            progress.MaxProgress = allFaceMods.Count * 20;
            var fileIndices = allFaceMods
                .Select(modName =>
                {
                    progress.CurrentProgress++;
                    progress.ItemName = modName;
                    var modPaths = modResolver.GetModDirectories(modName)
                        .Select(dir => Path.Combine(modRootDirectory, dir))
                        .ToList();
                    return new ModFileIndex
                    {
                        ModName = modName,
                        ModPaths = modPaths.AsReadOnly(),
                        LooseFiles = modPaths
                            .SelectMany(modPath => Directory
                                .EnumerateFiles(modPath, "*.*", SearchOption.AllDirectories)
                                .Select(f => new LooseFile
                                {
                                    AbsolutePath = f,
                                    DirectoryPath = modPath,
                                    RelativePath = Path.GetRelativePath(modPath, f)
                                }))
                            .GroupBy(x => x.RelativePath, StringComparer.OrdinalIgnoreCase)
                            .Select(g => g.Last())
                            .ToDictionary(x => x.RelativePath, StringComparer.OrdinalIgnoreCase),
                        ArchiveFiles = modPluginMap.GetArchivesForMod(modName)
                                .Select(archiveName => archiveProvider.GetArchivePath(archiveName))
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .Select(archivePath => new
                                {
                                    ArchivePath = archivePath,
                                    Files = new HashSet<string>(
                                        archiveProvider.GetArchiveFileNames(archivePath),
                                        StringComparer.OrdinalIgnoreCase) as IReadOnlySet<string>
                                })
                                .ToDictionary(x => x.ArchivePath, x => x.Files)
                    };
                })
                .ToList();
            log.Information(
                "{ModCount} mods indexed containing {LooseFileCount} loose and {ArchiveFileCount} archived files",
                fileIndices.Count,
                fileIndices.Sum(x => x.LooseFiles.Count),
                fileIndices.Sum(x => x.ArchiveFiles.Sum(a => a.Value.Count)));
            progress.JumpTo(0.05f);

            bool TryMergeFile(ModFileIndex index, string relativePath, out string outFileName, bool incProgress = true)
            {
                log.Debug("Processing file: {SourceFileName}", relativePath);
                outFileName = Path.Combine(outDir, relativePath);
                if (File.Exists(outFileName))
                {
                    log.Warning("File {MergeFileName} already exists and will be skipped.", outFileName);
                    return true;
                }

                if (index.LooseFiles.TryGetValue(relativePath, out var looseFile))
                {
                    log.Debug(
                        "File {SourceFileName} found as loose file in mod {ModName}",
                        relativePath, index.ModName);
                    progress.ItemName = $"[{index.ModName}] - Loose - {relativePath}";
                    Directory.CreateDirectory(Path.GetDirectoryName(outFileName));
                    File.Copy(looseFile.AbsolutePath, outFileName);
                    log.Information(
                        "Copied {SourceFileName} from {ModPath} to {MergeFileName}",
                        relativePath, looseFile.DirectoryPath, outFileName);
                    if (incProgress)
                        progress.CurrentProgress++;
                    return true;
                }

                var containingArchiveFile = index.ArchiveFiles
                    .Where(x => x.Value.Contains(relativePath, StringComparer.OrdinalIgnoreCase))
                    .Select(x => x.Key)
                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(containingArchiveFile))
                {
                    log.Debug(
                        "File {SourceFileName} found in archive {ArchiveName} belonging to mod {ModName}",
                        relativePath, Path.GetFileName(containingArchiveFile), index.ModName);
                    var archiveShortName = Path.GetFileName(containingArchiveFile);
                    progress.ItemName = $"[{index.ModName}] - {archiveShortName} - {relativePath}";
                    Directory.CreateDirectory(Path.GetDirectoryName(outFileName));
                    if (archiveProvider.CopyToFile(containingArchiveFile, relativePath, outFileName))
                        log.Information(
                            "Extracted {SourceFileName} from {ArchiveName} in {ModName} to {MergeFileName}",
                            relativePath, Path.GetFileName(containingArchiveFile), index.ModName, outFileName);
                    else
                        log.Warning(
                            "Failed to extract {SourceFileName} from {ArchiveName} in {ModName}",
                            relativePath, Path.GetFileName(containingArchiveFile), index.ModName);
                    if (incProgress)
                        progress.CurrentProgress++;
                    return true;
                }

                log.Debug("File {SourceFileName} not found in mod {ModName}", relativePath, index.ModName);
                return false;
            }

            progress.StartStage("Copying head part meshes and morphs");
            progress.AdjustRemaining(mergeInfo.Meshes.Count + mergeInfo.Morphs.Count, 0.2f); // 25% when done
            // Don't really want to mess with the actual merge info in case we need it later, but we do want to be able
            // to remove from the file sets as we go along, so we're not doing a gorillion redundant checks.
            // So, just make a copy of each set.
            var meshFileNames = new HashSet<string>(mergeInfo.Meshes);
            var morphFileNames = new HashSet<string>(mergeInfo.Morphs);
            var textureFileNames = mergeInfo.Textures
                .Where(f => !FileStructure.IsFaceGen(f)) // FaceGen already covered in its own subtask
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var processedFaceTints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var referencedFaceTints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddReferencedTextures(string meshFileName)
            {
                foreach (var rawTexturePath in GetReferencedTexturePaths(meshFileName))
                {
                    var texturePath = ExtractTexturePath(rawTexturePath);

                    // Face tints are a little weird in terms of the rules we want to apply and the warnings we want to
                    // generate. Missing facetints won't cause blackface like other facegen inconsistencies, they just
                    // won't do anything at all (i.e. no "makeup" or warpaint). It is normal for a mod to edit an NPCs
                    // face, and supply a head mesh, but no tint; it is also somewhat common, albeit annoying, for head
                    // meshes to reference completely bogus and invalid face tints, which modders sometimes don't notice
                    // because the game only looks at the facetint that *does* exist in the usual facegen path.
                    //
                    // What this all boils down to is that we should never expect a face tint texture to exist, *unless*
                    // it is specifically referenced explicitly in the plugin's NPC_ record (i.e. already in the
                    // textureFileNames), or implicitly in the head mesh. But the former is likely to be "somewhat bad"
                    // in the sense of producing unwanted/unexpected results, whereas the latter is just an oddity and
                    // we have no idea if it's really a problem. So we have to treat these two scenarios differently,
                    // and track the "implicitly referenced but not present" face tints from those in NPC records.
                    if (FileStructure.IsFaceGen(texturePath))
                        referencedFaceTints.Add(texturePath);
                    else
                        textureFileNames.Add(texturePath);
                }
            }

            foreach (var index in fileIndices)
            {
                progress.ItemName = $"[{index.ModName}] - Checking requirements";
                IterateAndDrain(meshFileNames, meshFileName =>
                {
                    if (TryMergeFile(index, meshFileName, out var outFileName))
                    {
                        AddReferencedTextures(outFileName);
                        return true;
                    }
                    return false;
                });
                IterateAndDrain(morphFileNames, morphFileName => TryMergeFile(index, morphFileName, out var _));
            }
            progress.JumpTo(0.25f);

            // TODO: Improve the ProgressViewModel so we don't need this stuff. Allow to define stages, stage weights,
            // and stage sizes, and have it do the rest automatically.
            float facegenStageSize = buildSettings.EnableDewiggify ? 0.3f : 0.5f;
            progress.StartStage("Copying FaceGen data");
            progress.AdjustRemaining(npcs.Count, facegenStageSize); // 75% when done
            foreach (var npc in npcs)
            {
                progress.ItemName = $"'{npc.Name}' ({npc.BasePluginName} - {npc.EditorId})";
                var index = fileIndices.SingleOrDefault(x =>
                    string.Equals(x.ModName, npc.FaceModName, StringComparison.OrdinalIgnoreCase));
                if (index != null)
                {
                    var faceMeshFileName = FileStructure.GetFaceMeshFileName(npc.BasePluginName, npc.LocalFormIdHex);
                    if (TryMergeFile(index, faceMeshFileName, out var outFaceMeshFileName, false))
                        AddReferencedTextures(outFaceMeshFileName);

                    var faceTintFileName = FileStructure.GetFaceTintFileName(npc.BasePluginName, npc.LocalFormIdHex);
                    if (TryMergeFile(index, faceTintFileName, out var _, false))
                    {
                        processedFaceTints.Add(faceTintFileName);
                        textureFileNames.Remove(faceTintFileName);
                    }
                }
                progress.CurrentProgress++;
            }
            progress.JumpTo(0.25f + facegenStageSize);

            if (buildSettings.EnableDewiggify && mergeInfo.WigConversions.Count > 0)
            {
                progress.StartStage("Processing wig conversions");
                progress.AdjustRemaining(mergeInfo.WigConversions.Count, 0.2f);
                var npcsByFormId = npcs.ToDictionary(x => Tuple.Create(x.BasePluginName, x.LocalFormIdHex));
                using var tempFileCache = new TempFileCache();
                Parallel.ForEach(mergeInfo.WigConversions, wigConversion =>
                {
                    var npcKey = Tuple.Create(wigConversion.BasePluginName, wigConversion.LocalFormIdHex);
                    var npc = npcsByFormId[npcKey];
                    var faceMeshFileName = FileStructure.GetFaceMeshFileName(npc.BasePluginName, npc.LocalFormIdHex);
                    var faceGenPath = Path.Combine(buildSettings.OutputDirectory, faceMeshFileName);
                    progress.ItemName = $"'{npc.Name}' ({npc.BasePluginName} - {npc.EditorId}) -- {faceMeshFileName}";
                    faceGenEditor.ReplaceHeadParts(
                        faceGenPath, wigConversion.RemovedHeadParts, wigConversion.AddedHeadParts,
                        wigConversion.HairColor, tempFileCache);
                    progress.CurrentProgress++;
                });
                progress.JumpTo(0.75f);
            }

            progress.StartStage("Copying textures");
            progress.AdjustRemaining(textureFileNames.Count, 0.2f); // 95% when done
            foreach (var index in fileIndices)
            {
                progress.ItemName = $"[{index.ModName}] - Checking requirements";
                IterateAndDrain(textureFileNames, textureFileName =>
                    processedFaceTints.Contains(textureFileName) || TryMergeFile(index, textureFileName, out var _));
            }
            progress.JumpTo(0.95f);

            progress.StartStage("Checking for problems");
            // Since we drain the sets as we copy files, anything left in the sets was by definition not found.
            var missingFiles = meshFileNames.Concat(morphFileNames).Concat(textureFileNames);
            foreach (var sourceFileName in missingFiles)
                log.Warning("Couldn't find a source for file {SourceFileName} in any included mods.", sourceFileName);
            if (missingFiles.Any())
                log.Warning(MissingFileWarningHelpText);

            referencedFaceTints.ExceptWith(processedFaceTints);
            foreach (var sourceFileName in referencedFaceTints)
                log.Information(
                    "Face tint file {SourceFileName} is implicitly referenced by the head mesh in its chosen mod, " +
                    "but is not provided by that mod. This is often harmless but MAY produce unexpected results, " +
                    "such as missing makeup/warpaint.",
                    sourceFileName);

            progress.StartStage("Done");
            progress.CurrentProgress = progress.MaxProgress;
        }

        private static string ExtractTexturePath(string rawTexturePath)
        {
            var texturePath = rawTexturePath;
            try
            {
                if (Path.IsPathRooted(texturePath))
                {
                    texturePath =
                        GetPathAfter(texturePath, @"data\textures", 5) ??
                        GetPathAfter(texturePath, @"data/textures", 5) ??
                        GetPathAfter(texturePath, @"\textures\", 1) ??
                        GetPathAfter(texturePath, @"/textures\", 1) ??
                        GetPathAfter(texturePath, @"\textures/", 1) ??
                        GetPathAfter(texturePath, @"/textures/", 1) ??
                        GetPathAfter(texturePath, @"\data\", 1) ??
                        GetPathAfter(texturePath, @"/data\", 1) ??
                        GetPathAfter(texturePath, @"\data/", 1) ??
                        GetPathAfter(texturePath, @"/data/", 1) ??
                        texturePath;
                }
            }
            catch (Exception)
            {
                // Just use the best we were able to come up with before the error.
            }
            return texturePath.PrefixPath("textures");
        }

        private static string GetPathAfter(string path, string search, int offset)
        {
            var index = path.LastIndexOf(search, StringComparison.OrdinalIgnoreCase);
            return index >= 0 ? path.Substring(index + offset) : null;
        }

        // This is kind of a horrible hack to read texture paths from a NIF without actually parsing the file.
        // And... it seems to work pretty much every time.
        // We'll replace this when we have real niftools support, for now it does the job.
        private static IEnumerable<string> GetReferencedTexturePaths(string nifFileName)
        {
            var fileText = File.ReadAllText(nifFileName);
            var match = TexturePathExpression.Match(fileText);
            while (match.Success)
            {
                yield return match.Value;
                match = match.NextMatch();
            }
        }

        private static void IterateAndDrain<T>(ISet<T> set, Func<T, bool> action)
        {
            var processed = new List<T>();
            foreach (var item in set)
            {
                if (action(item))
                    processed.Add(item);
            }
            set.ExceptWith(processed);
        }

        private static readonly string MissingFileWarningHelpText = @"
***** IMPORTANT: Missing files are NOT necessarily errors! *****

These warnings are for debugging purposes only, and simply mean that the merged mod will not carry its own version of
the file; instead, the game will obtain it from the winning mod in your mod order.

This is completely normal for mods that depend on vanilla assets or other asset-providing mods, such as Beards or KS
Hairdos. Most NPC overhauls do NOT repackage shared assets, so these warnings are expected. As long as the required mods
are installed, you won't have any problems.

Use these warnings as a guide if you experience CTDs, blackface, or texture bugs (purple/white textures), and ensure
that the resource mods are installed correctly. If you aren't having such issues, you can ignore the warnings.
";
    }

    class ModFileIndex
    {
        public string ModName { get; init; }
        public IReadOnlyList<string> ModPaths { get; init; }
        public IReadOnlyDictionary<string, LooseFile> LooseFiles { get; init; }
        public IDictionary<string, IReadOnlySet<string>> ArchiveFiles { get; init; }
    }

    class LooseFile
    {
        public string AbsolutePath { get; init; }
        public string DirectoryPath { get; init; }
        public string RelativePath { get; init; }
    }
}
