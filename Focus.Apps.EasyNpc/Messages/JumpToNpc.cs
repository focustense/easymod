namespace Focus.Apps.EasyNpc.Messages
{
    public class JumpToNpc
    {
        public RecordKey Key { get; private init; }

        public JumpToNpc(RecordKey key)
        {
            Key = key;
        }

        public JumpToNpc(string basePluginName, string localFormIdHex)
            : this(new RecordKey(basePluginName, localFormIdHex)) { }
    }
}