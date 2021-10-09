using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Profiles;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace Focus.Apps.EasyNpc.Build.Checks
{
    public class ModSettings : IGlobalBuildCheck
    {
        private readonly IFileSystem fs;
        private readonly IModSettings modSettings;

        public ModSettings(IModSettings modSettings, IFileSystem fs)
        {
            this.fs = fs;
            this.modSettings = modSettings;
        }

        public IEnumerable<BuildWarning> Run(Profile profile, BuildSettings settings)
        {
            if (string.IsNullOrWhiteSpace(modSettings.RootDirectory))
                yield return new BuildWarning(
                    BuildWarningId.ModDirectoryNotSpecified,
                    WarningMessages.ModDirectoryNotSpecified());
            else if (!fs.Directory.Exists(modSettings.RootDirectory))
                yield return new BuildWarning(
                    BuildWarningId.ModDirectoryNotFound,
                    WarningMessages.ModDirectoryNotFound(modSettings.RootDirectory));
        }
    }
}
