using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using XeLib.API;

namespace NPC_Bundler
{
    static class MergedFolder
    {
        private static readonly HashSet<string> SupportFileExtensions = new(
            new[] { "nif", "dds", "tri" }, StringComparer.OrdinalIgnoreCase);
        private static readonly Regex TexturePathExpression = new(
                @"[\w\s\p{P}]+\.dds",
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        public static void Build(
            IReadOnlyList<NpcConfiguration> npcs, MergedPluginResult mergeInfo, string outputModName,
            ProgressViewModel progress)
        {
            var modRootDirectory = BundlerSettings.Default.ModRootDirectory;
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

            progress.StartStage("Preparing");
            var modPluginMap = ModPluginMap.ForDirectory(modRootDirectory);
            var dataPath = Meta.GetGlobal("DataPath");

            progress.StartStage("Creating merge output directory");
            var outDir = Path.Combine(BundlerSettings.Default.ModRootDirectory, outputModName);
            Directory.CreateDirectory(outDir);

            // The best way to avoid making this process obscenely complicated and having to iterate through all the
            // mod directories and BSAs multiple times is build an index of ALL of the files in ALL included mods ahead
            // of time. This takes up a decent chunk of memory, but, hey, we're talking about users who are running
            // modded Skyrim and incorporating NPC mods full of 4K and 8K textures, so they can probably afford it.
            progress.StartStage("Building file index");
            var allFaceMods = npcs.Select(x => x.FaceModName).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
            // For progress reporting, we select an entirely arbitrary 5% of the total for building the index. We won't
            // know what the real total is until we actually read the NIFs, but the max can be adjusted as we go along.
            progress.MaxProgress = allFaceMods.Count * 20;
            var fileIndices = allFaceMods
                .Select(modName =>
                {
                    progress.CurrentProgress++;
                    progress.ItemName = modName;
                    var modPath = Path.Combine(modRootDirectory, modName);
                    return new ModFileIndex
                    {
                        ModName = modName,
                        ModPath = modPath,
                        LooseFiles = new HashSet<string>(
                            Directory.EnumerateFiles(modPath, "*.*", SearchOption.AllDirectories)
                                .Select(fileName => Path.GetRelativePath(modPath, fileName)),
                            StringComparer.OrdinalIgnoreCase),
                        ArchiveFiles = modPluginMap.GetArchivesForMod(modName)
                            .Select(archiveName => Path.Combine(dataPath, archiveName))
                            .Select(archivePath => new
                            {
                                ArchivePath = archivePath,
                                Files = new HashSet<string>(
                                    Resources.GetContainerFiles(archivePath, ""),
                                    StringComparer.OrdinalIgnoreCase) as IReadOnlySet<string>
                            })
                            .ToDictionary(x => x.ArchivePath, x => x.Files)
                    };
                })
                .ToList();
            progress.JumpTo(0.05f);

            bool TryMergeFile(ModFileIndex index, string relativePath, out string outFileName, bool incProgress = true)
            {
                outFileName = Path.Combine(outDir, relativePath);
                if (File.Exists(outFileName))
                    return false;

                if (index.LooseFiles.Contains(relativePath))
                {
                    progress.ItemName = $"[{index.ModName}] - Loose - {relativePath}";
                    Directory.CreateDirectory(Path.GetDirectoryName(outFileName));
                    File.Copy(Path.Combine(index.ModPath, relativePath), outFileName);
                    if (incProgress)
                        progress.CurrentProgress++;
                    return true;
                }

                var containingArchiveFile = index.ArchiveFiles
                    .Where(x => x.Value.Contains(relativePath))
                    .Select(x => x.Key)
                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(containingArchiveFile))
                {
                    var archiveShortName = Path.GetFileName(containingArchiveFile);
                    progress.ItemName = $"[{index.ModName}] - {archiveShortName} - {relativePath}";
                    Directory.CreateDirectory(Path.GetDirectoryName(outFileName));
                    Resources.ExtractFile(containingArchiveFile, relativePath, outFileName);
                    if (incProgress)
                        progress.CurrentProgress++;
                    return true;
                }

                return false;
            }

            progress.StartStage("Copying head part meshes and morphs");
            progress.AdjustRemaining(mergeInfo.Meshes.Count + mergeInfo.Morphs.Count, 0.2f); // 25% when done
            // Don't really want to mess with the actual merge info in case we need it later, but we do want to be able
            // to remove from the file sets as we go along, so we're not doing a gorillion redundant checks.
            // So, just make a copy of each set.
            var meshFileNames = new HashSet<string>(mergeInfo.Meshes);
            var morphFileNames = new HashSet<string>(mergeInfo.Morphs);
            var textureFileNames = new HashSet<string>(mergeInfo.Textures);
            foreach (var index in fileIndices)
            {
                progress.ItemName = $"[{index.ModName}]";
                IterateAndDrain(meshFileNames, meshFileName =>
                {
                    if (TryMergeFile(index, meshFileName, out var outFileName))
                    {
                        foreach (var texturePath in GetReferencedTexturePaths(outFileName))
                            textureFileNames.Add(texturePath.PrefixPath("textures"));
                        return true;
                    }
                    return false;
                });
                IterateAndDrain(morphFileNames, morphFileName => TryMergeFile(index, morphFileName, out var _));
            }
            progress.JumpTo(0.25f);

            progress.StartStage("Copying FaceGen data");
            progress.AdjustRemaining(npcs.Count, 0.5f); // 75% when done
            foreach (var npc in npcs)
            {
                progress.ItemName = $"'{npc.Name}' ({npc.BasePluginName} - {npc.EditorId})";
                var index = fileIndices.SingleOrDefault(x =>
                    string.Equals(x.ModName, npc.FaceModName, StringComparison.OrdinalIgnoreCase));
                if (index != null)
                {
                    var faceMeshFileName = FileStructure.GetFaceMeshFileName(npc.BasePluginName, npc.LocalFormIdHex);
                    if (TryMergeFile(index, faceMeshFileName, out var outFaceMeshFileName, false))
                    {
                        foreach (var texturePath in GetReferencedTexturePaths(outFaceMeshFileName))
                            textureFileNames.Add(texturePath.PrefixPath("textures"));
                    }

                    var faceTintFileName = FileStructure.GetFaceTintFileName(npc.BasePluginName, npc.LocalFormIdHex);
                    TryMergeFile(index, faceTintFileName, out var _, false);
                }
                progress.CurrentProgress++;
            }
            progress.JumpTo(0.75f);

            progress.StartStage("Copying textures");
            progress.AdjustRemaining(textureFileNames.Count, 0.2f); // 95% when done
            foreach (var index in fileIndices)
            {
                progress.ItemName = $"[{index.ModName}]";
                IterateAndDrain(textureFileNames, textureFileName => TryMergeFile(index, textureFileName, out var _));
            }
            progress.JumpTo(0.95f);

            progress.CurrentProgress = progress.MaxProgress;
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
    }

    class ModFileIndex
    {
        public string ModName { get; init; }
        public string ModPath { get; init; }
        public IReadOnlySet<string> LooseFiles { get; init; }
        public IDictionary<string, IReadOnlySet<string>> ArchiveFiles { get; init; }
    }
}