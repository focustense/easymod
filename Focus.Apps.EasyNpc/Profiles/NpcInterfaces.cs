namespace Focus.Apps.EasyNpc.Profiles
{
    public interface INpcSearchParameters : IRecordKey
    {
        string EditorId { get; }
        string Name { get; }
    }

    public interface INpcBasicInfo : INpcSearchParameters
    {
        bool IsFemale { get; }
    }
}
