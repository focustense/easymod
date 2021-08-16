using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Storage.Archives;
using System;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public class ArchiveCreationTask : BuildTask<ArchiveCreationTask.Result>
    {
        public class Result { }

        public delegate ArchiveCreationTask Factory(
            PatchSaveTask.Result patch, FaceGenCopyTask.Result faceGens, HeadPartResourceCopyTask.Result headParts,
            TextureCopyTask.Result textures);

        private const long GB = 1024 * 1024 * 1024;

        private readonly IDummyPluginBuilder dummyPluginBuilder;
        private readonly FaceGenCopyTask.Result faceGens;
        private readonly IFileSystem fs;
        private readonly HeadPartResourceCopyTask.Result headParts;
        private readonly PatchSaveTask.Result patch;
        private readonly TextureCopyTask.Result textures;

        public ArchiveCreationTask(
            IFileSystem fs, IDummyPluginBuilder dummyPluginBuilder, PatchSaveTask.Result patch,
            FaceGenCopyTask.Result faceGens, HeadPartResourceCopyTask.Result headParts, TextureCopyTask.Result textures)
        {
            this.dummyPluginBuilder = dummyPluginBuilder;
            this.faceGens = faceGens;
            this.fs = fs;
            this.headParts = headParts;
            this.patch = patch;
            this.textures = textures;
        }

        protected override async Task<Result> Run(BuildSettings settings)
        {
            // FaceGen files are usually much larger and therefore more expensive than other meshes/textures.
            // Applying extra weight to these gives a somewhat more realistic ETA.
            const int FaceGenWeightMultiplier = 3;
            const int MeshProgressWeight = 1;
            // Textures are considerably more expensive to add due to the the type of compression, so applying a higher
            // weight to them avoids "rushing" the progress while the meshes are running in parallel. We'll still get
            // some rushing due to .NET's Parallel implementation that creates a large backlog.
            const int TextureProgressWeight = 3;

            // Meshes tend to compress at around 50%, so 3 GB should be plenty of headroom to stay under 2 GB.
            const long MaxMeshesUncompressedSize = 3 * GB;

            // Textures can compress to 10% or less of their original size, but we shouldn't always assume best-case.
            // A 15 GB limit gives us an expected max compressed size of 1.5 GB, which is the same margin of error as
            // the mesh settings.
            const long MaxTexturesUncompressedSize = 15 * GB;

            var meshProgressSize = MeshProgressWeight *
                (faceGens.MeshPaths.Count * FaceGenWeightMultiplier +
                headParts.MeshPaths.Count + headParts.MorphPaths.Count);
            var textureProgressSize = TextureProgressWeight *
                (faceGens.TintPaths.Count * FaceGenWeightMultiplier + textures.TexturePaths.Count);
            // The "+5" below adds some headroom for follow-up tasks - dummy plugins and file cleanup.
            ItemCount.OnNext(meshProgressSize + textureProgressSize + 5);

            var baseName = fs.Path.GetFileNameWithoutExtension(patch.Path);
            var meshesTask = Task.Run(() => BuildFilteredArchive(settings.OutputDirectory, new()
            {
                Name = baseName,
                RelativePath = "meshes",
                DefaultProgressWeight = MeshProgressWeight,
                FaceGenProgressWeight = MeshProgressWeight * FaceGenWeightMultiplier,
                EstimatedDefaultCompressionRatio = 0.7,
                EstimatedFaceGenCompressionRatio = 0.5,
                MaxUncompressedSize = MaxMeshesUncompressedSize,
            }));
            var texturesTask = Task.Run(() => BuildFilteredArchive(settings.OutputDirectory, new()
            {
                Name = $"{baseName} - Textures",
                RelativePath = "textures",
                DefaultProgressWeight = TextureProgressWeight,
                FaceGenProgressWeight = TextureProgressWeight * FaceGenWeightMultiplier,
                EstimatedDefaultCompressionRatio = 0.5,
                EstimatedFaceGenCompressionRatio = 0.08,
                MaxUncompressedSize = MaxTexturesUncompressedSize,
            }));
            await Task.WhenAll(meshesTask, texturesTask);

            ItemName.OnNext("Creating dummy plugins");
            var archiveFileNames = meshesTask.Result.ArchiveResults
                .Concat(texturesTask.Result.ArchiveResults)
                .Select(x => x.FileName);
            foreach (var archiveFileName in archiveFileNames)
            {
                var archiveBaseName = fs.Path.GetFileNameWithoutExtension(archiveFileName);
                // Neither the default archive (with same name as merge) nor the standard textures archive need
                // dummy plugins; the game recognizes these automatically.
                if (archiveBaseName != baseName && archiveBaseName != $"{baseName} - Textures")
                    dummyPluginBuilder.CreateDummyPlugin(fs.Path.ChangeExtension(archiveFileName, ".esp"));
            }

            ItemName.OnNext("Cleaning up loose files");
            fs.Directory.Delete(fs.Path.Combine(settings.OutputDirectory, "meshes"), true);
            fs.Directory.Delete(fs.Path.Combine(settings.OutputDirectory, "textures"), true);

            return new Result();
        }

        private ArchiveBuilder.BuildResult BuildFilteredArchive(string outputDirectory, ArchiveSettings settings)
        {
            var outputFileName = fs.Path.Combine(outputDirectory, settings.Name) + ".bsa";
            return new ArchiveBuilder(ArchiveType.SSE)
                .AddDirectory(fs.Path.Combine(outputDirectory, settings.RelativePath), settings.RelativePath)
                .Compress(true)
                .ShareData(true)
                .MaxCompressedSize((long)(1.8 * GB /* leave headroom */), x =>
                {
                    var ratio = FileStructure.IsFaceGen(x.PathInArchive) ?
                        settings.EstimatedFaceGenCompressionRatio : settings.EstimatedDefaultCompressionRatio;
                    return (int)(x.Size * ratio);
                })
                .MaxUncompressedSize(settings.MaxUncompressedSize)
                .OnBeforeBuild(entries => CancellationToken.ThrowIfCancellationRequested())
                .OnPacking(entry =>
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    NextItemSync($"[{settings.Name}.bsa] <- {entry.PathInArchive}", 0);
                })
                .OnPacked(entry => NextItemSync(
                    $"[{settings.Name}.bsa] <- {entry.PathInArchive}",
                    FileStructure.IsFaceGen(entry.PathInArchive) ?
                        settings.FaceGenProgressWeight : settings.DefaultProgressWeight))
                .Build(outputFileName);
        }

        class ArchiveSettings
        {
            public int DefaultProgressWeight { get; init; }
            public double EstimatedDefaultCompressionRatio { get; init; }
            public double EstimatedFaceGenCompressionRatio { get; init; }
            public int FaceGenProgressWeight { get; init; }
            public long MaxUncompressedSize { get; init; }
            public string Name { get; init; } = string.Empty;
            public string RelativePath { get; init; } = string.Empty;
        }
    }
}
