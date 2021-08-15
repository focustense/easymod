using Focus.Apps.EasyNpc.GameData.Files;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public class TextureCopyTask : BuildTask<TextureCopyTask.Result>
    {
        public class Result
        {
            public IReadOnlyCollection<string> TexturePaths { get; private init; }

            public Result(IReadOnlyCollection<string> texturePaths)
            {
                TexturePaths = texturePaths;
            }
        }

        public delegate TextureCopyTask Factory(TexturePathExtractionTask.Result extracted);

        public override string Name => "Copy Textures";

        private readonly IFileCopier copier;
        private readonly TexturePathExtractionTask.Result extracted;

        public TextureCopyTask(IFileCopier copier, TexturePathExtractionTask.Result extracted)
        {
            this.copier = copier;
            this.extracted = extracted;
        }

        protected override Task<Result> Run(BuildSettings settings)
        {
            return Task.Run(() =>
            {
                var texturePaths = extracted.TexturePaths
                    .Where(p => !FileStructure.IsFaceGen(p))
                    .ToHashSet();
                ItemCount.OnNext(texturePaths.Count);
                copier.CopyAll(texturePaths, settings.OutputDirectory, NextItemSync, CancellationToken);
                return new Result(texturePaths);
            });
        }
    }
}
