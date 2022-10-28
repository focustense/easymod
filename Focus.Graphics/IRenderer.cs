using System.Numerics;

namespace Focus.Graphics
{
    public record Bounds3(Vector3 Min, Vector3 Max)
    {
        public static readonly Bounds3 Default = new Bounds3(Vector3.Zero, Vector3.One);
        public static readonly Bounds3 Empty = new Bounds3(Vector3.Zero, Vector3.Zero);

        public static Bounds3 FromPoints(IEnumerable<Vector3> points)
        {
            Vector3? min = null;
            Vector3? max = null;
            foreach (var point in points)
            {
                min = min != null ? Vector3.Min(min.Value, point) : point;
                max = max != null ? Vector3.Max(max.Value, point) : point;
            }
            return new Bounds3(min ?? Vector3.Zero, max ?? Vector3.Zero);
        }

        public static Bounds3 UnionAll<T>(IEnumerable<T> source, Func<T, Bounds3> getBounds)
        {
            return source.Aggregate(
                (Bounds3?)null, (acc, x) => Union(getBounds(x), acc)) ?? Default;
        }

        public static Bounds3 Union(Bounds3 first, Bounds3? second)
        {
            return (second != null && !second.IsEmpty()) ? first.Union(second) : first;
        }

        public Vector3 GetSize()
        {
            return Vector3.Abs(Max - Min);
        }

        public bool IsEmpty()
        {
            return Min == Vector3.Zero && Max == Vector3.Zero;
        }

        public Bounds3 Union(Bounds3 other)
        {
            return new Bounds3(Vector3.Min(Min, other.Min), Vector3.Max(Max, other.Max));
        }
    }

    public interface IRenderer : IDisposable
    {
        Bounds3 GetModelBounds();

        Vector3 GetModelCenter()
        {
            var bounds = GetModelBounds();
            return (bounds.Min + bounds.Max) / 2;
        }

        Vector3 GetModelSize()
        {
            var bounds = GetModelBounds();
            return Vector3.Abs(bounds.Max - bounds.Min);
        }

        void Render(Matrix4x4 model, Matrix4x4 view, Matrix4x4 projection);
    }

    public interface IMeshRenderer : IRenderer
    {
        bool HasTransparency();
        void LoadGeometry(IMesh mesh);
        void LoadTextures(TextureSet textureSet);
        void SetLights(IEnumerable<Light> lights);
    }
}
