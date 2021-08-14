using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Apps.EasyNpc.Profile;
using Focus.Apps.EasyNpc.Profiles;
using Focus.Files;
using Focus.ModManagers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace Focus.Apps.EasyNpc.Build.Checks
{
    public interface IBuildCheck
    {
        IEnumerable<BuildWarning> Run(Profiles.Profile profile, BuildSettings settings);
    }
}
