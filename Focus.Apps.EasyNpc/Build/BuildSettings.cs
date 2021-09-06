using Focus.Apps.EasyNpc.Profiles;

namespace Focus.Apps.EasyNpc.Build
{
    public class BuildSettings
    {
        public bool EnableDewiggify { get; init; } = true;
        public string OutputDirectory { get; init; }
        public string OutputModName { get; init; }
        public Profile Profile { get; init; }

        public BuildSettings(Profile profile, string outputDirectory, string outputModName)
        {
            Profile = profile;
            OutputDirectory = outputDirectory;
            OutputModName = outputModName;
        }
    }
}