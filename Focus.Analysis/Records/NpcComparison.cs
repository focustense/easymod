namespace Focus.Analysis.Records
{
    public class NpcComparison : RecordComparison
    {
        public bool ModifiesBehavior { get; init; }
        public bool ModifiesBody { get; init; }
        public bool ModifiesFace { get; init; }
        public bool ModifiesHair { get; init; }
        public bool ModifiesHeadParts { get; init; }
        public bool ModifiesOutfits { get; init; }
        public bool ModifiesRace { get; init; }
        public bool ModifiesScales { get; init; }
        public string? PluginName { get; init; } = string.Empty;
    }
}