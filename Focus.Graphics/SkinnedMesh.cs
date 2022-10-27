﻿using System.Collections.Immutable;
using System.Numerics;

namespace Focus.Graphics
{
    public record Pose(IImmutableDictionary<Bone, Matrix4x4> BoneTransforms);

    public interface ISkinnable
    {
        IImmutableDictionary<Bone, Matrix4x4> GetSkinTransforms();
        IImmutableDictionary<Vertex, IBoneWeights> GetVertexWeights();
    }

    public class SkinnedMesh : IMesh, ISkinnable
    {
        public class Builder
        {
            private readonly Dictionary<Bone, Matrix4x4> boneInverseTransforms = new();
            private readonly Dictionary<Vertex, IBoneWeights> vertexWeights = new();

            private IImmutableList<Face> faces = ImmutableList<Face>.Empty;

            public SkinnedMesh Build()
            {
                return new SkinnedMesh(
                    faces,
                    boneInverseTransforms.ToImmutableDictionary(),
                    vertexWeights.ToImmutableDictionary());
            }

            public Builder SetFaces(IEnumerable<Face> faces)
            {
                this.faces = faces.ToImmutableList();
                return this;
            }

            public Builder SetInverseTransform(Bone bone, Matrix4x4 inverseTransform)
            {
                boneInverseTransforms[bone] = inverseTransform;
                return this;
            }

            public Builder SetVertexWeights(Vertex vertex, IBoneWeights weights)
            {
                vertexWeights[vertex] = weights;
                return this;
            }
        }

        public IEnumerable<Face> Faces => skinnedFaces;

        private readonly IImmutableList<Face> unskinnedFaces;
        // Note: inverse transforms are relative to the bind/rest position, not the scene position,
        // which is what we'll receive when skinning.
        private readonly IImmutableDictionary<Bone, Matrix4x4> boneInverseTransforms;
        // NIFs appear to make it possible to have true duplicate vertices, i.e. having not only the
        // same coordinates but also same UVs, normals, etc., and these could therefore have
        // different bone weights. In practice this really makes no sense and shouldn't happen.
        private readonly IImmutableDictionary<Vertex, IBoneWeights> vertexWeights;

        private IImmutableList<Face> skinnedFaces;

        public SkinnedMesh(
            IImmutableList<Face> unskinnedFaces,
            IImmutableDictionary<Bone, Matrix4x4> boneInverseTransforms,
            IImmutableDictionary<Vertex, IBoneWeights> vertexWeights)
        {
            this.unskinnedFaces = unskinnedFaces;
            this.skinnedFaces = unskinnedFaces;
            this.boneInverseTransforms = boneInverseTransforms;
            this.vertexWeights = vertexWeights;
        }

        public void ApplyPose(Pose pose)
        {
            // In addition to "precomputing" the pose this way, we should consider exposing the
            // intermediate vertex-to-transform map so that it can be computed in the veretx shader
            // instead. This would likely have much better performance under frequent posing (e.g.
            // animations). Since posing is going to be done very infrequently for the time being,
            // probably only once per mesh, CPU compute is fine for now.
            skinnedFaces = unskinnedFaces.Select(f => ApplyPoseToFace(f, pose)).ToImmutableList();
        }

        public IImmutableDictionary<Bone, Matrix4x4> GetSkinTransforms()
        {
            return boneInverseTransforms;
        }

        public IImmutableDictionary<Vertex, IBoneWeights> GetVertexWeights()
        {
            return vertexWeights;
        }

        private Face ApplyPoseToFace(Face face, Pose pose)
        {
            var posedVertices = face.Vertices.Select(v => ApplyPoseToVertex(v, pose));
            return new Face(posedVertices.ToImmutableList());
        }

        private Vertex ApplyPoseToVertex(Vertex vertex, Pose pose)
        {
            if (!vertexWeights.TryGetValue(vertex, out var weights))
                return vertex;
            Matrix4x4 transform = new Matrix4x4();
            foreach (var (bone, weight) in weights)
            {
                if (!boneInverseTransforms.TryGetValue(bone, out var boneInverseTransform) ||
                    !pose.BoneTransforms.TryGetValue(bone, out var boneTransform))
                {
                    throw new ArgumentException(
                        $"Bone '{bone.Name}' in bone weights for vertex {vertex} is missing from " +
                        "the pose or inverse transforms.");
                }
                transform += boneTransform * boneInverseTransform * weight;
            }
            // Vector3.Transform treats vectors as rows rather than columns, which multiplies in the
            // wrong order. To fix, we can write our own transform function, or just transpose the
            // matrix which makes it equivalent to columns.
            transform = Matrix4x4.Transpose(transform);
            var transformedPoint = Vector3.Transform(vertex.Point, transform);
            var transformedNormal = Vector3.TransformNormal(vertex.Normal, transform);
            return new Vertex(transformedPoint, transformedNormal, vertex.UV);
        }
    }
}