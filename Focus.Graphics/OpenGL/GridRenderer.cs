using Silk.NET.OpenGL;
using System.Drawing;
using System.Numerics;

namespace Focus.Graphics.OpenGL
{
    public enum GridPlane { X, Y, Z }

    public class GridRenderer : IRenderer
    {
        public Color Color { get; set; } = Color.LightGray;

        private readonly GL gl;
        private readonly ShaderProgram shaderProgram;
        private readonly GridPlane plane;

        private VertexArrayObject? vao;
        private BufferObject? vbo;

        public GridRenderer(GL gl, GridPlane plane = GridPlane.Y)
            : this(gl, CreateDefaultShaderProgram(gl), plane)
        { }

        internal GridRenderer(GL gl, ShaderProgram shaderProgram, GridPlane plane)
        {
            this.gl = gl;
            this.shaderProgram = shaderProgram;
            this.plane = plane;
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
            shaderProgram.SetUniform("model", model);
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

        private IEnumerable<Vector3> GenerateVertices(float interval, float max)
        {
            // Getting a delegate for what to run inside the loop is much faster than putting a
            // switch statement inside the loop.
            var generator = GetGridLineGenerator(plane);
            for (var i = -max; i <= max; i += interval)
                foreach (var vertex in generator(i, max))
                    yield return vertex;
        }

        private static IEnumerable<Vector3> GenerateVerticesX(float i, float max)
        {
            yield return new Vector3(0, i, -max);
            yield return new Vector3(0, i, max);
            yield return new Vector3(0, -max, i);
            yield return new Vector3(0, max, i);
        }

        private static IEnumerable<Vector3> GenerateVerticesY(float i, float max)
        {
            yield return new Vector3(i, 0, -max);
            yield return new Vector3(i, 0, max);
            yield return new Vector3(-max, 0, i);
            yield return new Vector3(max, 0, i);
        }

        private static IEnumerable<Vector3> GenerateVerticesZ(float i, float max)
        {
            yield return new Vector3(i, -max, 0);
            yield return new Vector3(i, max, 0);
            yield return new Vector3(-max, i, 0);
            yield return new Vector3(max, i, 0);
        }

        private static Func<float, float, IEnumerable<Vector3>> GetGridLineGenerator(
            GridPlane plane) => plane switch
            {
                GridPlane.X => GenerateVerticesX,
                GridPlane.Y => GenerateVerticesY,
                GridPlane.Z => GenerateVerticesZ,
                _ => throw new ArgumentException($"Unrecognized plane: {plane}", nameof(plane)),
            };
    }
}
