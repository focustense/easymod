namespace Focus.Graphics
{
    public enum TexturePixelFormat
    {
        ARGB,
        BGRA,
        RGBA,
    }

    public interface ITextureSource
    {
        TexturePixelFormat Format { get; }

        TextureData GetTextureData();
    }
}
