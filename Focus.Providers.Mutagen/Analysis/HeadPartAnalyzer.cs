using Focus.Analysis.Records;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Providers.Mutagen.Analysis
{
    public class HeadPartAnalyzer : IRecordAnalyzer<HeadPartAnalysis>
    {
        public RecordType RecordType => RecordType.HeadPart;

        private readonly IGroupCache groups;
        private readonly IReferenceChecker<IHeadPartGetter>? referenceChecker;

        public HeadPartAnalyzer(IGroupCache groups, IReferenceChecker<IHeadPartGetter>? referenceChecker = null)
        {
            this.groups = groups;
            this.referenceChecker = referenceChecker;
        }

        public HeadPartAnalysis Analyze(string pluginName, IRecordKey key)
        {
            var group = groups.Get(pluginName, x => x.HeadParts);
            var headPart = group?.TryGetValue(key.ToFormKey());
            if (headPart is null)
                return new() { BasePluginName = key.BasePluginName, LocalFormIdHex = key.LocalFormIdHex };

            var isOverride = !key.PluginEquals(pluginName);
            var isMale = headPart.Flags.HasFlag(HeadPart.Flag.Male);
            var isFemale = headPart.Flags.HasFlag(HeadPart.Flag.Female);
            return new()
            {
                BasePluginName = key.BasePluginName,
                LocalFormIdHex = key.LocalFormIdHex,
                EditorId = headPart.EditorID ?? string.Empty,
                Exists = true,
                InvalidPaths = referenceChecker.SafeCheck(headPart),
                IsInjectedOrInvalid = isOverride && !groups.MasterExists(key.ToFormKey(), RecordType),
                IsOverride = isOverride,
                ExtraPartKeys = headPart.ExtraParts.ToRecordKeys(),
                IsMainPart = !headPart.Flags.HasFlag(HeadPart.Flag.IsExtraPart),
                ModelFileName = headPart.Model?.File,
                Name = headPart.Name?.String ?? string.Empty,
                PartType = ConvertHeadPartType(headPart.Type),
                // If neither male nor female are defined in the flags, assume it applies to both/either.
                // TODO: Is this a correct assumption?
                SupportsFemaleNpcs = isFemale || !isMale,
                SupportsMaleNpcs = isMale || !isFemale,
                ValidVanillaRaces = GetValidVanillaRaces(headPart).ToHashSet(),
            };
        }
        
        private static HeadPartType ConvertHeadPartType(HeadPart.TypeEnum? sourceType)
        {
            if (!sourceType.HasValue)
                return HeadPartType.Unknown;
            return Enum.TryParse<HeadPartType>(Enum.GetName(sourceType.Value), out var parsedType) ?
                parsedType : HeadPartType.Unknown;
        }

        private IEnumerable<VanillaRace> GetValidVanillaRaces(IHeadPartGetter headPart)
        {
            if (headPart.ValidRaces.IsNull)
                return Enumerable.Empty<VanillaRace>();
            var raceList = headPart.ValidRaces.WinnerFrom(groups);
            if (raceList is null)
                return Enumerable.Empty<VanillaRace>();
            return raceList.Items
                .Select(x => x.FormKey.ToLink<IRaceGetter>().WinnerFrom(groups))
                .NotNull()
                .Select(x => InferRace(x.EditorID))
                .NotNull();
        }

        private static VanillaRace? InferRace(string? editorId)
        {
            return editorId switch
            {
                "NordRace" => VanillaRace.Nord,
                "ImperialRace" => VanillaRace.Imperial,
                "RedguardRace" => VanillaRace.Redguard,
                "BretonRace" => VanillaRace.Breton,
                "HighElfRace" => VanillaRace.HighElf,
                "DarkElfRace" => VanillaRace.DarkElf,
                "WoodElfRace" => VanillaRace.WoodElf,
                "OrcRace" => VanillaRace.Orc,
                "ElderRace" => VanillaRace.Elder,
                _ => null,
            };
        }
    }
}
