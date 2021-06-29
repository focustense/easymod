using System;

namespace Focus.Apps.EasyNpc.GameData.Records
{
    public interface IRecordKey
    {
        string BasePluginName { get; }
        string LocalFormIdHex { get; }
    }

    public record RecordKey(string BasePluginName, string LocalFormIdHex) : IRecordKey
    {
        public RecordKey(IRecordKey key) : this(key.BasePluginName, key.LocalFormIdHex) { }

        public bool Equals(IRecordKey key)
        {
            return key.BasePluginName == BasePluginName && key.LocalFormIdHex == LocalFormIdHex;
        }
    }
}