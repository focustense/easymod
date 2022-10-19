using System.Numerics;

namespace Focus.Graphics
{
    public interface IMesh
    {
        IEnumerable<Face> Faces { get; }
    }

    public record Vertex(Vector3 Point, Vector3 Normal, Vector2 UV);

    public record Face(IEnumerable<Vertex> Vertices)
    {
        public IEnumerable<Tri> Triangulate()
        {
            using var vertexEnumerator = Vertices.GetEnumerator();
            if (!vertexEnumerator.MoveNext())
                yield break;
            var firstVertex = vertexEnumerator.Current;
            if (!vertexEnumerator.MoveNext())
                yield break;
            var previousVertex = vertexEnumerator.Current;
            while (vertexEnumerator.MoveNext())
            {
                var nextVertex = vertexEnumerator.Current;
                yield return Tri.WithVertices(firstVertex, previousVertex, nextVertex);
                previousVertex = nextVertex;
            }
        }
    }
}
