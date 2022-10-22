using System.Numerics;

namespace Focus.Graphics
{
    public record Bounds3(Vector3 Min, Vector3 Max)
    {
        public static readonly Bounds3 Default = new Bounds3(Vector3.Zero, Vector3.One);

        public static Bounds3 Union(Bounds3 first, Bounds3? second)
        {
            return second != null ? first.Union(second) : first;
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
        void LoadGeometry(IMesh mesh);
        void LoadTextures(TextureSet textureSet);
    }
}
