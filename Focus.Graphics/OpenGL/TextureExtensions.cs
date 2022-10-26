using Silk.NET.OpenGL;

namespace Focus.Graphics.OpenGL
{
    static class TextureExtensions
    {
        delegate Texture TextureFactory(
            GL gl, TextureUnit slot, uint width, uint height, Span<int> pixels,
            IEnumerable<CubemapFace>? cubemapFaceOrder);

        public static Texture CreateTexture(this ITextureSource source, GL gl, TextureUnit slot)
        {
            var textureData = source.GetTextureData();
            var cubemapFaceOrder =
                source.Type == TextureType.RowsCubemap ? source.CubemapFaceOrder : null;
            var factory = GetTextureFactory(source.Format);
            return factory(
                gl, slot, textureData.Width, textureData.Height, textureData.Pixels,
                cubemapFaceOrder);
        }

        private static TextureFactory GetTextureFactory(TexturePixelFormat format) => format switch
        {
            TexturePixelFormat.ARGB => Texture.FromArgb,
            TexturePixelFormat.BGRA => Texture.FromBgra,
            TexturePixelFormat.RGBA => Texture.FromRgba,
            _ => throw new NotSupportedException("Unsupported texture format")
        };
    }
}
