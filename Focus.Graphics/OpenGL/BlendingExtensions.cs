using Silk.NET.OpenGL;

namespace Focus.Graphics.OpenGL
{
    public static class BlendingExtensions
    {
        public static BlendingFactor ToGL(this AlphaBlendFactor factor) => factor switch
        {
            AlphaBlendFactor.Zero => BlendingFactor.Zero,
            AlphaBlendFactor.One => BlendingFactor.One,
            AlphaBlendFactor.SourceColor => BlendingFactor.SrcColor,
            AlphaBlendFactor.OneMinusSourceColor => BlendingFactor.OneMinusSrcColor,
            AlphaBlendFactor.DestinationColor => BlendingFactor.DstColor,
            AlphaBlendFactor.OneMinusDestinationColor => BlendingFactor.OneMinusDstColor,
            AlphaBlendFactor.SourceAlpha => BlendingFactor.SrcAlpha,
            AlphaBlendFactor.OneMinusSourceAlpha => BlendingFactor.OneMinusSrcAlpha,
            AlphaBlendFactor.DestinationAlpha => BlendingFactor.DstAlpha,
            AlphaBlendFactor.OneMinusDestinationAlpha => BlendingFactor.OneMinusDstAlpha,
            AlphaBlendFactor.ConstantColor => BlendingFactor.ConstantColor,
            AlphaBlendFactor.OneMinusConstantColor => BlendingFactor.OneMinusConstantColor,
            AlphaBlendFactor.ConstantAlpha => BlendingFactor.ConstantAlpha,
            AlphaBlendFactor.OneMinusConstantAlpha => BlendingFactor.OneMinusConstantAlpha,
            AlphaBlendFactor.SourceAlphaSaturate => BlendingFactor.SrcAlphaSaturate,
            _ => throw new ArgumentOutOfRangeException(nameof(factor)),
        };

        public static AlphaFunction ToGL(this AlphaTestFunction func) => func switch
        {
            AlphaTestFunction.Never => AlphaFunction.Never,
            AlphaTestFunction.Less => AlphaFunction.Less,
            AlphaTestFunction.LessOrEqual => AlphaFunction.Lequal,
            AlphaTestFunction.Equal => AlphaFunction.Equal,
            AlphaTestFunction.Greater => AlphaFunction.Greater,
            AlphaTestFunction.GreaterOrEqual => AlphaFunction.Gequal,
            AlphaTestFunction.NotEqual => AlphaFunction.Notequal,
            AlphaTestFunction.Always => AlphaFunction.Always,
            _ => throw new ArgumentOutOfRangeException(nameof(func)),
        };
    }
}
