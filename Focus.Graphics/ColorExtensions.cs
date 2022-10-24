using System.Drawing;
using System.Numerics;

namespace Focus.Graphics
{
    public static class ColorExtensions
    {
        public static Vector4 ToRgbaVector(this Color color)
        {
            return new Vector4(
                color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
        }

        public static Vector3 ToRgbVector(this Color color)
        {
            return new Vector3(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);
        }
    }
}
