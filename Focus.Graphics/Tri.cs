using System.Diagnostics;
using System.Numerics;

namespace Focus.Graphics
{
    public class Tri
    {
        public static Tri WithVertices(Vertex a, Vertex b, Vertex c)
        {
            var deltaPos1 = b.Point - a.Point;
            var deltaPos2 = c.Point - a.Point;
            var deltaUV1 = b.UV - a.UV;
            var deltaUV2 = c.UV - a.UV;
            var r = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV1.Y * deltaUV2.X);
            if (float.IsInfinity(r))
            {
                // We need 3 distinct UV coordinates. If any 2 are the same, then either one of the
                // deltaUVs above will be 0, or both deltaUVs will be the same vector.
                // In either case, d1.x * d2.y will equal d1.y * d2.x and 1/d gets us infinity.
                // Other libraries (e.g. assimp) seem to work around this by assuming default UV
                // direction in this case.
                deltaUV1 = new Vector2(1, 0);
                deltaUV2 = new Vector2(0, 1);
                r = 1; // 1 * 1 - 0 * 0
            }
            var tangent = (deltaPos1 * deltaUV2.Y - deltaPos2 * deltaUV1.Y) * r;
            var bitangent = (deltaPos2 * deltaUV1.X - deltaPos1 * deltaUV2.X) * r;
            if (float.IsInfinity(tangent.X))
                Debugger.Break();
            return new Tri(
                Orthogonalize(a.Point, a.Normal, tangent, bitangent, a.UV),
                Orthogonalize(b.Point, b.Normal, tangent, bitangent, b.UV),
                Orthogonalize(c.Point, c.Normal, tangent, bitangent, c.UV));
        }

        private readonly VertexData a;
        private readonly VertexData b;
        private readonly VertexData c;

        private Tri(VertexData a, VertexData b, VertexData c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }

        public IEnumerable<VertexData> GetVertices()
        {
            yield return a;
            yield return b;
            yield return c;
        }

        private static VertexData Orthogonalize(
            Vector3 point, Vector3 normal, Vector3 tangent, Vector3 bitangent, Vector2 uv)
        {
            Vector3 origTangent = tangent;
            // Gram-Schmidt: First make tangent orthogonal to normal
            tangent = Vector3.Normalize(tangent - normal * Vector3.Dot(normal, tangent));
            // Make bitangent orthogonal to normal and to tangent
            bitangent = Vector3.Normalize(bitangent - normal * Vector3.Dot(normal, bitangent));
            bitangent = Vector3.Normalize(bitangent - tangent * Vector3.Dot(tangent, bitangent));
            if (Vector3.Dot(Vector3.Cross(normal, tangent), bitangent) < 0)
                tangent *= -1.0f;
            if (float.IsNaN(tangent.X))
                Debugger.Break();
            return new VertexData(point, normal, tangent, bitangent, uv);
        }
    }
}
