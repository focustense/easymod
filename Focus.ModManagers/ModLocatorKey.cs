using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Focus.ModManagers
{
    public interface IModLocatorKey
    {
        string Id { get; }
        string Name { get; }
    }

    public class ModLocatorKey : IModLocatorKey
    {
        public static readonly ModLocatorKey Empty = new(string.Empty, string.Empty);

        private static readonly string prefix = "::";
        private static readonly string separator = "//";

        public string Id { get; init; }
        public string Name { get; init; }

        public static bool TryParse(string str, [MaybeNullWhen(false)] out ModLocatorKey key)
        {
            key = null;
            if (!str.StartsWith(prefix))
                return false;
            var parts = str[separator.Length..].Split(separator);
            if (parts.Length >= 2)
            {
                key = new ModLocatorKey(parts[0], parts[1]);
                return true;
            }
            return false;
        }

        public ModLocatorKey(IModLocatorKey key) : this(key.Id, key.Name) { }

        public ModLocatorKey(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not IModLocatorKey key)
                return false;
            return ModLocatorKeyComparer.Default.Equals(this, key);
        }

        public override int GetHashCode()
        {
            return ModLocatorKeyComparer.Default.GetHashCode(this);
        }

        public override string ToString()
        {
            return prefix + string.Join(separator, Id, Name);
        }

        public static bool operator ==(ModLocatorKey? x, IModLocatorKey? y)
        {
            return Equals(x, y);
        }

        public static bool operator !=(ModLocatorKey? x, IModLocatorKey? y)
        {
            return !(x == y);
        }
    }

    public class ModLocatorKeyComparer : IEqualityComparer<IModLocatorKey>
    {
        public static readonly ModLocatorKeyComparer Default = new ModLocatorKeyComparer();

        private static readonly StringComparer stringComparer = StringComparer.CurrentCultureIgnoreCase;

        public bool Equals(IModLocatorKey? x, IModLocatorKey? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;
            return stringComparer.Equals(x.Id, y.Id) && stringComparer.Equals(x.Name, y.Name);
        }

        public int GetHashCode([DisallowNull] IModLocatorKey obj)
        {
            return HashCode.Combine(stringComparer.GetHashCode(obj.Id), stringComparer.GetHashCode(obj.Name));
        }
    }

    public static class ModLocatorExtensions
    {
        public static bool IsEmpty(this IModLocatorKey key)
        {
            return ModLocatorKey.Empty.Equals(key);
        }
    }
}
