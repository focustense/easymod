using System.Numerics;
using System.Runtime.InteropServices;

namespace Focus.Graphics
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexData
    {
        internal static readonly IntPtr PointOffset = Marshal.OffsetOf<VertexData>(nameof(Point));
        internal static readonly IntPtr NormalOffset = Marshal.OffsetOf<VertexData>(nameof(Normal));
        internal static readonly IntPtr TangentOffset =
            Marshal.OffsetOf<VertexData>(nameof(Tangent));
        internal static readonly IntPtr BitangentOffset =
            Marshal.OffsetOf<VertexData>(nameof(Bitangent));
        internal static readonly IntPtr UVOffset = Marshal.OffsetOf<VertexData>(nameof(UV));

        public Vector3 Point;
        public Vector3 Normal;
        public Vector3 Tangent;
        public Vector3 Bitangent;
        public Vector2 UV;

        public VertexData(
            Vector3 point, Vector3 normal, Vector3 tangent, Vector3 bitangent, Vector2 uv)
        {
            Point = point;
            Normal = normal;
            Tangent = tangent;
            Bitangent = bitangent;
            UV = uv;
        }
    }
}
