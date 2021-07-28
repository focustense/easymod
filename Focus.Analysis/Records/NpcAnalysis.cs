using System.Collections.Generic;

namespace Focus.Analysis.Records
{
    public class NpcAnalysis : RecordAnalysis, IResourceDependencies
    {
        public override RecordType Type => RecordType.Npc;

        public bool CanUseFaceGen { get; init; }
        public NpcComparison? ComparisonToMaster { get; init; }
        public NpcComparison? ComparisonToPreviousOverride { get; init; }
        public IReadOnlyList<RecordKey> HeadParts { get; init; } = Empty.ReadOnlyList<RecordKey>();
        public bool IsChild { get; init; }
        public bool IsFemale { get; init; }
        public bool ModifiesBehavior { get; init; }
        public IReadOnlyList<string> UsedMeshes { get; init; } = Empty.ReadOnlyList<string>();
        public IReadOnlyList<string> UsedTextures { get; init; } = Empty.ReadOnlyList<string>();
        public NpcWigInfo? WigInfo { get; init; }
    }

    public record NpcWigInfo(RecordKey Key, string? ModelName, bool IsBald);
}