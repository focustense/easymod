namespace Focus.Apps.EasyNpc.Build
{
    public class BuildSettings
    {
        public bool EnableDewiggify { get; init; }
        public string OutputDirectory { get; init; }
        public string OutputModName { get; init; }
        public Profiles.Profile Profile { get; init; }
    }
}