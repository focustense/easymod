namespace Focus.Graphics
{
    public class TextureSet
    {
        public static readonly TextureSet Empty = new();

        public ITextureSource Diffuse { get; }
        public ITextureSource? Normal { get; }
        public ITextureSource? Specular { get; }

        public TextureSet(
            ITextureSource? diffuse = null, ITextureSource? normal = null,
            ITextureSource? specular = null)
        {
            Diffuse = diffuse ?? new DummyTextureSource();
            Normal = normal;
            Specular = specular;
        }
    }
}
