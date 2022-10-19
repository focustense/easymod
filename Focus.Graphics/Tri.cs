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
            var tangent = (deltaPos1 * deltaUV2.Y - deltaPos2 * deltaUV1.Y) * r;
            var bitangent = (deltaPos2 * deltaUV1.X - deltaPos1 * deltaUV2.X) * r;
            return new Tri(
                new VertexData(a.Point, a.Normal, Ortho(tangent, a.Normal), bitangent, a.UV),
                new VertexData(b.Point, b.Normal, Ortho(tangent, b.Normal), bitangent, b.UV),
                new VertexData(c.Point, c.Normal, Ortho(tangent, c.Normal), bitangent, c.UV));
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

        private static Vector3 Ortho(Vector3 tangent, Vector3 normal)
        {
            return Vector3.Normalize(tangent - normal * Vector3.Dot(normal, tangent));
        }
    }
}
