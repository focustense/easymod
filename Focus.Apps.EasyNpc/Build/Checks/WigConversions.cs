using Focus.Apps.EasyNpc.Profiles;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Build.Checks
{
    public class WigConversions : IBuildCheck
    {
        private readonly IWigResolver wigResolver;

        public WigConversions(IWigResolver wigResolver)
        {
            this.wigResolver = wigResolver;
        }

        public IEnumerable<BuildWarning> Run(Profile profile, BuildSettings settings)
        {
            var wigKeys = profile.Npcs
                .Select(x => x.FaceOption.Analysis.WigInfo)
                .NotNull()
                .Distinct();
            var matchedWigKeys = wigResolver.ResolveAll(wigKeys)
                .Where(x => x.HairKeys.Any())
                .Select(x => x.WigKey)
                .ToHashSet();
            return profile.Npcs
                .Select(x => new { Npc = x, Wig = x.FaceOption.Analysis.WigInfo })
                .Where(x => x.Wig is not null && (!settings.EnableDewiggify || !matchedWigKeys.Contains(x.Wig.Key)))
                .Select(x => settings.EnableDewiggify ?
                    new BuildWarning(
                        new RecordKey(x.Npc),
                        x.Wig.IsBald ? BuildWarningId.FaceModWigNotMatchedBald : BuildWarningId.FaceModWigNotMatched,
                        x.Wig.IsBald ?
                            WarningMessages.FaceModWigNotMatchedBald(
                                x.Npc.EditorId, x.Npc.Name, x.Npc.FaceOption.PluginName, x.Wig.ModelName) :
                            WarningMessages.FaceModWigNotMatched(
                                x.Npc.EditorId, x.Npc.Name, x.Npc.FaceOption.PluginName, x.Wig.ModelName)
                        ) :
                        new BuildWarning(
                            BuildWarningId.FaceModWigConversionDisabled,
                            WarningMessages.FaceModWigConversionDisabled(
                                x.Npc.EditorId, x.Npc.Name, x.Npc.FaceOption.PluginName, x.Wig.IsBald)));
        }
    }
}
