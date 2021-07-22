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

        public static bool Equals(IRecordKey x, IRecordKey y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;
            return x.Equals(y);
        }
    }
}