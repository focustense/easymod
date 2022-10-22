namespace Focus.Graphics
{
    public class DummyTextureSource : ITextureSource
    {
        public TexturePixelFormat Format => TexturePixelFormat.BGRA;

        private const int defaultColor = unchecked((int)0xcccc33ff);

        public TextureData GetTextureData()
        {
            return new TextureData(1, 1, new[] { defaultColor });
        }
    }
}
