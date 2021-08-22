using System.Collections.Generic;
using System.Drawing;

namespace Focus.Apps.EasyNpc.Build
{
    public class HeadPartInfo
    {
        public string? EditorId { get; init; }
        public string? FileName { get; init; }
    }

    public class NpcWigConversion
    {
        public IRecordKey Key { get; init; } = RecordKey.Null;
        public Color? HairColor { get; init; }
        public IReadOnlyList<HeadPartInfo> AddedHeadParts { get; init; } = new List<HeadPartInfo>().AsReadOnly();
        public IReadOnlyList<HeadPartInfo> RemovedHeadParts { get; init; } = new List<HeadPartInfo>().AsReadOnly();
    }
}
