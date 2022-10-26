using Silk.NET.OpenGL;

namespace Focus.Graphics.OpenGL
{
    static class TextureExtensions
    {
        public static Texture CreateTexture(this ITextureSource source, GL gl)
        {
            var textureData = source.GetTextureData();
            return source.Format switch
            {
                TexturePixelFormat.ARGB => Texture.FromArgb(gl, textureData.Width, textureData.Height, textureData.Pixels),
                TexturePixelFormat.BGRA => Texture.FromBgra(gl, textureData.Width, textureData.Height, textureData.Pixels),
                TexturePixelFormat.RGBA => Texture.FromRgba(gl, textureData.Width, textureData.Height, textureData.Pixels),
                _ => throw new NotSupportedException("Unsupported texture format")
            };
        }
    }
}
