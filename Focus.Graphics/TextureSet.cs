namespace Focus.Graphics
{
    public class TextureSet
    {
        public ITextureSource? Diffuse { get; }
        public ITextureSource? Normal { get; }

        public TextureSet(ITextureSource? diffuse = null, ITextureSource? normal = null)
        {
            Diffuse = diffuse;
            Normal = normal;
        }
    }
}
