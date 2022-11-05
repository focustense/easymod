using Focus.Providers.Mutagen;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serilog;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public class DewiggifyRecordsTask : BuildTask<DewiggifyRecordsTask.Result>
    {
        public class Result
        {
            public IReadOnlyList<NpcWigConversion> WigConversions { get; private init; }

            public Result(IReadOnlyList<NpcWigConversion> wigConversions)
            {
                WigConversions = wigConversions;
            }
        }

        public delegate DewiggifyRecordsTask Factory(
            PatchInitializationTask.Result patch, NpcDefaultsTask.Result defaults, NpcFacesTask.Result faces);

        private readonly NpcDefaultsTask.Result defaults;
        private readonly IReadOnlyGameEnvironment<ISkyrimModGetter> env;


        private readonly ILogger log;
        private readonly PatchInitializationTask.Result patch;
        private readonly IWigResolver wigResolver;

        public DewiggifyRecordsTask(
            IReadOnlyGameEnvironment<ISkyrimModGetter> env, IWigResolver wigResolver, ILogger log,
            PatchInitializationTask.Result patch, NpcDefaultsTask.Result defaults, NpcFacesTask.Result faces)
        {
            RunsAfter(faces);
            this.defaults = defaults;
            this.env = env;
            this.log = log;
            this.patch = patch;
            this.wigResolver = wigResolver;
        }

        protected override Task<Result> Run(BuildSettings settings)
        {
            if (!settings.EnableDewiggify)
                return Task.FromResult(new Result(new List<NpcWigConversion>().AsReadOnly()));
            return Task.Run(() =>
            {
                var wigs = defaults.Npcs
                    .Select(x => x.Model.FaceOption.Analysis.WigInfo)
                    .NotNull();
                var wigMatches = wigResolver.ResolveAll(wigs)
                    .Where(x => x.HairKeys.Count > 0)
                    .ToDictionary(x => x.WigKey, x => x.HairKeys);
                var npcsWithMatchedWigs = defaults.Npcs
                    .Where(x =>
                        x.Model.FaceOption.Analysis.WigInfo is not null &&
                        wigMatches.ContainsKey(x.Model.FaceOption.Analysis.WigInfo.Key))
                    .ToList();
                ItemCount.OnNext(npcsWithMatchedWigs.Count);
                var wigConversions = new List<NpcWigConversion>();
                foreach (var (model, record) in npcsWithMatchedWigs)
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    NextItem(model.DescriptiveLabel);
                    var hairKey = wigMatches[model.FaceOption.Analysis.WigInfo!.Key][0];
                    var oldHairParts = record.HeadParts
                        .Select(x => patch.Importer.TryResolve<IHeadPartGetter>(x.FormKey))
                        .NotNull()
                        .Where(x => x.Type == HeadPart.TypeEnum.Hair)
                        .Select(x => new { x.FormKey, x.EditorID, x.Model?.File })
                        .ToList();
                    record.HeadParts.Remove(oldHairParts.Select(x => x.FormKey));
                    var mergedHair =
                        patch.Importer.Import(hairKey.ToFormKey().AsLink<IHeadPartGetter>(), x => x.HeadParts);
                    if (mergedHair.HasValue)
                        record.HeadParts.Add(mergedHair.Value);
                    wigConversions.Add(new NpcWigConversion
                    {
                        Key = new RecordKey(model),
                        HairColor = GetHairColor(record),
                        AddedHeadParts = mergedHair.HasValue ?
                            DescribeHeadParts(mergedHair.Value, patch.Importer).ToList().AsReadOnly() :
                            new List<HeadPartInfo>().AsReadOnly(),
                        RemovedHeadParts = oldHairParts
                            .SelectMany(x => DescribeHeadParts(x.FormKey, patch.Importer))
                            .ToList()
                            .AsReadOnly(),
                    });
                    record.WornArmor.Clear();
                }
                return new Result(wigConversions);
            });
        }

        private IEnumerable<HeadPartInfo> DescribeHeadParts(FormKey formKey, RecordImporter importer)
        {
            var headPart = importer.TryResolve<IHeadPartGetter>(formKey);
            if (headPart == null)
                return Enumerable.Empty<HeadPartInfo>();
            return headPart.ExtraParts
                .SelectMany(x => DescribeHeadParts(x.FormKey, importer))
                .Prepend(new HeadPartInfo
                {
                    EditorId = headPart.EditorID,
                    FileName = headPart.Model?.File?.PrefixPath("meshes")
                });
        }

        private Color? GetHairColor(INpcGetter npc)
        {
            if (npc.HairColor.IsNull)
                return null;
            var colorRecord =
                npc.HairColor.TryResolve(env.LinkCache, npc, log, "This NPC will have the wrong hair color.");
            return colorRecord?.Color;
        }
    }
}
