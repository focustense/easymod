using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Focus
{
    public interface IRecordKey
    {
        string BasePluginName { get; }
        string LocalFormIdHex { get; }
    }

    public class RecordKey : IRecordKey
    {
        public static readonly RecordKey Null = new(string.Empty, string.Empty);

        internal static readonly StringComparison DefaultComparison = StringComparison.CurrentCultureIgnoreCase;

        public string BasePluginName { get; private init; }
        public string LocalFormIdHex { get; private init; }

        public static RecordKey Parse(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            var tokens = value.Split(':');
            if (tokens.Length != 2)
                throw new ArgumentException(
                    $"Invalid record key format: '{value}'. Must be of the form '0123456:Plugin.esp'.", nameof(value));
            // Currently not doing any validation of the tokens themselves. Not likely to be an issue when all actual
            // keys generally come from the game data, and those that don't (i.e. saved in a user profile) simply won't
            // match the game data and will be ignored.
            return new RecordKey(tokens[1], tokens[0]);
        }

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
            return obj is IRecordKey recordKey && RecordKeyComparer.Default.Equals(this, recordKey);
        }

        public static bool Equals(IRecordKey? x, IRecordKey? y)
        {
            return RecordKeyComparer.Default.Equals(x, y);
        }

        public override int GetHashCode()
        {
            return RecordKeyComparer.Default.GetHashCode(this);
        }

        public override string ToString()
        {
            return $"{LocalFormIdHex}:{BasePluginName}";
        }

        public static bool operator ==(RecordKey? x, IRecordKey? y)
        {
            return Equals(x, y);
        }

        public static bool operator !=(RecordKey? x, IRecordKey? y)
        {
            return !(x == y);
        }
    }

    public class RecordKeyComparer : IEqualityComparer<IRecordKey>
    {
        public static readonly RecordKeyComparer Default = new();

        internal static readonly StringComparison DefaultComparison = StringComparison.CurrentCultureIgnoreCase;

        public bool Equals(IRecordKey? x, IRecordKey? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;
            return
                string.Equals(x.BasePluginName, y.BasePluginName, DefaultComparison) &&
                string.Equals(x.LocalFormIdHex, y.LocalFormIdHex, DefaultComparison);
        }

        public int GetHashCode([DisallowNull] IRecordKey obj)
        {
            return HashCode.Combine(
                obj.BasePluginName.GetHashCode(DefaultComparison), obj.LocalFormIdHex.GetHashCode(DefaultComparison));
        }
    }

    public static class RecordKeyExtensions
    {
        public static bool PluginEquals(this IRecordKey recordKey, string pluginName)
        {
            return string.Equals(recordKey.BasePluginName, pluginName, RecordKeyComparer.DefaultComparison);
        }
    }
}