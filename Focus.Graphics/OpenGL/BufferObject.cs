using Silk.NET.OpenGL;

namespace Focus.Graphics.OpenGL
{
    public class BufferObject : IDisposable
    {
        public static unsafe BufferObject Create<T>(GL gl, BufferTargetARB bufferType, T[] data)
            where T : unmanaged
        {
            return Create(gl, bufferType, data.AsSpan());
        }

        public static unsafe BufferObject Create<T>(GL gl, BufferTargetARB bufferType, Span<T> data)
            where T : unmanaged
        {
            var handle = gl.GenBuffer();
            var elementSize = (uint)sizeof(T);
            var buffer = new BufferObject(gl, bufferType, handle, (uint)data.Length, elementSize);
            buffer.Bind();
            fixed (void* d = data)
            {
                gl.BufferData(
                    bufferType, (nuint)(data.Length * elementSize), d, BufferUsageARB.StaticDraw);
            }
            return buffer;
        }

        public uint ElementCount { get; }
        public uint ElementSize { get; }

        private readonly GL gl;
        private readonly BufferTargetARB bufferType;
        private readonly uint handle;

        unsafe public BufferObject(GL gl, BufferTargetARB bufferType, uint handle, uint elementCount, uint elementSize)
        {
            this.gl = gl;
            this.bufferType = bufferType;
            this.handle = handle;
            ElementCount = elementCount;
            ElementSize = elementSize;
        }

        internal unsafe void Bind()
        {
            gl.BindBuffer(bufferType, handle);
        }

        public void Dispose()
        {
            gl.DeleteBuffer(handle);
            GC.SuppressFinalize(this);
        }
    }
}
