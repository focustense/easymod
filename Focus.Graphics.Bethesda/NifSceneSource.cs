﻿using Focus.Files;
using Focus.Graphics.Formats;
using nifly;
using System.Collections.Immutable;
using System.Drawing;
using System.Numerics;

namespace Focus.Graphics.Bethesda
{
    public class NifSceneSource : ISceneSource
    {
        enum TextureSlot
        {
            Diffuse = 0,
            Normal = 1,
            Subsurface = 2,
            Detail = 3,
            Environment = 4,
            Reflection = 5,
            Tint = 6,
            Specular = 7,
        }

        public class Settings
        {
            // Reflections seem generally weak compared to what we see in NifSkope, etc.
            public float EnvironmentMultiplier { get; init; } = 3.0f;

            // Speculars seem generally weak compared to what we see in NifSkope, etc.
            // Using values > 1 make ours look much closer.
            public float SpecularMultiplier { get; init; } = 2.0f;
        }

        private readonly FileLoaderAsync openFile;
        private readonly IAsyncFileProvider fileProvider;
        private readonly IConcurrentCache<string, Task<ITextureSource?>> textureCache;
        private readonly Settings settings;

        public NifSceneSource(
            IAsyncFileProvider fileProvider, string fileName,
            IConcurrentCache<string, Task<ITextureSource?>>? textureCache = null,
            Settings? settings = null)
            : this(
                  fileProvider, NifLoader.FromProviderFile(fileProvider, fileName), textureCache,
                  settings)
        {
        }

        public NifSceneSource(
            IAsyncFileProvider fileProvider, FileLoaderAsync openFile,
            IConcurrentCache<string, Task<ITextureSource?>>? textureCache = null,
            Settings? settings = null)
        {
            this.fileProvider = fileProvider;
            this.settings = settings ?? new();
            this.textureCache = textureCache ?? new NullCache<string, Task<ITextureSource?>>();
            this.openFile = openFile;
        }

        public async Task<Scene> LoadAsync()
        {
            using var file = await openFile();
            var objects = Load(file);
            // NIFs don't define their own lights.
            return new(objects, Enumerable.Empty<Light>());
        }

        private IEnumerable<SceneObject> Load(NifFile file)
        {
            using var loader = new ObjectLoader(file, fileProvider, textureCache, settings);
            return loader.LoadObjects();
        }

        // Using an inner class for this allows us to save some disposables without having to pass
        // them around from method to method. Just a convenience.
        class ObjectLoader : IDisposable
        {
            private static readonly Task<ITextureSource?> UnspecifiedTextureTask = Task.FromResult((ITextureSource?)null);

            private readonly NifFile nif;
            private readonly NiHeader header;
            private readonly IAsyncFileProvider fileProvider;
            private readonly IConcurrentCache<string, Task<ITextureSource?>> textureCache;
            private readonly Settings settings;

