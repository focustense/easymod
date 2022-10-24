using Silk.NET.OpenGL;
using System.Drawing;
using System.Numerics;

namespace Focus.Graphics.OpenGL
{
    public class GridRenderer : IRenderer
    {
        public Color Color { get; set; } = Color.LightGray;

        private readonly GL gl;
        private readonly ShaderProgram shaderProgram;

        private VertexArrayObject? vao;
        private BufferObject? vbo;

        public GridRenderer(GL gl)
            : this(gl, CreateDefaultShaderProgram(gl))
        { }

        internal GridRenderer(GL gl, ShaderProgram shaderProgram)
        {
            this.gl = gl;
            this.shaderProgram = shaderProgram;
        }

        public void Dispose()
        {
            vbo?.Dispose();
            vao?.Dispose();
            shaderProgram.Dispose();
            GC.SuppressFinalize(this);
        }

        public Bounds3 GetModelBounds()
        {
            // Generally don't want grid to contribute to bounds calculation, as it's configured
            // independently of the scene itself.
            return Bounds3.Empty;
        }

        public unsafe void Render(Matrix4x4 model, Matrix4x4 view, Matrix4x4 projection)
        {
            if (vao == null || vbo == null)
                return;
            vao.Bind();
            shaderProgram.Use();
            // Ignore model transformation, grid is static.
            shaderProgram.SetUniform("model", Matrix4x4.Identity);
            shaderProgram.SetUniform("view", view);
            shaderProgram.SetUniform("projection", projection);
            shaderProgram.SetUniform("objectColor", Color.ToRgbVector());
            gl.DrawArrays(PrimitiveType.Lines, 0, vbo.ElementCount);
        }

        public void SetupGrid(float interval, float max)
        {
            var vertices = GenerateVertices(interval, max).ToArray();
            vbo = BufferObject.Create(gl, BufferTargetARB.ArrayBuffer, vertices);
            vao = VertexArrayObject.Bind(gl, vbo);
            vao.EnableAttributeArray(0, 3, VertexAttribPointerType.Float, 0);
        }

        private static ShaderProgram CreateDefaultShaderProgram(GL gl)
        {
            var vertexShaderPath =
                Path.Combine(AppContext.BaseDirectory, "OpenGL", "simple.vert");
            var fragmentShaderPath =
                Path.Combine(AppContext.BaseDirectory, "OpenGL", "simple.frag");
            return ShaderProgram.FromFiles(gl, vertexShaderPath, fragmentShaderPath);
        }

        private static IEnumerable<Vector3> GenerateVertices(float interval, float max)
        {
            for (var i = -max; i <= max; i += interval)
            {
                yield return new Vector3(i, 0, -max);
                yield return new Vector3(i, 0, max);
                yield return new Vector3(-max, 0, i);
                yield return new Vector3(max, 0, i);
            }
        }
    }
}
