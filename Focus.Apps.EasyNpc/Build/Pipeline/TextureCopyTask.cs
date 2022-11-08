using Focus.Apps.EasyNpc.GameData.Files;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public class TextureCopyTask : BuildTask<TextureCopyTask.Result>
    {
        public class Result
        {
            public IReadOnlyCollection<string> FailedPaths { get; private init; }
            public IReadOnlyCollection<string> TexturePaths { get; private init; }

            public Result(
                IReadOnlyCollection<string> texturePaths, IReadOnlyCollection<string> failedPaths)
            {
                TexturePaths = texturePaths;
                FailedPaths = failedPaths;
            }
        }

        public delegate TextureCopyTask Factory(TexturePathExtractionTask.Result extracted);

        private readonly IFileCopier copier;
        private readonly TexturePathExtractionTask.Result extracted;
        private readonly IEnumerable<ITexturePathFilter> filters;
        private readonly ILogger log;

        public TextureCopyTask(
            IFileCopier copier, IEnumerable<ITexturePathFilter> filters, ILogger log,
            TexturePathExtractionTask.Result extracted)
        {
            this.copier = copier;
            this.extracted = extracted;
            this.filters = filters;
            this.log = log;
        }

        protected override Task<Result> Run(BuildSettings settings)
        {
            return Task.Run(() =>
            {
                var texturePaths = extracted.TexturePaths
                    .Where(p => !FileStructure.IsFaceGen(p))
                    .Where(p =>
                    {
                        var firstExcludingFilter = filters.FirstOrDefault(f => !f.ShouldImport(p));
                        if (firstExcludingFilter is not null)
                        {
                            log.Debug(
                                "Texture '{texturePath}' is excluded by filter {filterType}",
                                p, firstExcludingFilter.GetType().Name);
                            return false;
                        }
                        return true;
                    })
                    .ToHashSet();
                ItemCount.OnNext(texturePaths.Count);
                copier.CopyAll(
                    texturePaths, settings.OutputDirectory, NextItemSync, out var failedPaths,
                    CancellationToken);
                return new Result(texturePaths, failedPaths);
            });
        }
    }
}
