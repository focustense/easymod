using System.Numerics;

namespace Focus.Graphics.Shapes
{
    public class Cube : IMesh
    {
        private static readonly Vector3[] Points = new[]
        {
            new Vector3(-1, -1, -1),
            new Vector3(-1, 1, -1),
            new Vector3(1, 1, -1),
            new Vector3(1, -1, -1),
            new Vector3(-1, -1, 1),
            new Vector3(-1, 1, 1),
            new Vector3(1, 1, 1),
            new Vector3(1, -1, 1),
        };

        private static readonly Vector3[] Normals = new[]
        {
            new Vector3(0, 0, -1),  // Back face
            new Vector3(0, 0, 1),   // Front face
            new Vector3(-1, 0, 0),  // Left face
            new Vector3(1, 0, 0),   // Right face
            new Vector3(0, -1, 0),  // Bottom face
            new Vector3(0, 1, 0),   // Top face
        };

        private static readonly Vector2[] UVs = new[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),
        };

        private static readonly Face[] PrecomputedFaces = new[]
        {
            // Back face
            new Face(
                new Vertex(Points[0], Normals[0], UVs[0]),
                new Vertex(Points[1], Normals[0], UVs[1]),
                new Vertex(Points[2], Normals[0], UVs[2]),
                new Vertex(Points[3], Normals[0], UVs[3])),
            // Front face
            new Face(
                new Vertex(Points[7], Normals[1], UVs[0]),
                new Vertex(Points[6], Normals[1], UVs[1]),
                new Vertex(Points[5], Normals[1], UVs[2]),
                new Vertex(Points[4], Normals[1], UVs[3])),
            // Left Face
            new Face(
                new Vertex(Points[0], Normals[2], UVs[0]),
                new Vertex(Points[4], Normals[2], UVs[1]),
                new Vertex(Points[5], Normals[2], UVs[2]),
                new Vertex(Points[1], Normals[2], UVs[3])),
            // Right Face
            new Face(
                new Vertex(Points[2], Normals[3], UVs[0]),
                new Vertex(Points[6], Normals[3], UVs[1]),
                new Vertex(Points[7], Normals[3], UVs[2]),
                new Vertex(Points[3], Normals[3], UVs[3])),
            // Bottom Face
            new Face(
                new Vertex(Points[0], Normals[4], UVs[0]),
                new Vertex(Points[3], Normals[4], UVs[1]),
                new Vertex(Points[7], Normals[4], UVs[2]),
                new Vertex(Points[4], Normals[4], UVs[3])),
            // Top Face
            new Face(
                new Vertex(Points[1], Normals[5], UVs[0]),
                new Vertex(Points[5], Normals[5], UVs[1]),
                new Vertex(Points[6], Normals[5], UVs[2]),
                new Vertex(Points[2], Normals[5], UVs[3])),
        };

        public IEnumerable<Face> Faces => PrecomputedFaces;
    }
}
