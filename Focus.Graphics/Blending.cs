namespace Focus.Graphics
{
    public enum AlphaBlendFactor
    {
        Zero,
        One,
        SourceColor,
        OneMinusSourceColor,
        DestinationColor,
        OneMinusDestinationColor,
        SourceAlpha,
        OneMinusSourceAlpha,
        DestinationAlpha,
        OneMinusDestinationAlpha,
        ConstantColor,
        OneMinusConstantColor,
        ConstantAlpha,
        OneMinusConstantAlpha,
        SourceAlphaSaturate,
    }

    public enum AlphaTestFunction
    {
        // GL doesn't directly support this anymore, we have to implement it in shader.
        // The order of these values must be kept in sync.
        Always,
        Never,
        Less,
        LessOrEqual,
        Equal,
        Greater,
        GreaterOrEqual,
        NotEqual,
    }

    public record AlphaBlendSettings(
        bool EnableBlending, AlphaBlendFactor SourceFactor, AlphaBlendFactor DestinationFactor,
        bool EnableTesting, AlphaTestFunction TestFunction, float TestThreshold)
    {
        public static readonly AlphaBlendSettings Default = new(
            false, AlphaBlendFactor.One, AlphaBlendFactor.Zero, false, AlphaTestFunction.Never, 0);
    }
}
