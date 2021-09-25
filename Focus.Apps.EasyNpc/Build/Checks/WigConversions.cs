using Focus.Apps.EasyNpc.Profiles;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Build.Checks
{
    public class WigConversions : IPreparableNpcBuildCheck
    {
        private HashSet<IRecordKey> resolvedWigKeys = new();
        private readonly IWigResolver wigResolver;

        public WigConversions(IWigResolver wigResolver)
        {
            this.wigResolver = wigResolver;
        }

        public void Prepare(Profile profile)
        {
            var wigKeys = profile.Npcs
                .Where(x => x.CanCustomizeFace)
                .SelectMany(x => x.Options)
                .Select(x => x.Analysis.WigInfo)
                .NotNull()
                .Distinct();
            resolvedWigKeys = wigResolver.ResolveAll(wigKeys)
                .Where(x => x.HairKeys.Any())
                .Select(x => x.WigKey)
                .ToHashSet(RecordKeyComparer.Default);
        }

        public IEnumerable<BuildWarning> Run(Npc npc, BuildSettings settings)
        {
            if (!settings.EnableDewiggify || !npc.CanCustomizeFace)
                yield break;
            var wig = npc.FaceOption.Analysis.WigInfo;
            if (wig is null)
                yield break;
            if (!resolvedWigKeys.Contains(wig.Key))
                yield return new BuildWarning(
                    new RecordKey(npc),
                    BuildWarningId.WigNotMatched,
                    WarningMessages.WigNotMatched(
                        npc.EditorId, npc.Name, npc.FaceOption.PluginName, wig.ModelName ?? string.Empty));
        }
    }
}
