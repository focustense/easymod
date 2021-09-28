using System;

namespace Focus.Analysis.Records
{
    public enum AssetKind { Unknown, Mesh, Morph, Texture, Icon };

    public class AssetReference
    {
        public AssetKind Kind { get; init; }
        public string NormalizedPath => GetNormalizedPath();
        public string Path { get; init; } = string.Empty;

        public AssetReference()
        {
        }

        public AssetReference(string path)
        {
            Path = path;
        }

        public AssetReference(string path, AssetKind kind)
        {
            Path = path;
            Kind = kind;
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
