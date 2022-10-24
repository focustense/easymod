using System.Numerics;
using System.Runtime.InteropServices;

namespace Focus.Graphics.OpenGL
{
    public record SimpleTri(SimpleVertexData A, SimpleVertexData B, SimpleVertexData C)
    {
        public static SimpleTri WithVertices(Vertex a, Vertex b, Vertex c, Vector3 color)
        {
            return new SimpleTri(
                new SimpleVertexData(a.Point, a.Normal, color),
                new SimpleVertexData(b.Point, b.Normal, color),
                new SimpleVertexData(c.Point, c.Normal, color));
        }

        public IEnumerable<SimpleVertexData> GetVertices()
        {
            yield return A;
            yield return B;
            yield return C;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SimpleVertexData
    {
        internal static readonly IntPtr PointOffset = Marshal.OffsetOf<SimpleVertexData>(nameof(Point));
        internal static readonly IntPtr NormalOffset = Marshal.OffsetOf<SimpleVertexData>(nameof(Normal));
        internal static readonly IntPtr ColorOffset = Marshal.OffsetOf<SimpleVertexData>(nameof(Color));

        public Vector3 Point;
        public Vector3 Normal;
        public Vector3 Color;

        public SimpleVertexData(Vector3 point, Vector3 normal, Vector3 color)
        {
            Point = point;
            Normal = normal;
            Color = color;
        }
    }
}
