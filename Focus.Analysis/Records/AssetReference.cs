using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Analysis.Records
{
    public enum AssetKind { Unknown, Mesh, Morph, Texture, Icon };

    public class AssetReference
    {
        public AssetKind Kind { get; init; }
        public string NormalizedPath => GetNormalizedPath();
        public string Path { get; init; } = string.Empty;
        public ISet<RecordType> SourceRecordTypes { get; init; } = new HashSet<RecordType>();

        public AssetReference()
        {
        }

        public AssetReference(string path)
            : this(path, AssetKind.Unknown, Enumerable.Empty<RecordType>()) { }

        public AssetReference(string path, AssetKind kind)
            : this(path, kind, Enumerable.Empty<RecordType>()) { }

        public AssetReference(string path, AssetKind kind, IEnumerable<RecordType> sourceRecordTypes)
        {
            Path = path;
            Kind = kind;
            SourceRecordTypes = sourceRecordTypes.ToHashSet();
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(obj, this))
                return true;
            if (obj is not AssetReference other)
                return false;
            return other.Kind == Kind && other.Path == Path;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Kind, Path);
        }

        private string GetNormalizedPath()
        {
            return Kind switch
            {
                AssetKind.Mesh or AssetKind.Morph => Path.PrefixPath("meshes"),
                AssetKind.Texture => Path.PrefixPath("textures"),
                _ => Path,
            };
        }
    }
}
