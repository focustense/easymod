﻿using System.Collections.Generic;

namespace Focus.Analysis.Records
{
    public class NpcAnalysis : RecordAnalysis, IResourceDependencies
    {
        public override RecordType Type => RecordType.Npc;

        public bool CanUseFaceGen { get; init; }
        public NpcComparison? ComparisonToBase { get; init; }
        public IReadOnlyList<NpcComparison> ComparisonToMasters { get; init; } = Empty.ReadOnlyList<NpcComparison>();
        public NpcComparison? ComparisonToPreviousOverride { get; init; }
        public IReadOnlyList<RecordKey> MainHeadParts { get; init; } = Empty.ReadOnlyList<RecordKey>();
        public bool IsChild { get; init; }
        public bool IsFemale { get; init; }
        public string Name { get; init; } = string.Empty;
        public RecordKey? SkinKey { get; init; }
        public IReadOnlyList<string> UsedMeshes { get; init; } = Empty.ReadOnlyList<string>();
        public IReadOnlyList<string> UsedTextures { get; init; } = Empty.ReadOnlyList<string>();
        public NpcWigInfo? WigInfo { get; init; }
    }

    public record NpcWigInfo(RecordKey Key, string? EditorId, string? ModelName, bool IsBald);
}