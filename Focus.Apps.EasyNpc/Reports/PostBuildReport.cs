using Focus.Apps.EasyNpc.GameData.Files;
using Focus.ModManagers;
using System.Collections.Generic;

namespace Focus.Apps.EasyNpc.Reports
{
    public enum PluginState { Missing, Enabled, Disabled, Unloadable }

    public class MergeArchiveInfo
    {
        public string? ArchiveName { get; init; }
        public PluginState ArchiveState => RequiresDummyPlugin ? DummyPluginState : PluginState.Enabled;
        public string? DummyPluginName { get; init; }
        public PluginState DummyPluginState { get; init; }
        public bool HasMissingArchive => string.IsNullOrEmpty(ArchiveName);
        public bool HasPluginError => RequiresDummyPlugin &&
            (string.IsNullOrEmpty(DummyPluginName) || DummyPluginState != PluginState.Enabled);
        public bool IsReadable { get; init; }
        public bool RequiresDummyPlugin { get; init; }
    }

    public class NpcConsistencyInfo : IRecordKey
    {
        public string BasePluginName { get; init; } = string.Empty;
        public string EditorId { get; init; } = string.Empty;
        public bool HasConsistentFaceTint { get; init; }
        public bool HasConsistentHeadParts { get; init; }
        public string LocalFormIdHex { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public ModComponentInfo? WinningFaceGenComponent { get; init; }
        public ModComponentInfo? WinningFaceTintComponent { get; init; }
        public string WinningPluginName { get; init; } = string.Empty;
    }

    public class PostBuildReport
    {
        public IReadOnlyList<ModComponentInfo> ActiveMergeComponents { get; init; } =
            new List<ModComponentInfo>().AsReadOnly();
        public IReadOnlyList<MergeArchiveInfo> Archives { get; init; } = new List<MergeArchiveInfo>().AsReadOnly();
        public IReadOnlyList<string> MainPluginMissingMasters { get; init; } = new List<string>().AsReadOnly();
        public string MainPluginName { get; init; } = FileStructure.MergeFileName;
        public PluginState MainPluginState { get; init; }
        public IReadOnlyList<NpcConsistencyInfo> Npcs { get; init; } = new List<NpcConsistencyInfo>().AsReadOnly();
    }
}
