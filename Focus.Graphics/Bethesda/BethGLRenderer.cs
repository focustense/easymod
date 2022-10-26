using Focus.Graphics.OpenGL;
using Silk.NET.OpenGL;
using System.Drawing;
using System.Numerics;

using Texture = Focus.Graphics.OpenGL.Texture;

namespace Focus.Graphics.Bethesda
{
    public class BethGLRenderer : IMeshRenderer
    {
        private static readonly ITextureSource DummyReflectionMap = new DummyTextureSource(Color.White);

        private readonly GL gl;
        private readonly ObjectRenderingSettings renderingSettings;

        private ShaderProgram shaderProgram;
        private VertexArrayObject? vao;
        private BufferObject? vbo;
        private BufferObject? ebo;
        private Texture? diffuseTexture;
        private Texture? normalTexture;
        private Texture? specularMap;
        private Texture? environmentTexture;
        private Texture? reflectionMap;
        private Bounds3 bounds = Bounds3.Default;

        public BethGLRenderer(GL gl, ObjectRenderingSettings renderingSettings)
            : this(gl, CreateDefaultShaderProgram(gl), renderingSettings)
        {
        }

        internal BethGLRenderer(
            GL gl, ShaderProgram shaderProgram, ObjectRenderingSettings renderingSettings)
        {
            this.gl = gl;
            this.renderingSettings = renderingSettings;
            this.shaderProgram = shaderProgram;
        }

        public void Dispose()
        {
            vbo?.Dispose();
            ebo?.Dispose();
            vao?.Dispose();
            diffuseTexture?.Dispose();
            normalTexture?.Dispose();
            shaderProgram.Dispose();
            GC.SuppressFinalize(this);
        }

        public Bounds3 GetModelBounds()
        {
            return bounds;
        }

        public void LoadGeometry(IMesh mesh)
        {
            var ungroupedVertices = mesh.Faces
                .SelectMany(f => f.Triangulate(Tri.WithVertices))
                .SelectMany(t => t.GetVertices())
                .ToList();
            var vertices = ungroupedVertices
                .GroupBy(v => (v.Point, v.Normal, v.UV))
                .Select(g => new VertexData(
                    g.Key.Point, g.Key.Normal, g.Sum(x => x.Tangent), g.Sum(x => x.Bitangent), g.Key.UV))
                .ToArray();
            var vertexIndexMap = vertices
                .Select((v, i) => new { v.Point, v.Normal, v.UV, Index = (uint)i })
                .ToDictionary(x => (x.Point, x.Normal, x.UV), x => x.Index);
            var vertexIndices = ungroupedVertices
                .Select(v => vertexIndexMap[(v.Point, v.Normal, v.UV)])
                .ToArray();
            bounds = Bounds3.FromPoints(vertices.Select(v => v.Point));

            vbo = BufferObject.Create(gl, BufferTargetARB.ArrayBuffer, vertices);
            ebo = BufferObject.Create(gl, BufferTargetARB.ElementArrayBuffer, vertexIndices);
            vao = VertexArrayObject.Bind(gl, vbo, ebo);
            vao.EnableAttributeArray(0, 3, VertexAttribPointerType.Float, VertexData.PointOffset);
            vao.EnableAttributeArray(1, 3, VertexAttribPointerType.Float, VertexData.NormalOffset);
            vao.EnableAttributeArray(2, 3, VertexAttribPointerType.Float, VertexData.TangentOffset);
            vao.EnableAttributeArray(3, 3, VertexAttribPointerType.Float, VertexData.BitangentOffset);
            vao.EnableAttributeArray(4, 2, VertexAttribPointerType.Float, VertexData.UVOffset);
        }

