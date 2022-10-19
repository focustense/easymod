using Silk.NET.OpenGL;
using System.Numerics;

namespace Focus.Graphics.OpenGL
{
    public class ShaderProgram : IDisposable
    {
        public static ShaderProgram FromFiles(GL gl, string vertexPath, string fragmentPath)
        {
            var vertexSource = File.ReadAllText(vertexPath);
            var fragmentSource = File.ReadAllText(fragmentPath);
            return FromSources(gl, vertexSource, fragmentSource);
        }

        public static ShaderProgram FromSources(GL gl, string vertexSource, string fragmentSource)
        {
            var vertexShader = CompileShader(gl, ShaderType.VertexShader, vertexSource);
            var fragmentShader = CompileShader(gl, ShaderType.FragmentShader, fragmentSource);
            var handle = gl.CreateProgram();
            gl.AttachShader(handle, vertexShader);
            gl.AttachShader(handle, fragmentShader);
            gl.LinkProgram(handle);
            gl.GetProgram(handle, GLEnum.LinkStatus, out var status);
            if (status == 0)
                throw new Exception(
                    $"Program failed to link with error: {gl.GetProgramInfoLog(handle)}");
            gl.DetachShader(handle, vertexShader);
            gl.DetachShader(handle, fragmentShader);
            gl.DeleteShader(vertexShader);
            gl.DeleteShader(fragmentShader);
            return new ShaderProgram(gl, handle);
        }

        private readonly GL gl;
        private readonly uint handle;

        private ShaderProgram(GL gl, uint handle)
        {
            this.gl = gl;
            this.handle = handle;
        }

        public void Dispose()
        {
            gl.DeleteProgram(handle);
            GC.SuppressFinalize(this);
        }

        public void SetUniform(string name, float value)
        {
            SetUniform(name, value, gl.Uniform1);
        }

        public void SetUniform(string name, int value)
        {
            SetUniform(name, value, gl.Uniform1);
        }

        public void SetUniform(string name, Vector3 value)
        {
            SetUniform(name, value, gl.Uniform3);
        }

        public unsafe void SetUniform(string name, Matrix4x4 value)
        {
            SetUniform(name, value, (loc, val) => gl.UniformMatrix4(loc, 1, false, (float*)&val));
        }

        public void Use()
        {
            gl.UseProgram(handle);
        }

        private static uint CompileShader(GL gl, ShaderType type, string source)
        {
            var shader = gl.CreateShader(type);
            gl.ShaderSource(shader, source);
            gl.CompileShader(shader);
            var infoLog = gl.GetShaderInfoLog(shader);
            if (!string.IsNullOrWhiteSpace(infoLog))
                throw new Exception(
                    $"Error compiling shader of type {type}, failed with error {infoLog}");
            return shader;
        }

        private void SetUniform<T>(string name, T value, Action<int, T> bind)
        {
            var location = gl.GetUniformLocation(handle, name);
            if (location == -1)
                throw new Exception($"{name} uniform not found on shader.");
            bind(location, value);
        }
    }
}
