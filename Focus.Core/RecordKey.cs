using System;

namespace Focus
{
    public interface IRecordKey
    {
        string BasePluginName { get; }
        string LocalFormIdHex { get; }
    }

    public class RecordKey : IRecordKey
    {
        internal static readonly StringComparison DefaultComparison = StringComparison.CurrentCultureIgnoreCase;

        public string BasePluginName { get; private init; }
        public string LocalFormIdHex { get; private init; }

        public RecordKey(string basePluginName, string localFormIdHex)
        {
            BasePluginName = basePluginName;
            LocalFormIdHex = localFormIdHex;
        }

        public RecordKey(IRecordKey key) : this(key.BasePluginName, key.LocalFormIdHex) { }

        public bool Equals(IRecordKey key)
        {
            return Equals(this, key);
        }

        public override bool Equals(object? obj)
        {
            return obj is IRecordKey recordKey && Equals(recordKey);
        }

        public static bool Equals(IRecordKey? x, IRecordKey? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;
            return
                string.Equals(x.BasePluginName, y.BasePluginName, DefaultComparison) &&
                string.Equals(x.LocalFormIdHex, y.LocalFormIdHex, DefaultComparison);
        }

        public override int GetHashCode()
        {
            return $"{LocalFormIdHex}:{BasePluginName}".GetHashCode(DefaultComparison);
        }

        public static bool operator ==(RecordKey x, IRecordKey y)
        {
            return Equals(x, y);
        }

        public static bool operator !=(RecordKey x, IRecordKey y)
        {
            return !(x == y);
        }
    }

    public static class RecordKeyExtensions
    {
        public static bool PluginEquals(this IRecordKey recordKey, string pluginName)
        {
            return string.Equals(recordKey.BasePluginName, pluginName, RecordKey.DefaultComparison);
        }
    }
}