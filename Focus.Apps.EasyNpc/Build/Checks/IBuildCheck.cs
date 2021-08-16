using Focus.Apps.EasyNpc.Profiles;
using System.Collections.Generic;

namespace Focus.Apps.EasyNpc.Build.Checks
{
    public interface IBuildCheck
    {
        IEnumerable<BuildWarning> Run(Profile profile, BuildSettings settings);
    }
}
