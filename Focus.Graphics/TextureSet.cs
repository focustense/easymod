namespace Focus.Graphics
{
    public class TextureSet
    {
        public static readonly TextureSet Empty = new();

        public ITextureSource Diffuse { get; }
        public ITextureSource? Normal { get; }

        public TextureSet(ITextureSource? diffuse = null, ITextureSource? normal = null)
        {
            Diffuse = diffuse ?? new DummyTextureSource();
            Normal = normal;
        }
    }
}
