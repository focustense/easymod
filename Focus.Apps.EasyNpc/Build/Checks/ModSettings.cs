using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Profiles;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace Focus.Apps.EasyNpc.Build.Checks
{
    public class ModSettings : IBuildCheck
    {
        private readonly IAppSettings appSettings;
        private readonly IFileSystem fs;

        public ModSettings(IAppSettings appSettings, IFileSystem fs)
        {
            this.appSettings = appSettings;
            this.fs = fs;
        }

        public IEnumerable<BuildWarning> Run(Profile profile, BuildSettings settings)
        {
            if (string.IsNullOrWhiteSpace(appSettings.ModRootDirectory))
                yield return new BuildWarning(
                    BuildWarningId.ModDirectoryNotSpecified,
                    WarningMessages.ModDirectoryNotSpecified());
            else if (!fs.Directory.Exists(appSettings.ModRootDirectory))
                yield return new BuildWarning(
                    BuildWarningId.ModDirectoryNotFound,
                    WarningMessages.ModDirectoryNotFound(appSettings.ModRootDirectory));
        }
    }
}
