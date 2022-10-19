namespace Focus.Graphics
{
    public ref struct TextureData
    {
        public uint Width;
        public uint Height;
        public Span<int> Pixels;

        public TextureData(uint width, uint height, Span<int> pixels)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
        }
    }
}
