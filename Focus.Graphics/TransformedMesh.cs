using System.Numerics;

namespace Focus.Graphics
{
    public class TransformedMesh : IMesh
    {
        private readonly IMesh originalMesh;
        private readonly Matrix4x4 transform;

        public TransformedMesh(IMesh originalMesh, Matrix4x4 transform)
        {
            this.originalMesh = originalMesh;
            this.transform = transform;
        }

        public IEnumerable<Face> Faces => originalMesh.Faces
            .Select(f => new Face(
                f.Vertices.Select(v => new Vertex(
                    Vector3.Transform(v.Point, transform),
                    Vector3.Normalize(Vector3.TransformNormal(v.Normal, transform)),
                    v.UV))));
    }
}
