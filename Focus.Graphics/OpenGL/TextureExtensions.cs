using Silk.NET.OpenGL;

namespace Focus.Graphics.OpenGL
{
    static class TextureExtensions
    {
        public static Texture CreateTexture(this ITextureSource source, GL gl)
        {
            var textureData = source.GetTextureData();
            return Texture.FromArgb(gl, textureData.Width, textureData.Height, textureData.Pixels);
        }
    }
}
