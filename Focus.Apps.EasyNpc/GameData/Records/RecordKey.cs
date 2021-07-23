using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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

    public class RecordKeyComparer : IEqualityComparer<IRecordKey>
    {
        public static readonly RecordKeyComparer OrdinalIgnoreCase = new(StringComparison.OrdinalIgnoreCase);

        private readonly StringComparison comparisonType;

        public RecordKeyComparer(StringComparison comparisonType)
        {
            this.comparisonType = comparisonType;
        }

        public bool Equals(IRecordKey x, IRecordKey y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;
            return
                string.Equals(x.BasePluginName, y.BasePluginName, comparisonType) &&
                string.Equals(x.LocalFormIdHex, y.LocalFormIdHex, comparisonType);
        }

        public int GetHashCode([DisallowNull] IRecordKey obj)
        {
            return $"{obj.LocalFormIdHex}:{obj.BasePluginName}".GetHashCode(comparisonType);
        }
    }
}