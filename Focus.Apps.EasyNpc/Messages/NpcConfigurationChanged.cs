namespace Focus.Apps.EasyNpc.Messages
{
    public class NpcConfigurationChanged
    {
        public RecordKey Key { get; private init; }

        public NpcConfigurationChanged(RecordKey key)
        {
            Key = key;
        }

        public NpcConfigurationChanged(IRecordKey key)
        {
            Key = new RecordKey(key);
        }
    }
}