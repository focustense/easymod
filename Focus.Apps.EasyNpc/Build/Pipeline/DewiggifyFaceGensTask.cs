using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public class DewiggifyFaceGensTask : BuildTask<DewiggifyFaceGensTask.Result>
    {
        public class Result
        {
            public IReadOnlyList<string> FailedPaths { get; private init; }
            public bool Skipped { get; private init; }

            public Result(bool skipped)
                : this(new List<string>().AsReadOnly())
            {
                Skipped = skipped;
            }

            public Result(IReadOnlyList<string> failedPaths)
            {
                FailedPaths = failedPaths;
            }
        }

        public delegate DewiggifyFaceGensTask Factory(
            FaceGenCopyTask.Result faceGen, DewiggifyRecordsTask.Result dewiggifyRecords);

        private readonly DewiggifyRecordsTask.Result dewiggifyRecords;
        private readonly FaceGenCopyTask.Result faceGen;
        private readonly IFaceGenEditor faceGenEditor;

        public DewiggifyFaceGensTask(
            IFaceGenEditor faceGenEditor, FaceGenCopyTask.Result faceGen, DewiggifyRecordsTask.Result dewiggifyRecords)
        {
            this.faceGenEditor = faceGenEditor;
            this.faceGen = faceGen;
            this.dewiggifyRecords = dewiggifyRecords;
        }

        protected override async Task<Result> Run(BuildSettings settings)
        {
            if (!settings.EnableDewiggify)
                return new Result(true);
            var itemsToReplace = faceGen.MeshPaths
                .Join(
                    dewiggifyRecords.WigConversions,
                    path => path,
                    rec => FileStructure.GetFaceMeshFileName(rec.Key),
                    (path, rec) => new
                    {
                        AbsolutePath = Path.Combine(settings.OutputDirectory, path),
                        RelativePath = path,
                        rec.AddedHeadParts,
                        rec.RemovedHeadParts,
                        rec.HairColor,
                    },
                    PathComparer.Default)
                .ToList();
            ItemCount.OnNext(itemsToReplace.Count);
            var replaceTasks = itemsToReplace
                .Select(x => Task.Run(async () =>
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    NextItem(x.RelativePath);
                    try
                    {
                        var success = await faceGenEditor.ReplaceHeadParts(
                            x.AbsolutePath, x.RemovedHeadParts, x.AddedHeadParts, x.HairColor);
                        return new { x.RelativePath, Success = success };
                    }
                    catch
                    {
                        return new { x.RelativePath, Success = false };
                    }
                }));
            var results = await Task.WhenAll(replaceTasks);
            var failedPaths = results.Where(x => !x.Success).Select(x => x.RelativePath);
            return new Result(failedPaths.ToList().AsReadOnly());
        }
    }
}
