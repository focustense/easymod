using System.Numerics;
using System.Runtime.InteropServices;

namespace Focus.Graphics
{
    public interface IMesh
    {
        IEnumerable<Face> Faces { get; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public record Vertex(Vector3 Point, Vector3 Normal, Vector2 UV);

    public record Face(IEnumerable<Vertex> Vertices)
    {
        public Face(params Vertex[] vertices)
            : this(vertices.AsEnumerable()) { }

        public IEnumerable<T> Triangulate<T>(Func<Vertex, Vertex, Vertex, T> selector)
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
                yield return selector(firstVertex, previousVertex, nextVertex);
                previousVertex = nextVertex;
            }
        }
    }
}