            public ObjectLoader(
                NifFile file, IAsyncFileProvider fileProvider,
                IConcurrentCache<string, Task<ITextureSource?>> textureCache, Settings settings)
            {
                this.fileProvider = fileProvider;
                this.settings = settings;
                this.textureCache = textureCache;
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
                var renderingSettings = GetShapeRenderingSettings(shape);
                var textures = GetTexturesFromShape(shape);
                return new SceneObject(mesh, textures, renderingSettings);
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
                try
                {
                    return await ManagedDdsTextureSource.PreloadAsync(
                        () => fileProvider.ReadBytesAsync(texturePath));
                }
                catch
                {
                    // TODO: Log something
                }

                // Sometimes - rarely - BCnEncoder.NET fails with divide by zero. The DirectXTex
                // version generally works for these odd cases.
                try
                {
                    return await NativeDdsTextureSource.PreloadAsync(
                        () => fileProvider.ReadBytesAsync(texturePath));
                }
                catch
                {
                    // TODO: Log something
                    return null;
                }
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

            private ObjectRenderingSettings GetShapeRenderingSettings(NiShape shape)
            {
                if (!shape.HasShaderProperty() ||
                    !header.TryGetBlock<NiShader>(shape.ShaderPropertyRef(), out var shader))
                    return new ObjectRenderingSettings();
                nifly.Vector3? tintColor = null;
                if (shader is BSLightingShaderProperty bsShader)
                {
                    var shaderType = shader.GetShaderType();
                    if (shaderType == (int)BSLightingShaderPropertyShaderType.BSLSP_HAIRTINT)
                        tintColor = bsShader.hairTintColor;
                    else if (shaderType == (int)BSLightingShaderPropertyShaderType.BSLSP_SKINTINT)
                        tintColor = bsShader.skinTintColor;
                }
                return new ObjectRenderingSettings
                {
                    Shininess = shader.GetGlossiness(),
                    SpecularSource = shader.HasSpecular()
                        ? SupportsSpecularMap(shader)
                            ? SpecularSource.SpecularMap : SpecularSource.NormalMapAlpha
                        : SpecularSource.None,
                    SpecularLightingColor = shader.GetSpecularColor().ToColor(),
                    SpecularLightingStrength = shader.HasSpecular()
                        ? shader.GetSpecularStrength() * settings.SpecularMultiplier : 0,
                    EnvironmentStrength =
                        shader.GetEnvironmentMapScale() * settings.EnvironmentMultiplier,
                    NormalSpace = shader.IsModelSpace()
                        ? NormalSpace.ObjectSpace : NormalSpace.TangentSpace,
                    // This seems to be right for some NIFs, like hands, but not others, like face?
                    NormalMapSwizzle = shader.IsModelSpace()
                        ? NormalMapSwizzle.RBGA : NormalMapSwizzle.None,
                    TintColor = tintColor?.ToColor() ?? Color.White,
                };
            }

            private Task<ITextureSource?> GetTextureSourceAsync(
                IReadOnlyList<string> texturePaths, TextureSlot slot)
            {
                return GetTextureSourceAsync(texturePaths[(int)slot]);
            }

            private Task<ITextureSource?> GetTextureSourceAsync(string texturePath)
            {
                return textureCache.GetOrAdd(texturePath, _ => CreateTextureSourceAsync(texturePath));
            }

            private async Task<TextureSet> GetTexturesFromShape(NiShape shape)
            {
                if (!shape.HasShaderProperty() ||
                    !header.TryGetBlock<NiShader>(shape.ShaderPropertyRef(), out var shader) ||
                    !shader.HasTextureSet() ||
                    !header.TryGetBlock<BSShaderTextureSet>(shader.TextureSetRef(), out var textureSet))
                    return TextureSet.Empty;
                var textureList = textureSet.textures.items().Select(x => x.get()).ToList();
                var diffuseTask = GetTextureSourceAsync(textureList, TextureSlot.Diffuse);
                var normalTask = GetTextureSourceAsync(textureList, TextureSlot.Normal);
                var specularTask = SupportsSpecularMap(shader)
                    ? GetTextureSourceAsync(textureList, TextureSlot.Specular)
                    : UnspecifiedTextureTask;
                var environmentTask = GetTextureSourceAsync(textureList, TextureSlot.Environment);
                var reflectionTask = GetTextureSourceAsync(textureList, TextureSlot.Reflection);
                var supportsTint = IsTintShader(shader.GetShaderType());
                var detailTask = supportsTint
                    ? GetTextureSourceAsync(textureList, TextureSlot.Detail)
                    : UnspecifiedTextureTask;
                var tintTask = supportsTint
                    ? GetTextureSourceAsync(textureList, TextureSlot.Tint)
                    : UnspecifiedTextureTask;
                return new TextureSet(
                    await diffuseTask, await normalTask, await specularTask, await environmentTask,
                    await reflectionTask, await detailTask, await tintTask);
            }

            private static bool IsTintShader(uint shaderType)
            {
                return shaderType == (int)BSLightingShaderPropertyShaderType.BSLSP_FACE
                    || shaderType == (int)BSLightingShaderPropertyShaderType.BSLSP_HAIRTINT
                    || shaderType == (int)BSLightingShaderPropertyShaderType.BSLSP_SKINTINT;
            }

            private static bool SupportsSpecularMap(NiShader shader)
            {
                var shaderType = shader.GetShaderType();
                return IsTintShader(shaderType)
                    || shaderType == (int)BSLightingShaderPropertyShaderType.BSLSP_MULTILAYERPARALLAX;
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