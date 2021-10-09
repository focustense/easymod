using System.Collections.Generic;

namespace Focus.Analysis.Records
{
    public enum HeadPartType
    {
        Eyebrows,
        Eyes,
        Face,
        FacialHair,
        Hair,
        Misc,
        Scars,
        Unknown,
    }

    public class HeadPartAnalysis : RecordAnalysis
    {
        public override RecordType Type => RecordType.HeadPart;

        // Comparisons don't exist yet, but are likely to be added soon, e.g. for
        // vanilla head part replacers: https://github.com/focustense/easymod/issues/6.

        public IReadOnlyList<IRecordKey> ExtraPartKeys { get; init; } = Empty.ReadOnlyList<IRecordKey>();
        public bool IsMainPart { get; init; }
        public string? ModelFileName { get; init; }
        public string Name { get; init; } = string.Empty;
        public HeadPartType PartType { get; init; }
        public bool SupportsFemaleNpcs { get; init; }
        public bool SupportsMaleNpcs { get; init; }
        // Vanilla races are not meant to be an exhaustive list of all valid races. They are used for some specific
        // heuristics, such as matching against common names/identifiers without explicit race info.
        public IReadOnlySet<VanillaRace> ValidVanillaRaces { get; init; } = new HashSet<VanillaRace>();
    }
}
