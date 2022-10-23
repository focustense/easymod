using Focus.Files;
using Focus.Graphics.Formats;
using nifly;
using System.Collections.Immutable;
using System.Numerics;

namespace Focus.Graphics.Bethesda
{
    public class NifSceneSource : ISceneSource
    {
        private readonly Func<Task<NifFile>> openFile;
        private readonly IAsyncFileProvider fileProvider;

        public NifSceneSource(IAsyncFileProvider fileProvider, string fileName)
            : this(fileProvider, () => OpenFileAsync(fileProvider, fileName))
        {
        }

        public NifSceneSource(IAsyncFileProvider fileProvider, Func<NifFile> openFile)
        {
            this.fileProvider = fileProvider;
            this.openFile = () => Task.FromResult(openFile());
        }

        public NifSceneSource(IAsyncFileProvider fileProvider, Func<Task<NifFile>> openFile)
        {
            this.fileProvider = fileProvider;
            this.openFile = openFile;
        }

        public async Task<IEnumerable<SceneObject>> LoadAsync()
        {
            using var file = await openFile();
            return LoadAsync(file);
        }

        private IEnumerable<SceneObject> LoadAsync(NifFile file)
        {
            using var loader = new ObjectLoader(file, fileProvider);
            return loader.LoadObjects();
        }

        private static async Task<NifFile> OpenFileAsync(
            IAsyncFileProvider fileProvider, string fileName)
        {
            var data = await fileProvider.ReadBytesAsync(fileName);
            return new NifFile(new vectoruchar(data.ToArray()));
        }

        // Using an inner class for this allows us to save some disposables without having to pass
        // them around from method to method. Just a convenience.
        class ObjectLoader : IDisposable
        {
            private readonly NifFile nif;
            private readonly NiHeader header;
            private readonly IAsyncFileProvider fileProvider;

            public ObjectLoader(NifFile file, IAsyncFileProvider fileProvider)
            {
                this.fileProvider = fileProvider;
                nif = file;
                header = file.GetHeader();
            }

            public void Dispose()
            {
                header.Dispose();
            }

            public IEnumerable<SceneObject> LoadObjects()
            {
                var bonesById = GetBoneMap(out var defaultBoneTransforms);
                var defaultPose = new Pose(defaultBoneTransforms.ToImmutableDictionary());
                var meshes = new List<IMesh>();
                using var shapes = nif.GetShapes();
                return shapes
                    .Select(shape => CreateMesh(shape, bonesById, defaultPose))
                    .NotNull()
                    .ToList();
            }

            protected SceneObject? CreateMesh(
                NiShape shape, IReadOnlyDictionary<uint, Bone> bonesById, Pose defaultPose)
            {
                var meshBuilder = new SkinnedMesh.Builder();
                if (!TrySetFacesFromVerts(shape, meshBuilder, out var outVertices))
                    return null;
                var boneIdsByIndex = GetBoneIdsByIndex(shape);
                AddSkinTransformsFromShape(shape, meshBuilder, bonesById, boneIdsByIndex);
                AddBoneWeightsFromShape(
                    shape, meshBuilder, bonesById, boneIdsByIndex, outVertices);
                var mesh = meshBuilder.Build();
                mesh.ApplyPose(defaultPose);
                var textures = GetTexturesFromShape(shape);
                return new SceneObject(mesh, textures);
            }

            private void AddBoneWeightsFromShape(
                NiShape shape,
                SkinnedMesh.Builder meshBuilder,
                IReadOnlyDictionary<uint, Bone> bonesById,
                IReadOnlyDictionary<uint, uint> boneIdsByIndex,
                IImmutableList<Vertex> outVertices)
            {
                // Bone weight lookup.
                // nifly has a GetShapeBoneWeights function, but SWIG made a total mess of it.
                if (!shape.IsSkinned())
                    return;
                using var skinInstanceRef = shape.SkinInstanceRef();
                if (
                    !header.TryGetBlock<BSDismemberSkinInstanceNiSkinInstance>(
                        skinInstanceRef, out var skinInstance)
                    || !header.TryGetBlock<NiSkinData>(skinInstance.dataRef, out var skinData))
                {
                    // Can't get weights.
                    return;
                }
                // Weights show up in both the skin data and the partition. The partition is
                // organized better for this purpose (actually has weights per vertex), but the skin
                // data has higher precision. There may also be multiple partitions, so skin data is
                // less confusing.
                var vertexWeights =
                    (
                        from boneTuple in skinData.bones.Select(
                            (boneData, boneIndex) => (boneData, boneIndex))
                        let boneData = boneTuple.boneData
                        let bone = bonesById[boneIdsByIndex[(uint)boneTuple.boneIndex]]
                        from skinWeight in boneData.vertexWeights
                        // Probably DO need to apply vertex map here. May be inconsistent with
                        // the requirements in TrySetFacesFromVerts.
                        let vertex = outVertices[skinWeight.index]
                        group KeyValuePair.Create(bone, skinWeight.weight) by vertex into g
                        select (vertex: g.Key, weights: g))
                    .ToDictionary(x => x.vertex, x => BoneWeights.FromWeights(x.weights));
                foreach (var vw in vertexWeights)
                    meshBuilder.SetVertexWeights(vw.Key, vw.Value);
                skinInstance.Dispose();
            }

