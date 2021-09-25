using Focus.Apps.EasyNpc.Profiles;
using System.Collections.Generic;

namespace Focus.Apps.EasyNpc.Build.Checks
{
    public interface IGlobalBuildCheck
    {
        IEnumerable<BuildWarning> Run(Profile profile, BuildSettings settings);
    }

    public interface INpcBuildCheck
    {
        IEnumerable<BuildWarning> Run(Npc npc, BuildSettings settings);
    }

    public interface IPreparableNpcBuildCheck : INpcBuildCheck
    {
        void Prepare(Profile profile);
    }
}
