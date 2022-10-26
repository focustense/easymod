using System.Drawing;

namespace Focus.Graphics
{
    public class DummyTextureSource : ITextureSource
    {
        public TexturePixelFormat Format => TexturePixelFormat.ARGB;
        public TextureType Type => TextureType.Rows2D;

        private readonly int color;

        public DummyTextureSource() : this(Color.Purple) { }

        public DummyTextureSource(Color color) : this(color.ToArgb()) { }


        private DummyTextureSource(int color)
        {
            this.color = color;
        }

        public TextureData GetTextureData()
        {
            return new TextureData(1, 1, new[] { color });
        }
    }
}
