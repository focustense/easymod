namespace Focus.Apps.EasyNpc.Messages
{
    public class JumpToNpc
    {
        public IRecordKey Key { get; private init; }

        public JumpToNpc(IRecordKey key)
        {
            Key = key;
        }

        public JumpToNpc(string basePluginName, string localFormIdHex)
            : this(new RecordKey(basePluginName, localFormIdHex)) { }
    }
}