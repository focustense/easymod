namespace Focus.Graphics
{
    public enum CubemapFace
    {
        Top,
        Bottom,
        Left,
        Right,
        Front,
        Back,
    }

    public enum TexturePixelFormat
    {
        ARGB,
        BGRA,
        RGBA,
    }

    public enum TextureType
    {
        // Typical type of image, where array rows correspond to pixel rows.
        Rows2D,

        // Cubemap in row order; like Rows2D, array rows correspond to rows of a single face, then
        // all the rows of the next face, etc. Each entire face is a contiguous region of the same
        // width and height.
        RowsCubemap,
    }

    public interface ITextureSource
    {
        // Only applies for Cubemap texture types.
        IEnumerable<CubemapFace> CubemapFaceOrder => Enumerable.Empty<CubemapFace>();
        TexturePixelFormat Format { get; }
        TextureType Type { get; }

        TextureData GetTextureData();
    }
}
