﻿using System.Collections.Generic;

namespace Focus.Analysis.Records
{
    public class NpcAnalysis : RecordAnalysis
    {
        public override RecordType Type => RecordType.Npc;

        public bool CanUseFaceGen { get; init; }
        public NpcComparison? ComparisonToBase { get; init; }
        public IReadOnlyList<NpcComparison> ComparisonToMasters { get; init; } = Empty.ReadOnlyList<NpcComparison>();
        public NpcComparison? ComparisonToPreviousOverride { get; init; }
        public IReadOnlyList<RecordKey> MainHeadParts { get; init; } = Empty.ReadOnlyList<RecordKey>();
        public bool IsAudioTemplate { get; init; }
        public bool IsChild { get; init; }
        public bool IsFemale { get; init; }
        public string Name { get; init; } = string.Empty;
        public RecordKey? SkinKey { get; init; }
        public NpcTemplateInfo? TemplateInfo { get; init; }
        public NpcWigInfo? WigInfo { get; init; }
    }

    public enum NpcTemplateTargetType { Invalid, Npc, LeveledNpc }

    public record NpcTemplateInfo(RecordKey Key, NpcTemplateTargetType TargetType, bool InheritsTraits);

    public record NpcWigInfo(RecordKey Key, string? EditorId, string? ModelName, bool IsBald);
}