            private void AddSkinTransformsFromShape(
                NiShape shape, SkinnedMesh.Builder meshBuilder,
                IReadOnlyDictionary<uint, Bone> bonesById,
                IReadOnlyDictionary<uint, uint> boneIdsByIndex)
            {
                foreach (var (boneIndex, boneId) in boneIdsByIndex)
                {
                    var bone = bonesById[boneId];
                    using var boneTransform = new MatTransform();
                    if (!nif.GetShapeTransformSkinToBone(shape, boneIndex, boneTransform))
                        continue;
                    var invTransform = boneTransform.ToMat4();
                    meshBuilder.SetInverseTransform(bone, invTransform);
                }
            }

            private async Task<ITextureSource?> CreateTextureSourceAsync(string texturePath)
            {
                if (!Path.GetExtension(texturePath).Equals(".dds", StringComparison.OrdinalIgnoreCase)
                    || !await fileProvider.ExistsAsync(texturePath))
                    return null;
                return await DdsTextureSource.PreloadAsync(
                    () => fileProvider.ReadBytesAsync(texturePath));
            }

            private IReadOnlyDictionary<uint, uint> GetBoneIdsByIndex(NiShape shape)
            {
                using var boneIds = new vectorint();
                nif.GetShapeBoneIDList(shape, boneIds);
                return boneIds
                    .Select((id, index) => (index, id))
                    .ToDictionary(x => (uint)x.index, x => (uint)x.id);
            }

            private IReadOnlyDictionary<uint, Bone> GetBoneMap(
                out IDictionary<Bone, Matrix4x4> defaultTransforms)
            {
                using var childRefs = new setNiRef();
                nif.GetRootNode().GetChildRefs(childRefs);
                var bones = new Dictionary<uint, Bone>();
                defaultTransforms = new Dictionary<Bone, Matrix4x4>();
                foreach (var childRef in childRefs)
                {
                    if (!header.TryGetBlock<NiNode>(childRef, out var boneNode))
                        continue;
                    var boneName = boneNode.name.get();
                    var bone = new Bone(boneName);
                    bones.Add(childRef.index, bone);
                    using var boneGlobalTransform = new MatTransform();
                    nif.GetNodeTransformToGlobal(bone.Name, boneGlobalTransform);
                    defaultTransforms.Add(bone, boneGlobalTransform.ToMat4());
                }
                return bones;
            }

            private async Task<TextureSet> GetTexturesFromShape(NiShape shape)
            {
                if (!shape.HasShaderProperty() ||
                    !header.TryGetBlock<NiShader>(shape.ShaderPropertyRef(), out var shader) ||
                    !shader.HasTextureSet() ||
                    !header.TryGetBlock<BSShaderTextureSet>(shader.TextureSetRef(), out var textureSet))
                    return TextureSet.Empty;
                var textureList = textureSet.textures.items().Select(x => x.get()).ToList();
                textureSet.Dispose();
                shader.Dispose();
                // We'll add more texture types later; currently only support diffuse and normal.
                var diffuseTask = CreateTextureSourceAsync(textureList[0]);
                var normalTask = CreateTextureSourceAsync(textureList[1]);
                return new TextureSet(await diffuseTask, await normalTask);
            }

            private bool TrySetFacesFromVerts(
                NiShape shape, SkinnedMesh.Builder meshBuilder,
                out IImmutableList<Vertex> outVertices)
            {
                using var triangles = new vectorTriangle();
                // Unsure if we need to do the equivalent of ApplyMapToTriangles here (actual
                // function is not available from SWIG) or if it's already incorporated into the
                // line below.
                shape.GetTriangles(triangles);
                using var verts = new vectorVector3();
                using var uvs = new vectorVector2();
                if (!nif.GetVertsForShape(shape, verts) || !nif.GetUvsForShape(shape, uvs))
                {
                    outVertices = ImmutableList<Vertex>.Empty;
                    return false;
                }
                using var normals = nif.GetNormalsForShape(shape);
                var faceVertices = Enumerable.Range(0, verts.Count)
                        .Select(i => MakeVertex(
                            verts[i], normals?[i] ?? new nifly.Vector3(), uvs[i]))
                        .ToImmutableList();
                meshBuilder.SetFaces(
                    from i in Enumerable.Range(0, triangles.Count)
                    let tri = triangles[i]
                    let v1 = faceVertices[tri.p1]
                    let v2 = faceVertices[tri.p2]
                    let v3 = faceVertices[tri.p3]
                    select new Face(new[] { v1, v2, v3 }));
                outVertices = faceVertices;
                return true;
            }

            private static Vertex MakeVertex(nifly.Vector3 point, nifly.Vector3 normal, nifly.Vector2 uv)
            {
                return new Vertex(point.ToVector3(), normal.ToVector3(), uv.ToVector2());
            }
        }
    }
}
