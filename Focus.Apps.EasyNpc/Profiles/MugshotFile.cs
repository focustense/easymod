#nullable enable

namespace Focus.Apps.EasyNpc.Profiles
{
    public class MugshotFile
    {
        public bool IsPlaceholder => string.IsNullOrEmpty(TargetModId) && string.IsNullOrEmpty(TargetModName);
        public string Path { get; private init; }
        public string TargetModId { get; private init; }
        public string TargetModName { get; private init; }

        public MugshotFile(string path)
            : this(string.Empty, string.Empty, path) { }

        public MugshotFile(string modId, string modName, string path)
        {
            TargetModId = modId;
            TargetModName = modName;
            Path = path;
        }
    }
}
