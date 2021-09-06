using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Skyrim;
using System;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public class PatchSaveTask : BuildTask<PatchSaveTask.Result>
    {
        public class Result
        {
            public ISkyrimModGetter Mod { get; private init; }
            public string Path { get; private init; }

            public Result(ISkyrimModGetter mod, string path)
            {
                Mod = mod;
                Path = path;
            }
        }

        public delegate PatchSaveTask Factory(
            PatchInitializationTask.Result patch, NpcFacesTask.Result faces, DewiggifyRecordsTask.Result wigs);

        private readonly IFileSystem fs;
        private readonly IGameSettings gameSettings;
        private readonly PatchInitializationTask.Result patch;

        public PatchSaveTask(
            IFileSystem fs, IGameSettings gameSettings, PatchInitializationTask.Result patch, NpcFacesTask.Result faces,
            DewiggifyRecordsTask.Result wigs)
        {
            RunsAfter(faces, wigs);
            this.fs = fs;
            this.gameSettings = gameSettings;
            this.patch = patch;
        }

        protected override Task<Result> Run(BuildSettings settings)
        {
            return Task.Run(() =>
            {
                fs.Directory.CreateDirectory(settings.OutputDirectory);
                var outputPath = fs.Path.Combine(settings.OutputDirectory, patch.Mod.ModKey.FileName);
                BackupPreviousMerge(outputPath);
                SaveMod(patch.Mod, outputPath);
                return new Result(patch.Mod, outputPath);
            });
        }

        private void BackupPreviousMerge(string mergeFilePath)
        {
            if (fs.File.Exists(mergeFilePath))
            {
                var backupPath = $"{mergeFilePath}.{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                fs.File.Move(mergeFilePath, backupPath, true);
            }
        }

        private void SaveMod(SkyrimMod mod, string outputPath)
        {
            var loadOrder = gameSettings.PluginLoadOrder.Select(x => ModKey.FromNameAndExtension(x));
            using var stream = fs.File.Create(outputPath);
            mod.WriteToBinaryParallel(stream, new BinaryWriteParameters
            {
                MastersListOrdering = new MastersListOrderingByLoadOrder(loadOrder)
            });
        }
    }
}
