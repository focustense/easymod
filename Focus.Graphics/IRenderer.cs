using System.Numerics;

namespace Focus.Graphics
{
    public interface IRenderer : IDisposable
    {
        Vector3 GetModelSize();

        void LoadGeometry(IMesh mesh);
        void LoadTextures(TextureSet textureSet);
        void Render(Matrix4x4 model, Matrix4x4 view, Matrix4x4 projection);
    }
}
