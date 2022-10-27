namespace Focus.Graphics
{
    public class TextureSet
    {
        public static readonly TextureSet Empty = new();

        public ITextureSource? Detail { get; }
        public ITextureSource Diffuse { get; }
        // Environment texture (as opposed to "environment map" in some engines) is the reflection
        // source or skybox, and should be a cube map.
        public ITextureSource? Environment { get; }
        public ITextureSource? Normal { get; }
        // Reflection map tells us how much is reflected, vs. environment texture which gives the
        // thing being reflected. Reflection map is generally low-res, black and white.
        public ITextureSource? Reflection { get; }
        public ITextureSource? Specular { get; }
        public ITextureSource? Tint { get; }

        public TextureSet(
            ITextureSource? diffuse = null, ITextureSource? normal = null,
            ITextureSource? specular = null, ITextureSource? environment = null,
            ITextureSource? reflection = null, ITextureSource? detail = null,
            ITextureSource? tint = null)
        {
            Diffuse = diffuse ?? new DummyTextureSource();
            Normal = normal;
            Specular = specular;
            Environment = environment;
            Reflection = reflection;
            Detail = detail;
            Tint = tint;
        }
    }
}
