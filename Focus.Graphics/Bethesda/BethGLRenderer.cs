using Focus.Graphics.OpenGL;
using Silk.NET.OpenGL;
using System.Numerics;

using Texture = Focus.Graphics.OpenGL.Texture;

namespace Focus.Graphics.Bethesda
{
    public class BethGLRenderer : IRenderer
    {
        private readonly GL gl;

        private ShaderProgram shaderProgram;
        private VertexArrayObject? vao;
        private BufferObject? vbo;
        private BufferObject? ebo;
        private Texture? diffuseTexture;
        private Texture? normalTexture;
        private Vector3 center;
        private Bounds bounds = new(Vector3.Zero, Vector3.One);

        public BethGLRenderer(GL gl)
            : this(gl, CreateDefaultShaderProgram(gl))
        {
        }

        internal BethGLRenderer(GL gl, ShaderProgram shaderProgram)
        {
            this.gl = gl;
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

        public Vector3 GetModelSize()
        {
            return Vector3.Abs(bounds.Max - bounds.Min);
        }

        public void LoadGeometry(IMesh mesh)
        {
            var ungroupedVertices = mesh.Faces
                .SelectMany(f => f.Triangulate())
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
            bounds = vertices.Aggregate(
                new Bounds(Vector3.Zero, Vector3.Zero),
                (bounds, x) => new Bounds(
                    Vector3.Min(bounds.Min, x.Point), Vector3.Max(bounds.Max, x.Point)));
            center =
                vertices.Aggregate(Vector3.Zero, (sum, v) => sum + v.Point) / vertices.Length;

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
            diffuseTexture = textureSet.Diffuse?.CreateTexture(gl);
            normalTexture = textureSet.Normal?.CreateTexture(gl);
        }

        public unsafe void Render(Matrix4x4 model, Matrix4x4 view, Matrix4x4 projection)
        {
            if (vao == null || ebo == null)
                return;
            vao.Bind();
            shaderProgram.Use();
            var centerModel = Matrix4x4.CreateTranslation(-center) * model;
            shaderProgram.SetUniform("model", centerModel);
            shaderProgram.SetUniform("view", view);
            shaderProgram.SetUniform("projection", projection);
            shaderProgram.SetUniform("ambientLightingStrength", 0.4f);
            shaderProgram.SetUniform("specularLightingStrength", 1.0f);
            shaderProgram.SetUniform("lightColor", Vector3.One);
            shaderProgram.SetUniform("lightPosition", new Vector3(0f, -1f, 0f));
            BindTextures();
            gl.DrawElements(PrimitiveType.Triangles, ebo.ElementCount, DrawElementsType.UnsignedInt, null);
        }

        private void BindTextures()
        {
            if (diffuseTexture != null)
            {
                diffuseTexture.Bind(TextureUnit.Texture0);
                shaderProgram.SetUniform("diffuseTexture", 0);
            }
            if (normalTexture != null)
            {
                normalTexture.Bind(TextureUnit.Texture1);
                shaderProgram.SetUniform("normalTexture", 1);
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

        record Bounds(Vector3 Min, Vector3 Max);
    }
}
