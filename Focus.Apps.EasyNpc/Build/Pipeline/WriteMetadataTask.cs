using Focus.Apps.EasyNpc.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public class WriteMetadataTask : BuildTask<WriteMetadataTask.Result>
    {
        private static readonly string MetadataFileName = "build_info.json";

        public class Result { }

        public delegate WriteMetadataTask Factory(
            FaceGenCopyTask.Result faceGen, TexturePathExtractionTask.Result textureExtraction,
            TextureCopyTask.Result textures, SharedResourceCopyTask.Result sharedResources);

        private readonly FaceGenCopyTask.Result faceGen;
        private readonly IFileSystem fs;
        private readonly SharedResourceCopyTask.Result sharedResources;
        private readonly TexturePathExtractionTask.Result textureExtraction;
        private readonly TextureCopyTask.Result textures;

        public WriteMetadataTask(
            IFileSystem fs, FaceGenCopyTask.Result faceGen,
            TexturePathExtractionTask.Result textureExtraction, TextureCopyTask.Result textures,
            SharedResourceCopyTask.Result sharedResources)
        {
            this.fs = fs;
            this.faceGen = faceGen;
            this.sharedResources = sharedResources;
            this.textureExtraction = textureExtraction;
            this.textures = textures;
        }

        protected override async Task<Result> Run(BuildSettings settings)
        {
            var metadata = new Metadata
            {
                FailedTexturePathExtractionSources = textureExtraction.FailedSourcePaths.ToList(),
                MissingAssetPaths = faceGen.FailedPaths
                    .Concat(sharedResources.FailedPaths)
                    .Concat(textures.FailedPaths)
                    // Some tasks might add paths relative to the mod output directory, which tends
                    // to be unhelpful, so strip that.
                    // If an absolute path refers to a source outside the output directory, then we
                    // want to keep it.
                    .Select(path => path.StartsWith(
                        settings.OutputDirectory, StringComparison.CurrentCultureIgnoreCase)
                        ? fs.Path.GetRelativePath(settings.OutputDirectory, path)
                        : path)
                    .Select(path => !fs.Path.IsPathRooted(path) ? path : path.Replace(@"\", "/"))
                    .OrderBy(path => path, StringComparer.CurrentCultureIgnoreCase)
                    .ToList()
            };
            var outputJson = JsonConvert.SerializeObject(metadata, Formatting.Indented);
            var metadataPath = fs.Path.Combine(settings.OutputDirectory, MetadataFileName);
            await fs.File.WriteAllTextAsync(metadataPath, outputJson, CancellationToken);
            return new();
        }

        // Private for now, though in the future there may be reasons to pull this out, e.g.
        // incremental builds, restoring previous build settings, etc.
        class Metadata
        {
            public string AppVersion { get; set; } = AssemblyProperties.Version.ToString();
            public List<string> FailedTexturePathExtractionSources { get; set; } = new();
            public List<string> MissingAssetPaths { get; set; } = new();
        }
    }
}
