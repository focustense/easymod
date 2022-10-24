using Silk.NET.OpenGL;
using System.Drawing;
using System.Numerics;

namespace Focus.Graphics.OpenGL
{
    public class PhongRenderer : IMeshRenderer
    {
        public float AmbientStrength { get; set; } = 0.1f;
        public Color LightColor { get; set; } = Color.White;
        public Vector3 LightPosition { get; set; } = Vector3.UnitY;
        public IReadOnlyList<Color> Palette { get; set; } = new[] { Color.Orange };
        public float Shininess { get; set; } = 32;
        public float SpecularStrength { get; set; } = 0.5f;

        private readonly GL gl;
        private readonly ShaderProgram shaderProgram;

        private VertexArrayObject? vao;
        private BufferObject? vbo;
        private BufferObject? ebo;
        private Bounds3 bounds = Bounds3.Default;

        public PhongRenderer(GL gl) : this(gl, CreateDefaultShaderProgram(gl))
        {
        }

        internal PhongRenderer(
            GL gl, ShaderProgram shaderProgram)
        {
            this.gl = gl;
            this.shaderProgram = shaderProgram;
        }

        public void Dispose()
        {
            vbo?.Dispose();
            ebo?.Dispose();
            vao?.Dispose();
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
                .Select((f, i) => (face: f, color: Palette[i % Palette.Count]))
                .SelectMany(f => f.face.Triangulate(
                    (a, b, c) => SimpleTri.WithVertices(a, b, c, f.color.ToRgbVector())))
                .SelectMany(t => t.GetVertices())
                .ToList();
            var vertices = ungroupedVertices
                .DistinctBy(x => (x.Point, x.Normal))
                .ToArray();
            var vertexIndexMap = vertices
                .Select((v, i) => new { v.Point, v.Normal, Index = (uint)i })
                .ToDictionary(x => (x.Point, x.Normal), x => x.Index);
            var vertexIndices = ungroupedVertices
                .Select(v => vertexIndexMap[(v.Point, v.Normal)])
                .ToArray();
            bounds = Bounds3.FromPoints(vertices.Select(v => v.Point));

            vbo = BufferObject.Create(gl, BufferTargetARB.ArrayBuffer, vertices);
            ebo = BufferObject.Create(gl, BufferTargetARB.ElementArrayBuffer, vertexIndices);
            vao = VertexArrayObject.Bind(gl, vbo, ebo);
            vao.EnableAttributeArray(0, 3, VertexAttribPointerType.Float, SimpleVertexData.PointOffset);
            vao.EnableAttributeArray(1, 3, VertexAttribPointerType.Float, SimpleVertexData.NormalOffset);
            vao.EnableAttributeArray(2, 3, VertexAttribPointerType.Float, SimpleVertexData.ColorOffset);
        }

        public void LoadTextures(TextureSet textureSet)
        {
            throw new NotSupportedException("This renderer does not support texture maps.");
        }

        public unsafe void Render(Matrix4x4 model, Matrix4x4 view, Matrix4x4 projection)
        {
            if (vao == null || ebo == null)
                return;
            vao.Bind();
            shaderProgram.Use();
            shaderProgram.SetUniform("model", model);
            shaderProgram.SetUniform("view", view);
            shaderProgram.SetUniform("projection", projection);
            shaderProgram.SetUniform("ambientStrength", AmbientStrength);
            shaderProgram.SetUniform("lightPosition", LightPosition);
            shaderProgram.SetUniform("lightColor", LightColor.ToRgbVector());
            shaderProgram.SetUniform("shininess", Shininess);
            shaderProgram.SetUniform("specularStrength", SpecularStrength);
            gl.DrawElements(PrimitiveType.Triangles, ebo.ElementCount, DrawElementsType.UnsignedInt, null);
        }

        private static ShaderProgram CreateDefaultShaderProgram(GL gl)
        {
            var vertexShaderPath =
                Path.Combine(AppContext.BaseDirectory, "OpenGL", "phong.vert");
            var fragmentShaderPath =
                Path.Combine(AppContext.BaseDirectory, "OpenGL", "phong.frag");
            return ShaderProgram.FromFiles(gl, vertexShaderPath, fragmentShaderPath);
        }
    }
}