        public void LoadTextures(TextureSet textureSet)
        {
            diffuseTexture = textureSet.Diffuse?.CreateTexture(gl, TextureUnit.Texture0);
            normalTexture = textureSet.Normal?.CreateTexture(gl, TextureUnit.Texture1);
            specularMap = textureSet.Specular?.CreateTexture(gl, TextureUnit.Texture2);
            environmentTexture = textureSet.Environment?.CreateTexture(gl, TextureUnit.Texture3);
            // Reflection map is only interesting if an environment texture is specified.
            // If so, we need a reflection map and should create a dummy if none exists.
            reflectionMap = environmentTexture != null
                ? (textureSet.Reflection ?? DummyReflectionMap).CreateTexture(
                    gl, TextureUnit.Texture4)
                : null;
        }

        public unsafe void Render(Matrix4x4 model, Matrix4x4 view, Matrix4x4 projection)
        {
            if (vao == null || ebo == null)
                return;
            vao.Bind();
            shaderProgram.Use();
            shaderProgram.SetUniform("modelToWorld", model);
            shaderProgram.SetUniform("worldToView", view);
            shaderProgram.SetUniform(
                "viewToModel",
                Matrix4x4.Transpose(
                    Matrix4x4.Invert(view * model, out var inverted)
                        ? inverted
                        : throw new Exception("Failed to compute inverse model-view transform.")));
            shaderProgram.SetUniform("projection", projection);
            shaderProgram.SetUniform(
                "ambientLightingColor", renderingSettings.AmbientLightingColor.ToRgbVector());
            shaderProgram.SetUniform(
                "ambientLightingStrength", renderingSettings.AmbientLightingStrength);
            shaderProgram.SetUniform(
                "diffuseLightingColor", renderingSettings.DiffuseLightingColor.ToRgbVector());
            shaderProgram.SetUniform(
                "diffuseLightingStrength", renderingSettings.DiffuseLightingStrength);
            shaderProgram.SetUniform(
                "specularLightingColor", renderingSettings.SpecularLightingColor.ToRgbVector());
            shaderProgram.SetUniform(
                "specularLightingStrength", renderingSettings.SpecularLightingStrength);
            shaderProgram.SetUniform("specularSource", (int)renderingSettings.SpecularSource);
            shaderProgram.SetUniform("shininess", renderingSettings.Shininess);
            shaderProgram.SetUniform("environmentStrength", renderingSettings.EnvironmentStrength);
            shaderProgram.SetUniform("lightPosition", new Vector3(0f, 0f, -50f));
            shaderProgram.SetUniform("normalSpace", (int)renderingSettings.NormalSpace);
            shaderProgram.SetUniform("normalMapSwizzle", (int)renderingSettings.NormalMapSwizzle);
            shaderProgram.SetUniform("hasNormalMap", normalTexture != null);
            shaderProgram.SetUniform("hasEnvironmentTexture", environmentTexture != null);
            BindTextures();
            gl.DrawElements(PrimitiveType.Triangles, ebo.ElementCount, DrawElementsType.UnsignedInt, null);
        }

        private void BindTextures()
        {
            if (diffuseTexture != null)
            {
                diffuseTexture.Bind();
                shaderProgram.SetUniform("diffuseTexture", 0);
            }
            if (normalTexture != null)
            {
                normalTexture.Bind();
                shaderProgram.SetUniform("normalMap", 1);
            }
            if (specularMap != null)
            {
                specularMap.Bind();
                shaderProgram.SetUniform("specularMap", 2);
            }
            if (environmentTexture != null)
            {
                environmentTexture.Bind();
                shaderProgram.SetUniform("environmentTexture", 3);
            }
            if (reflectionMap != null)
            {
                reflectionMap.Bind();
                shaderProgram.SetUniform("reflectionMap", 4);
            }
        }

        private static ShaderProgram CreateDefaultShaderProgram(GL gl)
        {
            var vertexShaderPath =
                Path.Combine(AppContext.BaseDirectory, "Bethesda", "shader.vert");
            var fragmentShaderPath =
                Path.Combine(AppContext.BaseDirectory, "Bethesda", "shader.frag");
            return ShaderProgram.FromFiles(gl, vertexShaderPath, fragmentShaderPath);
        }
    }
}
