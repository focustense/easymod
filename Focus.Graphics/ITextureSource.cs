namespace Focus.Graphics
{
    public enum TexturePixelFormat
    {
        ARGB,
        BGRA
    }

    public interface ITextureSource
    {
        TexturePixelFormat Format { get; }

        TextureData GetTextureData();
    }
}
