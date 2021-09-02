using System.Collections.Generic;
using System.Linq;

namespace Focus.Analysis.Records
{
    public abstract class RecordAnalysis : IRecordKey
    {
        public string BasePluginName { get; init; } = string.Empty;
        public string EditorId { get; init; } = string.Empty;
        public bool Exists { get; init; } = false;
        public IReadOnlyList<ReferencePath> InvalidPaths { get; init; } = Empty.ReadOnlyList<ReferencePath>();
        public bool IsInjectedOrInvalid { get; init; }
        public bool IsOverride { get; init; }
        public string LocalFormIdHex { get; init; } = string.Empty;
        public abstract RecordType Type { get; }
    }

    public class ReferenceInfo
    {
        public string? EditorId { get; init; }
        public IRecordKey Key { get; init; } = RecordKey.Null;
        public RecordType Type { get; init; } = 0;

        public ReferenceInfo()
        {
        }

        public ReferenceInfo(IRecordKey key, RecordType type)
        {
            Key = key;
            Type = type;
        }

        public ReferenceInfo(IRecordKey key, RecordType type, string editorId)
            : this(key, type)
        {
            EditorId = editorId;
        }

        public override string ToString()
        {
            return $"{Type} <{Key.LocalFormIdHex}:{Key.BasePluginName}> '{EditorId}'";
        }
    }

    public class ReferencePath
    {
        public IReadOnlyList<ReferenceInfo> References { get; init; } = Empty.ReadOnlyList<ReferenceInfo>();

        public ReferencePath() { }

        public ReferencePath(IEnumerable<ReferenceInfo> refs)
        {
            References = refs.ToList().AsReadOnly();
        }

        public override string ToString()
        {
            return string.Join(" -> ", References.Select(x => $"[{x}]"));
        }
    }
}
