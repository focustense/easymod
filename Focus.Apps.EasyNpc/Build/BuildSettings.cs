using Focus.Apps.EasyNpc.Profiles;

namespace Focus.Apps.EasyNpc.Build
{
    public class BuildSettings
    {
        public bool EnableArchiving { get; init; } = true;
        public bool EnableDewiggify { get; init; }
        public string OutputDirectory { get; init; }
        public string OutputModName { get; init; }
        public Profile Profile { get; init; }
        public int TextureExtractionTimeoutSec { get; init; } = 30;

        public BuildSettings(Profile profile, string outputDirectory, string outputModName)
        {
            Profile = profile;
            OutputDirectory = outputDirectory;
            OutputModName = outputModName;
        }
    }
}