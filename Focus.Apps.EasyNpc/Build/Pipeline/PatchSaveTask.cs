using Mutagen.Bethesda.Skyrim;
using System;
using System.IO.Abstractions;
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

        public override string Name => "Save Patch";

        private readonly IFileSystem fs;
        private readonly PatchInitializationTask.Result patch;

        public PatchSaveTask(
            IFileSystem fs, PatchInitializationTask.Result patch, NpcFacesTask.Result faces,
            DewiggifyRecordsTask.Result wigs)
        {
            RunsAfter(faces, wigs);
            this.fs = fs;
            this.patch = patch;
        }

        protected override Task<Result> Run(BuildSettings settings)
        {
            return Task.Run(() =>
            {
                var outputPath =
                fs.Path.Combine(settings.OutputDirectory, patch.Mod.ModKey.FileName);
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
            using var stream = fs.File.Create(outputPath);
            mod.WriteToBinaryParallel(stream);
        }
    }
}
